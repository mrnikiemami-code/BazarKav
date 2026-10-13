using System.Reflection;
using BazarKav.Tenant.Application.Commands.CreateTenant;
using BazarKav.Tenant.Application.Mappings;
using BazarKav.Tenant.Presentation.Mappings;
using Microsoft.Extensions.DependencyInjection;

namespace BazarKav.Tenant.Presentation;

public static class TenantPresentation
{
    public const string ManagementPolicy = "TenantManagement";
    public const string PermissionClaim = "bazarkav.permission";
    public const string ManagementPermission = "tenant.manage";
    public static Assembly ApplicationAssembly => typeof(CreateTenantCommand).Assembly;

    public static IServiceCollection AddTenantPresentation(this IServiceCollection services)
    {
        TenantMapping.Validate();
        TenantRequestMapping.Validate();
        services.AddAuthorization(options => options.AddPolicy(ManagementPolicy, policy =>
            policy.RequireAuthenticatedUser().RequireClaim(PermissionClaim, ManagementPermission)));
        return services;
    }
}
