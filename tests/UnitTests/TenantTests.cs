using BazarKav.BuildingBlocks.Application;
using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Application.Commands.CreateTenant;
using BazarKav.Tenant.Application.Commands.RenameTenant;
using BazarKav.Tenant.Application.Queries.ListTenants;
using BazarKav.Tenant.Application.Validation.ListTenants;
using BazarKav.Tenant.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.UnitTests;

public sealed class TenantTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-13T00:00:00Z");

    [Fact]
    public void Creation_has_stable_identity_initial_state_and_version()
    {
        var id = Guid.NewGuid();
        var tenant = TenantEntity.Create(id, "sample-shop", "  فروشگاه نمونه  ", Now);
        Assert.Equal(id, tenant.Id);
        Assert.Equal("فروشگاه نمونه", tenant.Name);
        Assert.Equal(TenantStatus.SettingUp, tenant.Status);
        Assert.Equal(1, tenant.Version);
        Assert.Equal(Now, tenant.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A-shop")]
    [InlineData("-shop")]
    [InlineData("a")]
    [InlineData("shop-")]
    [InlineData("shop name")]
    public void Invalid_codes_are_rejected_by_domain(string code) =>
        Assert.Throws<ArgumentException>(() => TenantEntity.Create(Guid.NewGuid(), code, "نام", Now));

    [Fact]
    public void Domain_rejects_invalid_names_and_identifiers()
    {
        Assert.Throws<ArgumentException>(() => TenantEntity.Create(Guid.Empty, "shop", "نام", Now));
        Assert.Throws<ArgumentException>(() => TenantEntity.Create(Guid.NewGuid(), "shop", "  ", Now));
        Assert.Throws<ArgumentException>(() => TenantEntity.Create(Guid.NewGuid(), "shop", new string('x', 201), Now));
    }

    [Fact]
    public void Lifecycle_validates_reason_and_preserves_identity()
    {
        var tenant = TenantEntity.Create(Guid.NewGuid(), "shop", "نام", Now);
        Assert.NotNull(tenant.ChangeStatus(TenantStatus.Suspended, "reason", Now));
        Assert.Equal(1, tenant.Version);
        Assert.Null(tenant.ChangeStatus(TenantStatus.Active, null, Now.AddSeconds(1)));
        Assert.NotNull(tenant.ChangeStatus(TenantStatus.Suspended, " ", Now.AddSeconds(2)));
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal(2, tenant.Version);
        Assert.Null(tenant.ChangeStatus(TenantStatus.Suspended, "  متوقف  ", Now.AddSeconds(2)));
        Assert.Equal("متوقف", tenant.SuspensionReason);
        Assert.Null(tenant.ChangeStatus(TenantStatus.Active, null, Now.AddSeconds(3)));
        Assert.Null(tenant.SuspensionReason);
        Assert.NotNull(tenant.ChangeStatus(TenantStatus.SettingUp, null, Now.AddSeconds(4)));
        Assert.Equal("shop", tenant.Code);
        Assert.Equal(4, tenant.Version);
    }

    [Fact]
    public void Backwards_clock_cannot_partially_mutate_entity()
    {
        var tenant = TenantEntity.Create(Guid.NewGuid(), "shop", "نام", Now);
        Assert.Throws<ArgumentOutOfRangeException>(() => tenant.Rename("نام جدید", Now.AddSeconds(-1)));
        Assert.Equal("نام", tenant.Name);
        Assert.Throws<ArgumentOutOfRangeException>(() => tenant.ChangeStatus(TenantStatus.Active, null, Now.AddSeconds(-1)));
        Assert.Equal(TenantStatus.SettingUp, tenant.Status);
        Assert.Equal(1, tenant.Version);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(1000001, 20)]
    public void Pagination_is_bounded(int page, int pageSize) =>
        Assert.False(new ListTenantsValidator().Validate(new ListTenantsQuery(page, pageSize)).IsValid);

    [Fact]
    public async Task Validation_rejects_bad_input_before_repository_is_called()
    {
        var repository = new FakeRepository();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBazarKavApplication(typeof(CreateTenantCommand).Assembly);
        services.AddSingleton<ITenantRepository>(repository);
        services.AddSingleton<ITenantUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton(TimeProvider.System);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<ValidationException>(() => scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateTenantCommand("INVALID", "نام")));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Stale_version_does_not_mutate_or_commit()
    {
        var repository = new FakeRepository { Entity = TenantEntity.Create(Guid.NewGuid(), "shop", "نام", Now) };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RenameTenantCommandHandler(repository, unitOfWork, TimeProvider.System);
        var result = await handler.Handle(new RenameTenantCommand(repository.Entity.Id, "تغییر", 2), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("tenant.version_conflict", result.Error.Code);
        Assert.Equal("نام", repository.Entity.Name);
        Assert.Equal(0, unitOfWork.Calls);
    }

    private sealed class FakeRepository : ITenantRepository
    {
        public int Calls { get; private set; }
        public TenantEntity? Entity { get; init; }
        public Task<TenantEntity?> FindAsync(Guid id, CancellationToken ct)
        { Calls++; return Task.FromResult(Entity); }
        public Task<bool> CodeExistsAsync(string code, CancellationToken ct)
        { Calls++; return Task.FromResult(false); }
        public void Add(TenantEntity tenant) { Calls++; }
    }
    private sealed class FakeUnitOfWork : ITenantUnitOfWork
    {
        public int Calls { get; private set; }
        public Task<TenantCommitOutcome> SaveChangesAsync(CancellationToken ct)
        { Calls++; return Task.FromResult(TenantCommitOutcome.Saved); }
    }
}
