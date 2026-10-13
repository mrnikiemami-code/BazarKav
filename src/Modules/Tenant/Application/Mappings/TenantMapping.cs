using BazarKav.Tenant.Contracts;
using Mapster;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.Tenant.Application.Mappings;

public static class TenantMapping
{
    private static readonly TypeAdapterConfig Configuration = CreateConfiguration();

    // Each caller receives an isolated configuration. GlobalSettings is never mutated.
    public static TypeAdapterConfig CreateConfiguration()
    {
        var config = new TypeAdapterConfig
        {
            RequireExplicitMapping = true,
            RequireDestinationMemberSource = true
        };
        config.NewConfig<TenantEntity, TenantDto>()
            .MapToConstructor(true)
            .Map(dto => dto.Id, tenant => tenant.Id)
            .Map(dto => dto.Code, tenant => tenant.Code)
            .Map(dto => dto.Name, tenant => tenant.Name)
            .Map(dto => dto.Status, tenant => (TenantState)tenant.Status)
            .Map(dto => dto.SuspensionReason, tenant => tenant.SuspensionReason)
            .Map(dto => dto.Version, tenant => tenant.Version)
            .Map(dto => dto.CreatedAt, tenant => tenant.CreatedAt)
            .Map(dto => dto.UpdatedAt, tenant => tenant.UpdatedAt);
        config.Compile();
        return config;
    }

    public static void Validate() => _ = Configuration;
    public static TenantDto ToDto(TenantEntity tenant) => tenant.Adapt<TenantDto>(Configuration);
}
