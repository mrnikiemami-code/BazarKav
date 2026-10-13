using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Contracts;

namespace BazarKav.Tenant.Application.Commands.RenameTenant;

public sealed record RenameTenantCommand(Guid Id, string Name, long ExpectedVersion) : ICommand<Result<TenantDto>>;
