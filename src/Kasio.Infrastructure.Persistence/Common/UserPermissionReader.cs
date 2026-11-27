using Kasio.Application.Common.Interfaces;
using Kasio.Contracts.User;
using Kasio.Domain.Permissions;
using Kasio.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kasio.Infrastructure.Persistence.Common;

public sealed class UserPermissionReader(KasioDbContext db) : IUserPermissionReader
{
    private static readonly IReadOnlySet<string> NoPermissions = new HashSet<string>(StringComparer.Ordinal);

    public async Task<IReadOnlyList<UserPermissionStateResponse>?> GetStatesAsync(
        UserId userId, CancellationToken cancellationToken = default)
    {
        var (states, _) = await LoadAsync(userId, cancellationToken);
        return states;
    }

    public async Task<IReadOnlySet<string>> GetEffectivePermissionCodesAsync(
        UserId userId, CancellationToken cancellationToken = default)
    {
        var (states, isActive) = await LoadAsync(userId, cancellationToken);

        // Tài khoản bị khóa mất hiệu lực ngay cả khi access token còn hạn (BRD 3.4, AC-RBAC-03).
        if (states is null || !isActive)
            return NoPermissions;

        return states
            .Where(state => state.HasPermission)
            .Select(state => state.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<(IReadOnlyList<UserPermissionStateResponse>? States, bool IsActive)> LoadAsync(
        UserId userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.RoleId, u.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return (null, false);

        var all = await db.Permissions.AsNoTracking()
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);

        var rolePermissionIds = (await db.Roles.AsNoTracking()
                .Where(r => r.Id == user.RoleId)
                .SelectMany(r => r.Permissions)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var overrides = (await db.Set<UserPermission>().AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => new { up.PermissionId, up.IsGranted })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.PermissionId, x => x.IsGranted);

        var states = all
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p =>
            {
                var fromRole = rolePermissionIds.Contains(p.Id);
                var hasOverride = overrides.TryGetValue(p.Id, out var isGranted);

                var setting = !hasOverride ? "Inherit" : isGranted ? "Allow" : "Deny";
                var hasPermission = hasOverride ? isGranted : fromRole;

                return new UserPermissionStateResponse(
                    p.Id.Value,
                    p.Name,
                    PermissionCatalog.DisplayNames.GetValueOrDefault(p.Name, p.Name),
                    fromRole,
                    setting,
                    hasPermission);
            })
            .ToList();

        return (states, user.IsActive);
    }
}
