using BazarKav.Tenant.Application.Commands.ChangeTenantStatus;
using BazarKav.Tenant.Application.Commands.CreateTenant;
using BazarKav.Tenant.Application.Commands.RenameTenant;
using BazarKav.Tenant.Presentation.Requests;
using Mapster;

namespace BazarKav.Tenant.Presentation.Mappings;

public static class TenantRequestMapping
{
    private sealed record RenameInput(Guid Id, RenameTenantRequest Request);
    private sealed record StatusInput(Guid Id, ChangeTenantStatusRequest Request);
    private static readonly TypeAdapterConfig Configuration = CreateConfiguration();

    private static TypeAdapterConfig CreateConfiguration()
    {
        var config = new TypeAdapterConfig
        {
            RequireExplicitMapping = true,
            RequireDestinationMemberSource = true
        };
        config.NewConfig<CreateTenantRequest, CreateTenantCommand>()
            .MapToConstructor(true)
            .Map(command => command.Code, request => request.Code)
            .Map(command => command.Name, request => request.Name);
        config.NewConfig<RenameInput, RenameTenantCommand>()
            .MapToConstructor(true)
            .Map(command => command.Id, input => input.Id)
            .Map(command => command.Name, input => input.Request.Name)
            .Map(command => command.ExpectedVersion, input => input.Request.ExpectedVersion);
        config.NewConfig<StatusInput, ChangeTenantStatusCommand>()
            .MapToConstructor(true)
            .Map(command => command.Id, input => input.Id)
            .Map(command => command.Status, input => input.Request.Status)
            .Map(command => command.Reason, input => input.Request.Reason)
            .Map(command => command.ExpectedVersion, input => input.Request.ExpectedVersion);
        config.Compile();
        return config;
    }

    public static void Validate() => _ = Configuration;
    public static CreateTenantCommand ToCommand(CreateTenantRequest request) =>
        request.Adapt<CreateTenantCommand>(Configuration);
    public static RenameTenantCommand ToCommand(Guid routeId, RenameTenantRequest request) =>
        new RenameInput(routeId, request).Adapt<RenameTenantCommand>(Configuration);
    public static ChangeTenantStatusCommand ToCommand(Guid routeId, ChangeTenantStatusRequest request) =>
        new StatusInput(routeId, request).Adapt<ChangeTenantStatusCommand>(Configuration);
}
