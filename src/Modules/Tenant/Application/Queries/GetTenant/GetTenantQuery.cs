using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Contracts;

namespace BazarKav.Tenant.Application.Queries.GetTenant;

public sealed record GetTenantQuery(Guid Id) : IQuery<Result<TenantDto>>;
