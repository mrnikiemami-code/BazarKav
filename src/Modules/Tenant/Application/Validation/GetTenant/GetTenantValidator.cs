using BazarKav.Tenant.Application.Queries.GetTenant;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.Policies;
using FluentValidation;

namespace BazarKav.Tenant.Application.Validation.GetTenant;

public sealed class GetTenantValidator : AbstractValidator<GetTenantQuery>
{
    public GetTenantValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
