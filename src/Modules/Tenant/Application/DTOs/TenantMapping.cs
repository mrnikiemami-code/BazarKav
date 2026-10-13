using BazarKav.Tenant.Contracts;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.Tenant.Application.DTOs;

public static class TenantMapping
{
    public static TenantDto ToDto(TenantEntity tenant) => new(tenant.Id, tenant.Code, tenant.Name,
        (TenantState)tenant.Status, tenant.SuspensionReason, tenant.Version, tenant.CreatedAt, tenant.UpdatedAt);
}
