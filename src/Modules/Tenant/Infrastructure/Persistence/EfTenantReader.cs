using System.Linq.Expressions;
using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.Tenant.Infrastructure.Persistence;

public sealed class EfTenantReader(TenantDbContext context) : ITenantReader
{
    private static readonly Expression<Func<TenantEntity, TenantDto>> Projection = tenant =>
        new TenantDto(tenant.Id, tenant.Code, tenant.Name, (TenantState)tenant.Status,
            tenant.SuspensionReason, tenant.Version, tenant.CreatedAt, tenant.UpdatedAt);

    public Task<TenantDto?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Tenants.AsNoTracking().Where(tenant => tenant.Id == id)
            .Select(Projection).SingleOrDefaultAsync(cancellationToken);

    public async Task<TenantPage> ListAsync(int page, int pageSize, TenantState? status, CancellationToken cancellationToken)
    {
        var query = context.Tenants.AsNoTracking();
        if (status.HasValue) query = query.Where(tenant => tenant.Status == (TenantStatus)status.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(tenant => tenant.CreatedAt).ThenBy(tenant => tenant.Id)
            .Skip(checked((page - 1) * pageSize)).Take(pageSize).Select(Projection).ToListAsync(cancellationToken);
        return new TenantPage(items, page, pageSize, count);
    }
}
