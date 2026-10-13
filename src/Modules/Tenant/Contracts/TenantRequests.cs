namespace BazarKav.Tenant.Contracts;

public sealed record CreateTenantRequest(string Code, string Name);
public sealed record RenameTenantRequest(string Name, long ExpectedVersion);
public sealed record ChangeTenantStatusRequest(TenantState Status, string? Reason, long ExpectedVersion);
