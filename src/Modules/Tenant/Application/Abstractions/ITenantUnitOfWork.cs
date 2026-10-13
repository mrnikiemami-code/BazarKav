namespace BazarKav.Tenant.Application.Abstractions;

public enum TenantCommitOutcome { Saved, DuplicateCode, ConcurrencyConflict }

public interface ITenantUnitOfWork
{
    Task<TenantCommitOutcome> SaveChangesAsync(CancellationToken cancellationToken);
}
