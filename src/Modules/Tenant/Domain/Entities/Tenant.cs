using BazarKav.Tenant.Domain.Errors;
using BazarKav.Tenant.Domain.Policies;
using BazarKav.Tenant.Domain.ValueObjects;

namespace BazarKav.Tenant.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public TenantStatus Status { get; private set; }
    public string? SuspensionReason { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Tenant() { }

    public static Tenant Create(Guid id, string code, string name, DateTimeOffset now)
    {
        if (id == Guid.Empty) throw new ArgumentException("Tenant identifier is required.", nameof(id));
        if (!TenantRules.IsValidCode(code)) throw new ArgumentException("Invalid tenant code.", nameof(code));
        if (!TenantRules.IsValidName(name)) throw new ArgumentException("Invalid tenant name.", nameof(name));
        return new Tenant
        {
            Id = id, Code = code, Name = name.Trim(), Status = TenantStatus.SettingUp,
            Version = 1, CreatedAt = now.ToUniversalTime(), UpdatedAt = now.ToUniversalTime()
        };
    }

    public void Rename(string name, DateTimeOffset now)
    {
        if (!TenantRules.IsValidName(name)) throw new ArgumentException("Invalid tenant name.", nameof(name));
        Touch(now);
        Name = name.Trim();
    }

    public TenantRuleError? ChangeStatus(TenantStatus target, string? reason, DateTimeOffset now)
    {
        var allowed = (Status, target) is
            (TenantStatus.SettingUp, TenantStatus.Active) or
            (TenantStatus.Active, TenantStatus.Suspended) or
            (TenantStatus.Suspended, TenantStatus.Active);
        if (!allowed) return TenantRuleError.InvalidTransition;
        if (target == TenantStatus.Suspended &&
            (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > TenantRules.ReasonMaximumLength
             || reason.Any(char.IsControl)))
            return TenantRuleError.SuspensionReasonRequired;
        Touch(now);
        Status = target;
        SuspensionReason = target == TenantStatus.Suspended ? reason!.Trim() : null;
        return null;
    }

    private void Touch(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        if (utc < UpdatedAt) throw new ArgumentOutOfRangeException(nameof(now), "Time cannot move backwards.");
        var nextVersion = checked(Version + 1);
        UpdatedAt = utc;
        Version = nextVersion;
    }
}
