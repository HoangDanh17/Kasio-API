using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Kasio.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(KasioDbContext dbContext) : IRoleRepository
{
    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        dbContext.Roles.Add(role);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        dbContext.Roles.Remove(role);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Include(r => r.Permissions)
            .ToListAsync(cancellationToken);
    }

    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        return dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return dbContext.Roles
         .Include(r => r.Permissions)
         .FirstOrDefaultAsync(r => r.Name == name.Trim(), cancellationToken);
    }
}
