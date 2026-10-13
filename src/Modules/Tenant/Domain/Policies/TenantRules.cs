namespace BazarKav.Tenant.Domain.Policies;

public static class TenantRules
{
    public const int CodeMaximumLength = 64;
    public const int NameMaximumLength = 200;
    public const int ReasonMaximumLength = 500;

    public static bool IsValidCode(string? code) =>
        code is { Length: >= 2 and <= CodeMaximumLength }
        && code[0] is >= 'a' and <= 'z'
        && code[^1] is not '-'
        && code.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= NameMaximumLength
        && !name.Any(char.IsControl);
}
