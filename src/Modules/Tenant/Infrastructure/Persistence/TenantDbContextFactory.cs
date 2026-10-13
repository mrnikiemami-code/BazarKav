using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BazarKav.Tenant.Infrastructure.Persistence;

public sealed class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("BAZARKAV_TENANT_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set BAZARKAV_TENANT_CONNECTION_STRING for migration tooling.");
        return new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connection).Options);
    }
}
