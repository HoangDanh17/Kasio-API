using ErrorOr;
using Kasio.Application.Common.Interfaces;
using Kasio.Domain.Permissions;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;

namespace Kasio.Application.Roles;

/// <summary>Kiểm tra một tập quyền có được phép gán vào role tùy chỉnh hay không.</summary>
internal static class RolePermissionValidator
{
    public static async Task<ErrorOr<Success>> ValidateAsync(
        User actor,
        IReadOnlyList<Permission> permissions,
        IUserPermissionReader permissionReader,
        CancellationToken cancellationToken)
    {
        var actorPermissions = await permissionReader.GetEffectivePermissionCodesAsync(actor.Id, cancellationToken);

        foreach (var permission in permissions)
        {
            switch (PermissionGrantPolicy.Check(permission.Name, actorPermissions))
            {
                case PermissionGrantViolation.PlatformPermission:
                    return RoleErrors.PlatformPermissionNotAllowed;
                case PermissionGrantViolation.OwnerOnlyPermission:
                    return RoleErrors.OwnerOnlyPermission;
                case PermissionGrantViolation.NotHeld:
                    return RoleErrors.ForbiddenPermissionGrant;
            }
        }

        return Result.Success;
    }
}
