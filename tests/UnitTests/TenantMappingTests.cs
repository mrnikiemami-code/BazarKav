using BazarKav.Tenant.Application.Mappings;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.ValueObjects;
using BazarKav.Tenant.Presentation.Mappings;
using BazarKav.Tenant.Presentation.Requests;
using Mapster;
using Xunit;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.UnitTests;

public sealed class TenantMappingTests
{
    [Theory]
    [InlineData(TenantStatus.SettingUp)]
    [InlineData(TenantStatus.Active)]
    [InlineData(TenantStatus.Suspended)]
    public void Entity_mapping_preserves_every_field_and_status(TenantStatus status)
    {
        var now = DateTimeOffset.Parse("2026-10-13T00:00:00Z");
        var tenant = TenantEntity.Create(Guid.NewGuid(), "map-shop", "فروشگاه", now);
        if (status != TenantStatus.SettingUp) tenant.ChangeStatus(TenantStatus.Active, null, now.AddMinutes(1));
        if (status == TenantStatus.Suspended) tenant.ChangeStatus(status, "تعلیق موقت", now.AddMinutes(2));
        var expected = new TenantDto(tenant.Id, tenant.Code, tenant.Name, (TenantState)status,
            tenant.SuspensionReason, tenant.Version, tenant.CreatedAt, tenant.UpdatedAt);
        Assert.Equal(expected, TenantMapping.ToDto(tenant));
    }

    [Fact]
    public void Request_mapping_preserves_input_and_takes_identifiers_from_the_route()
    {
        var routeId = Guid.NewGuid();
        var create = TenantRequestMapping.ToCommand(new CreateTenantRequest("shop", "نام"));
        Assert.Equal("shop", create.Code);
        Assert.Equal("نام", create.Name);
        var rename = TenantRequestMapping.ToCommand(routeId, new RenameTenantRequest("جدید", 13));
        Assert.Equal(routeId, rename.Id);
        Assert.Equal("جدید", rename.Name);
        Assert.Equal(13, rename.ExpectedVersion);
        var change = TenantRequestMapping.ToCommand(routeId,
            new ChangeTenantStatusRequest(TenantState.Suspended, "دلیل", 14));
        Assert.Equal(routeId, change.Id);
        Assert.Equal(TenantState.Suspended, change.Status);
        Assert.Equal("دلیل", change.Reason);
        Assert.Equal(14, change.ExpectedVersion);
    }

    [Fact]
    public void Unregistered_mapping_is_rejected_and_configurations_are_isolated()
    {
        var first = TenantMapping.CreateConfiguration();
        var second = TenantMapping.CreateConfiguration();
        Assert.NotSame(first, second);
        Assert.True(first.RequireExplicitMapping);
        Assert.True(first.RequireDestinationMemberSource);
        var tenant = TenantEntity.Create(Guid.NewGuid(), "map-shop", "نام", DateTimeOffset.UtcNow);
        Assert.Throws<CompileException>(() => tenant.Adapt<UnregisteredDto>(first));
        Assert.Equal(tenant.Id, tenant.Adapt<TenantDto>(second).Id);
    }

    public sealed record UnregisteredDto(Guid Id);
}
