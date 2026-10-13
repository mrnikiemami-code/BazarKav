using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Application.DTOs;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Domain.ValueObjects;
using MediatR;

namespace BazarKav.Tenant.Application.Commands.ChangeTenantStatus;

public sealed class ChangeTenantStatusCommandHandler(ITenantRepository repository, ITenantUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ChangeTenantStatusCommand, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(ChangeTenantStatusCommand request, CancellationToken cancellationToken)
    {
        var tenant = await repository.FindAsync(request.Id, cancellationToken);
        if (tenant is null) return Result<TenantDto>.Failure(TenantErrors.NotFound);
        if (tenant.Version != request.ExpectedVersion) return Result<TenantDto>.Failure(TenantErrors.Concurrency);
        var failure = tenant.ChangeStatus((TenantStatus)request.Status, request.Reason, timeProvider.GetUtcNow());
        if (failure is not null)
            return Result<TenantDto>.Failure(new Error(failure.Code, failure.Message, ErrorKind.Conflict));
        var outcome = await unitOfWork.SaveChangesAsync(cancellationToken);
        return outcome == TenantCommitOutcome.Saved
            ? Result<TenantDto>.Success(TenantMapping.ToDto(tenant))
            : Result<TenantDto>.Failure(TenantErrors.FromCommit(outcome));
    }
}
