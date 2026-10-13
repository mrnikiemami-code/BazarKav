using BazarKav.Tenant.Application.Commands.ChangeTenantStatus;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.Policies;
using FluentValidation;

namespace BazarKav.Tenant.Application.Validation.ChangeTenantStatus;

public sealed class ChangeTenantStatusValidator : AbstractValidator<ChangeTenantStatusCommand>
{
    public ChangeTenantStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(TenantRules.ReasonMaximumLength)
            .Must(reason => reason is null || !reason.Any(char.IsControl))
            .When(x => x.Status == TenantState.Suspended);
        RuleFor(x => x.Reason).Empty().When(x => x.Status != TenantState.Suspended);
    }
}
