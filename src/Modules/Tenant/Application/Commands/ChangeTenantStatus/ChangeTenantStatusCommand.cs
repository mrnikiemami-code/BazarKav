using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Contracts;

namespace BazarKav.Tenant.Application.Commands.ChangeTenantStatus;

public sealed record ChangeTenantStatusCommand(Guid Id, TenantState Status, string? Reason, long ExpectedVersion)
    : ICommand<Result<TenantDto>>;
