using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Application.DTOs;
using BazarKav.Tenant.Contracts;
using MediatR;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.Tenant.Application.Commands.CreateTenant;

public sealed class CreateTenantCommandHandler(ITenantRepository repository, ITenantUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (await repository.CodeExistsAsync(request.Code, cancellationToken))
            return Result<TenantDto>.Failure(TenantErrors.DuplicateCode);
        var tenant = TenantEntity.Create(Guid.NewGuid(), request.Code, request.Name, timeProvider.GetUtcNow());
        repository.Add(tenant);
        var outcome = await unitOfWork.SaveChangesAsync(cancellationToken);
        return outcome == TenantCommitOutcome.Saved
            ? Result<TenantDto>.Success(TenantMapping.ToDto(tenant))
            : Result<TenantDto>.Failure(TenantErrors.FromCommit(outcome));
    }
}
