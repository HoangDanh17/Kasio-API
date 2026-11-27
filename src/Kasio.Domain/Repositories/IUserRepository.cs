using Kasio.Domain.Roles;
using Kasio.Domain.Users;

namespace Kasio.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetPageAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? search, CancellationToken cancellationToken = default);
    Task<bool> ExistsRoleInUseAsync(RoleId roleId, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
}
