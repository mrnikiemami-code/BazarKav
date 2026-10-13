using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.Tenant.Application.Abstractions;

public interface ITenantRepository
{
    Task<TenantEntity?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);
    void Add(TenantEntity tenant);
}
