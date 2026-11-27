using ErrorOr;

namespace Kasio.Domain.Permissions;

public static class PermissionErrors
{
    public static Error NotFound => Error.NotFound(
        code: "Permission.NotFound",
        description: "Không tìm thấy quyền.");

    public static Error InvalidId => Error.Validation(
        code: "Permission.InvalidId",
        description: "ID quyền không hợp lệ.");

    public static Error InvalidName => Error.Validation(
        code: "Permission.InvalidName",
        description: "Mã quyền không được để trống.");

    public static Error InvalidGroup => Error.Validation(
        code: "Permission.InvalidGroup",
        description: "Nhóm quyền không được để trống.");
}
