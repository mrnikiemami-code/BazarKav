using BazarKav.Tenant.Application.Commands.CreateTenant;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.Policies;
using FluentValidation;

namespace BazarKav.Tenant.Application.Validation.CreateTenant;

public sealed class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantValidator()
    {
        RuleFor(x => x.Code).Must(TenantRules.IsValidCode);
        RuleFor(x => x.Name).Must(TenantRules.IsValidName);
    }
}
