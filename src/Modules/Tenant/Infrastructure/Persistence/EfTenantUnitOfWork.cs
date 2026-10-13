using BazarKav.Tenant.Application.Abstractions;
using BazarKav.Tenant.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BazarKav.Tenant.Infrastructure.Persistence;

public sealed class EfTenantUnitOfWork(TenantDbContext context) : ITenantUnitOfWork
{
    public async Task<TenantCommitOutcome> SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            // EF Core owns the single SaveChanges transaction. Repositories never commit.
            await context.SaveChangesAsync(cancellationToken);
            return TenantCommitOutcome.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return TenantCommitOutcome.ConcurrencyConflict;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: TenantConfiguration.CodeIndex })
        {
            context.ChangeTracker.Clear();
            return TenantCommitOutcome.DuplicateCode;
        }
        // All other failures and cancellation propagate to the owning boundary.
    }
}
