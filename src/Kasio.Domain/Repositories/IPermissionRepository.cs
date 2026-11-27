using Kasio.Domain.Permissions;

namespace Kasio.Domain.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetByIdsAsync(IReadOnlyList<PermissionId> ids, CancellationToken cancellationToken);
}
