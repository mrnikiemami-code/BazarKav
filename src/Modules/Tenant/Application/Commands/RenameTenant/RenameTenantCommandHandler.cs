using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Application.DTOs;
using BazarKav.Tenant.Contracts;
using MediatR;

namespace BazarKav.Tenant.Application.Commands.RenameTenant;

public sealed class RenameTenantCommandHandler(ITenantRepository repository, ITenantUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RenameTenantCommand, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(RenameTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await repository.FindAsync(request.Id, cancellationToken);
        if (tenant is null) return Result<TenantDto>.Failure(TenantErrors.NotFound);
        if (tenant.Version != request.ExpectedVersion) return Result<TenantDto>.Failure(TenantErrors.Concurrency);
        tenant.Rename(request.Name, timeProvider.GetUtcNow());
        var outcome = await unitOfWork.SaveChangesAsync(cancellationToken);
        return outcome == TenantCommitOutcome.Saved
            ? Result<TenantDto>.Success(TenantMapping.ToDto(tenant))
            : Result<TenantDto>.Failure(TenantErrors.FromCommit(outcome));
    }
}
