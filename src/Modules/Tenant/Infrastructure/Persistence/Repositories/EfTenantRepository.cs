using BazarKav.Tenant.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.Tenant.Infrastructure.Persistence.Repositories;

public sealed class EfTenantRepository(TenantDbContext context) : ITenantRepository
{
    public Task<TenantEntity?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Tenants.SingleOrDefaultAsync(tenant => tenant.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        context.Tenants.AnyAsync(tenant => tenant.Code == code, cancellationToken);

    public void Add(TenantEntity tenant) => context.Tenants.Add(tenant);
}
