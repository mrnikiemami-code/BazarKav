using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Infrastructure.Persistence;
using BazarKav.Tenant.Infrastructure.Persistence.Repositories;
using BazarKav.Tenant.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BazarKav.Tenant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        TenantProjection.Validate();
        services.AddDbContext<TenantDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITenantRepository, EfTenantRepository>();
        services.AddScoped<ITenantReader, EfTenantReader>();
        services.AddScoped<ITenantUnitOfWork, EfTenantUnitOfWork>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }

    public static async Task MigrateTenantDatabaseAsync(this IServiceProvider provider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TenantDbContext>().Database.MigrateAsync(cancellationToken);
    }
}
