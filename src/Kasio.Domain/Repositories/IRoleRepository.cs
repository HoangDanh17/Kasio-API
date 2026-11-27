using Kasio.Domain.Roles;

namespace Kasio.Domain.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    Task DeleteAsync(Role role, CancellationToken cancellationToken = default);
}
