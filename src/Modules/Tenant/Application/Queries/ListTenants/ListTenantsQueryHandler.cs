using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Contracts;
using MediatR;

namespace BazarKav.Tenant.Application.Queries.ListTenants;

public sealed class ListTenantsQueryHandler(ITenantReader reader) : IRequestHandler<ListTenantsQuery, Result<TenantPage>>
{
    public async Task<Result<TenantPage>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        return Result<TenantPage>.Success(await reader.ListAsync(request.Page, request.PageSize, request.Status, cancellationToken));
    }
}
