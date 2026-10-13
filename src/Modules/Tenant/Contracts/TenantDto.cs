namespace BazarKav.Tenant.Contracts;

public enum TenantState { SettingUp = 0, Active = 1, Suspended = 2 }

public sealed record TenantDto(Guid Id, string Code, string Name, TenantState Status,
    string? SuspensionReason, long Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record TenantPage(IReadOnlyList<TenantDto> Items, int Page, int PageSize, int TotalCount);
