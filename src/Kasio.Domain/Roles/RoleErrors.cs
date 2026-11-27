using ErrorOr;

namespace Kasio.Domain.Roles;

public static class RoleErrors
{
    public static Error InvalidName => Error.Validation(
        code: "Role.InvalidName",
        description: "Tên role không được để trống");

    public static Error NotFound => Error.NotFound(
        code: "Role.NotFound",
        description: "Role không tồn tại");

    public static Error AlreadyExists => Error.Conflict(
        code: "Role.AlreadyExists",
        description: "Role đã tồn tại");

    public static Error RoleInUse => Error.Conflict(
        code: "Role.RoleInUse",
        description: "Role đang được sử dụng bởi người dùng khác");

    public static Error PermissionAlreadyGranted => Error.Conflict(
        code: "Role.PermissionAlreadyGranted",
        description: "Quyền đã được cấp cho role này");

    public static Error PermissionNotGranted => Error.NotFound(
        code: "Role.PermissionNotGranted",
        description: "Quyền chưa được cấp cho role này");
    public static Error SomePermissionsNotFound => Error.NotFound(
        code: "Role.SomePermissionsNotFound",
        description: "Một số quyền không tồn tại");

    public static Error SystemRole => Error.Conflict(
        code: "Role.SystemRole",
        description: "Role hệ thống không thể sửa hoặc xóa.");

    // Quy tắc cấp quyền (BRD: BR-44, BR-48, mục 10)
    public static Error OwnerOnly => Error.Forbidden(
        code: "Role.OwnerOnly",
        description: "Chỉ Owner mới được tạo hoặc sửa role của tenant.");

    public static Error ForbiddenPermissionGrant => Error.Forbidden(
        code: "Role.ForbiddenPermissionGrant",
        description: "Chỉ được cấp quyền mà bạn đang có.");

    public static Error OwnerOnlyPermission => Error.Forbidden(
        code: "Role.OwnerOnlyPermission",
        description: "staff.manage và tenant.settings.write chỉ thuộc Owner, không cấp qua role tùy chỉnh.");

    public static Error PlatformPermissionNotAllowed => Error.Forbidden(
        code: "Role.PlatformPermissionNotAllowed",
        description: "Quyền nền tảng không thể gán cho role tenant.");
}
