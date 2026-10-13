namespace BazarKav.Tenant.Domain.Errors;

public sealed record TenantRuleError(string Code, string Message)
{
    public static readonly TenantRuleError InvalidTransition =
        new("tenant.invalid_transition", "این تغییر وضعیت مجاز نیست.");
    public static readonly TenantRuleError SuspensionReasonRequired =
        new("tenant.suspension_reason_required", "دلیل تعلیق لازم است.");
}
