using BazarKav.Tenant.Application.Commands.RenameTenant;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.Policies;
using FluentValidation;

namespace BazarKav.Tenant.Application.Validation.RenameTenant;

public sealed class RenameTenantValidator : AbstractValidator<RenameTenantCommand>
{
    public RenameTenantValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).Must(TenantRules.IsValidName);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
