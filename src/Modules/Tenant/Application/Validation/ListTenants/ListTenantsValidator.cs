using BazarKav.Tenant.Application.Queries.ListTenants;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.Policies;
using FluentValidation;

namespace BazarKav.Tenant.Application.Validation.ListTenants;

public sealed class ListTenantsValidator : AbstractValidator<ListTenantsQuery>
{
    public ListTenantsValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(status => status is null || Enum.IsDefined(status.Value));
    }
}
