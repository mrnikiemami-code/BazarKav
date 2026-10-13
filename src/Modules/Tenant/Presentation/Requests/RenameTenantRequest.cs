namespace BazarKav.Tenant.Presentation.Requests;

public sealed record RenameTenantRequest(string Name, long ExpectedVersion);
