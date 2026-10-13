using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BazarKav.BuildingBlocks.Application;
using BazarKav.BuildingBlocks.Web;
using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Application.Mappings;
using BazarKav.Tenant.Infrastructure.Persistence.Configurations;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.ValueObjects;
using BazarKav.Tenant.Infrastructure;
using BazarKav.Tenant.Infrastructure.Persistence;
using BazarKav.Tenant.Infrastructure.Persistence.Repositories;
using BazarKav.Tenant.Presentation;
using BazarKav.Tenant.Presentation.Endpoints;
using BazarKav.Tenant.Presentation.Requests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BAZARKAV_TEST_POSTGRES")))
            Skip = "Set BAZARKAV_TEST_POSTGRES to an isolated bazarkav_test PostgreSQL database. CI always supplies it.";
    }
}

public sealed class TenantPostgresTests : IAsyncLifetime
{
    private readonly string databaseName = "bazarkav_test_" + Guid.NewGuid().ToString("N");
    private string adminConnection = null!;
    private string connection = null!;
    private bool created;

    public async Task InitializeAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("BAZARKAV_TEST_POSTGRES")
            ?? throw new InvalidOperationException("PostgreSQL test configuration is missing."));
        if (builder.Database != "bazarkav_test")
            throw new InvalidOperationException("Only the explicitly named bazarkav_test database is accepted.");
        adminConnection = builder.ConnectionString;
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        // The identifier is generated locally from a constant prefix and a GUID, never user input.
        await new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin).ExecuteNonQueryAsync();
        created = true;
        builder.Database = databaseName;
        connection = builder.ConnectionString;
        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!created) return;
        using (var pooled = new NpgsqlConnection(connection)) NpgsqlConnection.ClearPool(pooled);
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin).ExecuteNonQueryAsync();
    }

    private TenantDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connection).Options);

    private async Task<WebApplication> StartAppAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddBazarKavWeb();
        builder.Services.AddBazarKavApplication(TenantPresentation.ApplicationAssembly);
        builder.Services.AddTenantInfrastructure(connection);
        builder.Services.AddTenantPresentation();
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
        var app = builder.Build();
        app.UseBazarKavWeb();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTenantEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient AdminClient(WebApplication app)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Actor", "admin");
        return client;
    }

    [PostgresFact]
    public async Task Migration_is_repeatable_and_model_has_no_drift()
    {
        await using var db = NewContext();
        Assert.False(db.Database.HasPendingModelChanges());
        await db.Database.MigrateAsync();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Tenants.ToListAsync());
    }

    [PostgresFact]
    public async Task Http_create_rename_activate_suspend_resume_and_read_survive_new_scopes()
    {
        await using var app = await StartAppAsync();
        using var client = AdminClient(app);
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "tenant-flow");
        using var createdResponse = await client.PostAsJsonAsync("/api/v1/tenants", new CreateTenantRequest("sample-shop", "فروشگاه"));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal("tenant-flow", createdResponse.Headers.GetValues("X-Correlation-ID").Single());
        var tenant = (await createdResponse.Content.ReadFromJsonAsync<TenantDto>())!;
        Assert.Equal(TenantState.SettingUp, tenant.Status);
        Assert.Equal(1, tenant.Version);
        Assert.Equal($"/api/v1/tenants/{tenant.Id}", createdResponse.Headers.Location!.OriginalString);
        using var renameResponse = await client.PutAsJsonAsync($"/api/v1/tenants/{tenant.Id}/name",
            new RenameTenantRequest("نام جدید", tenant.Version));
        Assert.Equal(HttpStatusCode.OK, renameResponse.StatusCode);
        tenant = (await renameResponse.Content.ReadFromJsonAsync<TenantDto>())!;
        foreach (var state in new[] { TenantState.Active, TenantState.Suspended, TenantState.Active })
        {
            using var response = await client.PutAsJsonAsync($"/api/v1/tenants/{tenant.Id}/status",
                new ChangeTenantStatusRequest(state, state == TenantState.Suspended ? "توقف موقت" : null, tenant.Version));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            tenant = (await response.Content.ReadFromJsonAsync<TenantDto>())!;
            Assert.Equal(state, tenant.Status);
        }
        var persisted = await client.GetFromJsonAsync<TenantDto>($"/api/v1/tenants/{tenant.Id}");
        Assert.Equal("نام جدید", persisted!.Name);
        Assert.Equal("sample-shop", persisted.Code);
        Assert.Equal(5, persisted.Version);
        Assert.Null(persisted.SuspensionReason);
    }

    [PostgresFact]
    public async Task Http_rejects_invalid_input_duplicates_stale_versions_and_illegal_transitions()
    {
        await using var app = await StartAppAsync();
        using var client = AdminClient(app);
        using var invalid = await client.PostAsJsonAsync("/api/v1/tenants", new CreateTenantRequest("BAD", ""));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var createdResponse = await client.PostAsJsonAsync("/api/v1/tenants", new CreateTenantRequest("sample", "نام"));
        var tenant = (await createdResponse.Content.ReadFromJsonAsync<TenantDto>())!;
        using var duplicate = await client.PostAsJsonAsync("/api/v1/tenants", new CreateTenantRequest("sample", "دیگر"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var stale = await client.PutAsJsonAsync($"/api/v1/tenants/{tenant.Id}/name", new RenameTenantRequest("دیگر", 9));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var badTransition = await client.PutAsJsonAsync($"/api/v1/tenants/{tenant.Id}/status",
            new ChangeTenantStatusRequest(TenantState.Suspended, "reason", tenant.Version));
        Assert.Equal(HttpStatusCode.Conflict, badTransition.StatusCode);
        using var missing = await client.GetAsync($"/api/v1/tenants/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var badPaging = await client.GetAsync("/api/v1/tenants?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, badPaging.StatusCode);
        await using var db = NewContext();
        var only = Assert.Single(await db.Tenants.ToListAsync());
        Assert.Equal("نام", only.Name);
        Assert.Equal(1, only.Version);
    }

    [PostgresFact]
    public async Task Management_requires_authenticated_permission_and_headers_do_not_grant_access()
    {
        await using var app = await StartAppAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Tenant-ID", Guid.NewGuid().ToString());
        using var anonymous = await client.PostAsJsonAsync("/api/v1/tenants", new CreateTenantRequest("blocked", "نام"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Actor", "member");
        using var forbidden = await client.GetAsync("/api/v1/tenants");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        await using var db = NewContext();
        Assert.Empty(await db.Tenants.ToListAsync());
    }

    [PostgresFact]
    public async Task Database_uniqueness_is_authoritative_after_both_prechecks_pass()
    {
        await using var first = NewContext();
        await using var second = NewContext();
        var repo1 = new EfTenantRepository(first);
        var repo2 = new EfTenantRepository(second);
        Assert.False(await repo1.CodeExistsAsync("race", default));
        Assert.False(await repo2.CodeExistsAsync("race", default));
        repo1.Add(TenantEntity.Create(Guid.NewGuid(), "race", "اول", DateTimeOffset.UtcNow));
        repo2.Add(TenantEntity.Create(Guid.NewGuid(), "race", "دوم", DateTimeOffset.UtcNow));
        Assert.Equal(TenantCommitOutcome.Saved, await new EfTenantUnitOfWork(first).SaveChangesAsync(default));
        Assert.Equal(TenantCommitOutcome.DuplicateCode, await new EfTenantUnitOfWork(second).SaveChangesAsync(default));
        Assert.Empty(second.ChangeTracker.Entries());
        await using var check = NewContext();
        Assert.Single(await check.Tenants.ToListAsync());
    }

    [PostgresFact]
    public async Task Concurrent_edits_are_rejected_and_failed_transaction_is_atomic()
    {
        var seed = TenantEntity.Create(Guid.NewGuid(), "seed-shop", "اول", DateTimeOffset.UtcNow);
        await using (var setup = NewContext())
        {
            setup.Tenants.Add(seed);
            await setup.SaveChangesAsync();
        }
        await using var first = NewContext();
        await using var second = NewContext();
        var a = (await new EfTenantRepository(first).FindAsync(seed.Id, default))!;
        var b = (await new EfTenantRepository(second).FindAsync(seed.Id, default))!;
        a.Rename("برنده", DateTimeOffset.UtcNow);
        b.Rename("بازنده", DateTimeOffset.UtcNow);
        Assert.Equal(TenantCommitOutcome.Saved, await new EfTenantUnitOfWork(first).SaveChangesAsync(default));
        Assert.Equal(TenantCommitOutcome.ConcurrencyConflict, await new EfTenantUnitOfWork(second).SaveChangesAsync(default));
        await using var batch = NewContext();
        batch.Tenants.Add(TenantEntity.Create(Guid.NewGuid(), "new-shop", "تازه", DateTimeOffset.UtcNow));
        batch.Tenants.Add(TenantEntity.Create(Guid.NewGuid(), "seed-shop", "تکراری", DateTimeOffset.UtcNow));
        Assert.Equal(TenantCommitOutcome.DuplicateCode, await new EfTenantUnitOfWork(batch).SaveChangesAsync(default));
        await using var check = NewContext();
        var persisted = Assert.Single(await check.Tenants.ToListAsync());
        Assert.Equal("برنده", persisted.Name);
        Assert.Equal(2, persisted.Version);
    }

    [PostgresFact]
    public async Task Queries_are_untracked_filtered_and_paginated_without_mutation()
    {
        await using var db = NewContext();
        var a = TenantEntity.Create(Guid.NewGuid(), "shop-a", "الف", DateTimeOffset.UtcNow);
        var b = TenantEntity.Create(Guid.NewGuid(), "shop-b", "ب", DateTimeOffset.UtcNow);
        a.ChangeStatus(TenantStatus.Active, null, DateTimeOffset.UtcNow);
        db.Tenants.AddRange(a, b);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var reader = new EfTenantReader(db);
        Assert.Equal(a.Id, (await reader.GetAsync(a.Id, default))!.Id);
        var page = await reader.ListAsync(1, 1, TenantState.Active, default);
        Assert.Single(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty((await reader.ListAsync(2, 1, TenantState.Active, default)).Items);
    }

    [PostgresFact]
    public async Task Mapster_projection_matches_persisted_entities_in_every_status()
    {
        await using var db = NewContext();
        foreach (var state in new[] { TenantStatus.SettingUp, TenantStatus.Active, TenantStatus.Suspended })
        {
            var tenant = TenantEntity.Create(Guid.NewGuid(), $"map-{(int)state}", "فروشگاه", DateTimeOffset.UtcNow);
            if (state != TenantStatus.SettingUp) tenant.ChangeStatus(TenantStatus.Active, null, DateTimeOffset.UtcNow);
            if (state == TenantStatus.Suspended) tenant.ChangeStatus(state, "دلیل", DateTimeOffset.UtcNow);
            db.Tenants.Add(tenant);
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var reader = new EfTenantReader(db);
        var entities = await db.Tenants.AsNoTracking().ToListAsync();
        foreach (var entity in entities)
            Assert.Equal(TenantMapping.ToDto(entity), await reader.GetAsync(entity.Id, default));
        var sql = TenantProjection.ToDtos(db.Tenants.AsNoTracking()).ToQueryString();
        Assert.Contains("suspension_reason", sql);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [PostgresFact]
    public async Task Cancellation_is_propagated_by_repository_reader_and_unit_of_work()
    {
        await using var db = NewContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EfTenantRepository(db).CodeExistsAsync("shop", cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EfTenantReader(db).GetAsync(Guid.NewGuid(), cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EfTenantUnitOfWork(db).SaveChangesAsync(cts.Token));
    }

    // Test-only authentication. No such header or implementation exists in production code.
    public sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var actor = Request.Headers["X-Test-Actor"].ToString();
            if (actor is not ("admin" or "member")) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, actor) };
            if (actor == "admin") claims.Add(new Claim(TenantPresentation.PermissionClaim, TenantPresentation.ManagementPermission));
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
