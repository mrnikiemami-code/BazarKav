using BazarKav.Tenant.Contracts;

namespace BazarKav.Tenant.Application.Abstractions;

public interface ITenantReader
{
    Task<TenantDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<TenantPage> ListAsync(int page, int pageSize, TenantState? status, CancellationToken cancellationToken);
}
