using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.ValueObjects;
using BazarKav.Tenant.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BazarKav.Tenant.Infrastructure.Persistence;

public sealed class EfTenantReader(TenantDbContext context) : ITenantReader
{
    public Task<TenantDto?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        TenantProjection.ToDtos(context.Tenants.AsNoTracking().Where(tenant => tenant.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TenantPage> ListAsync(int page, int pageSize, TenantState? status, CancellationToken cancellationToken)
    {
        var query = context.Tenants.AsNoTracking();
        if (status.HasValue) query = query.Where(tenant => tenant.Status == (TenantStatus)status.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await TenantProjection.ToDtos(query.OrderBy(tenant => tenant.CreatedAt).ThenBy(tenant => tenant.Id)
            .Skip(checked((page - 1) * pageSize)).Take(pageSize)).ToListAsync(cancellationToken);
        return new TenantPage(items, page, pageSize, count);
    }
}
