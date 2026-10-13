using BazarKav.Tenant.Application.Mappings;
using BazarKav.Tenant.Contracts;
using Mapster;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.Tenant.Infrastructure.Persistence.Configurations;

public static class TenantProjection
{
    private static readonly TypeAdapterConfig Configuration = CreateConfiguration();

    private static TypeAdapterConfig CreateConfiguration()
    {
        var configuration = TenantMapping.CreateConfiguration();
        configuration.CompileProjection();
        return configuration;
    }

    public static void Validate() => _ = Configuration;
    public static IQueryable<TenantDto> ToDtos(IQueryable<TenantEntity> query) =>
        query.ProjectToType<TenantDto>(Configuration);
}
