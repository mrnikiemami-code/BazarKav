using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Contracts;

namespace BazarKav.Tenant.Application.Commands.CreateTenant;

public sealed record CreateTenantCommand(string Code, string Name) : ICommand<Result<TenantDto>>;
