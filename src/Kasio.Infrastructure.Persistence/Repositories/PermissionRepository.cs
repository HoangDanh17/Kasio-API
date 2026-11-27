using Kasio.Domain.Permissions;
using Kasio.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kasio.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository(KasioDbContext dbContext) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(IReadOnlyList<PermissionId> ids, CancellationToken cancellationToken)
    {
        return await dbContext.Permissions
        .Where(p => ids.Contains(p.Id))
        .ToListAsync(cancellationToken);
    }

    public Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken = default)
    {
        return dbContext.Permissions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
