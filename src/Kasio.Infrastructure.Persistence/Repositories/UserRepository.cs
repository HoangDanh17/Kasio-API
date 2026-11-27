using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kasio.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(KasioDbContext dbContext) : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Remove(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Email được chuẩn hóa lowercase ở domain nên so sánh trực tiếp vẫn dùng được unique index.
        var normalized = EmailNormalizer.Normalize(email);

        return dbContext.Users
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<bool> ExistsRoleInUseAsync(RoleId roleId, CancellationToken cancellationToken = default)
        => dbContext.Users.AnyAsync(u => u.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyList<User>> GetPageAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        return await ApplySearch(dbContext.Users.AsNoTracking(), search)
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, CancellationToken cancellationToken = default)
        => ApplySearch(dbContext.Users.AsNoTracking(), search).CountAsync(cancellationToken);

    private static IQueryable<User> ApplySearch(IQueryable<User> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = $"%{search.Trim()}%";
        return query.Where(u => EF.Functions.ILike(u.FullName, term) || EF.Functions.ILike(u.Email, term));
    }
}
