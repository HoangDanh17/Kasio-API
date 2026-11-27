namespace Kasio.Domain.Permissions;

/// <summary>Lý do một lần cấp/thu hồi quyền bị từ chối (BRD: BR-44, BR-48, mục 10).</summary>
public enum PermissionGrantViolation
{
    None,
    PlatformPermission,
    OwnerOnlyPermission,
    NotHeld
}
