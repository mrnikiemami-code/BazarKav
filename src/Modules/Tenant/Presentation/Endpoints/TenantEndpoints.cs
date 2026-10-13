using BazarKav.BuildingBlocks.Web.Errors;
using BazarKav.Tenant.Application.Commands.ChangeTenantStatus;
using BazarKav.Tenant.Application.Commands.CreateTenant;
using BazarKav.Tenant.Application.Commands.RenameTenant;
using BazarKav.Tenant.Application.Queries.GetTenant;
using BazarKav.Tenant.Application.Queries.ListTenants;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Presentation.Requests;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BazarKav.Tenant.Presentation.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tenants")
            .WithTags("Tenant").RequireAuthorization(TenantPresentation.ManagementPolicy);

        group.MapPost("/", async (CreateTenantRequest request, ISender sender, HttpContext context, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateTenantCommand(request.Code, request.Name), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/tenants/{result.Value.Id}", result.Value)
                : result.ToHttpResult(context);
        }).WithName("Tenant.Create");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, HttpContext context, CancellationToken ct) =>
            (await sender.Send(new GetTenantQuery(id), ct)).ToHttpResult(context)).WithName("Tenant.Get");

        group.MapGet("/", async (int? page, int? pageSize, TenantState? status,
            ISender sender, HttpContext context, CancellationToken ct) =>
            (await sender.Send(new ListTenantsQuery(page ?? 1, pageSize ?? 20, status), ct))
                .ToHttpResult(context)).WithName("Tenant.List");

        group.MapPut("/{id:guid}/name", async (Guid id, RenameTenantRequest request,
            ISender sender, HttpContext context, CancellationToken ct) =>
            (await sender.Send(new RenameTenantCommand(id, request.Name, request.ExpectedVersion), ct))
                .ToHttpResult(context)).WithName("Tenant.Rename");

        group.MapPut("/{id:guid}/status", async (Guid id, ChangeTenantStatusRequest request,
            ISender sender, HttpContext context, CancellationToken ct) =>
            (await sender.Send(new ChangeTenantStatusCommand(id, request.Status, request.Reason, request.ExpectedVersion), ct))
                .ToHttpResult(context)).WithName("Tenant.ChangeStatus");
        return endpoints;
    }
}
