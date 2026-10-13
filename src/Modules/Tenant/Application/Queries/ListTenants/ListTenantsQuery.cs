using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Contracts;

namespace BazarKav.Tenant.Application.Queries.ListTenants;

public sealed record ListTenantsQuery(int Page = 1, int PageSize = 20, TenantState? Status = null) : IQuery<Result<TenantPage>>;
