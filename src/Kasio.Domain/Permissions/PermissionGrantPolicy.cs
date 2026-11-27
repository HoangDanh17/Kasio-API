namespace Kasio.Domain.Permissions;

/// <summary>
/// Quy tắc cấp quyền theo BRD:
/// BR-44 chỉ cấp quyền mình đang có; BR-48 staff.manage/tenant.settings.write chỉ thuộc Owner;
/// mục 10 không gán quyền nền tảng cho role/tenant.
/// </summary>
public static class PermissionGrantPolicy
{
    public static PermissionGrantViolation Check(string permissionName, IReadOnlySet<string> actorPermissions)
    {
        if (PermissionCatalog.IsPlatform(permissionName))
            return PermissionGrantViolation.PlatformPermission;

        if (PermissionCatalog.OwnerOnly.Contains(permissionName))
            return PermissionGrantViolation.OwnerOnlyPermission;

        return actorPermissions.Contains(permissionName)
            ? PermissionGrantViolation.None
            : PermissionGrantViolation.NotHeld;
    }
}
