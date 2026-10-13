using BazarKav.Tenant.Contracts;

namespace BazarKav.Tenant.Presentation.Requests;

public sealed record ChangeTenantStatusRequest(TenantState Status, string? Reason, long ExpectedVersion);
