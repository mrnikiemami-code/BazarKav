using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Contracts;
using MediatR;

namespace BazarKav.Tenant.Application.Queries.GetTenant;

public sealed class GetTenantQueryHandler(ITenantReader reader) : IRequestHandler<GetTenantQuery, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await reader.GetAsync(request.Id, cancellationToken);
        return tenant is null ? Result<TenantDto>.Failure(TenantErrors.NotFound) : Result<TenantDto>.Success(tenant);
    }
}
