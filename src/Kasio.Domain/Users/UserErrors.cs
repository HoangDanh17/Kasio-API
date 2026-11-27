using ErrorOr;

namespace Kasio.Domain.Users;

public static class UserErrors
{
    public static Error InvalidFullName => Error.Validation(
        code: "User.InvalidFullName",
        description: "Tên người dùng không được để trống.");

    public static Error InvalidEmail => Error.Validation(
        code: "User.InvalidEmail",
        description: "Email không hợp lệ.");

    public static Error DuplicateEmail => Error.Conflict(
       code: "User.DuplicateEmail",
       description: "Email này đã được sử dụng trên hệ thống.");

    public static Error InvalidPasswordHash => Error.Validation(
        code: "User.InvalidPasswordHash",
        description: "Mật khẩu không hợp lệ.");

    public static Error InvalidPhoneNumber => Error.Validation(
        code: "User.InvalidPhoneNumber",
        description: "Số điện thoại không hợp lệ.");

    public static Error InvalidAddress => Error.Validation(
        code: "User.InvalidAddress",
        description: "Địa chỉ không hợp lệ.");

    public static Error NotFound => Error.NotFound(
        code: "User.NotFound",
        description: "Người dùng không tồn tại.");

    public static Error InvalidRoleId => Error.Validation(
       code: "User.InvalidRoleId",
       description: "Vai trò không hợp lệ.");

    public static Error NotFoundPermission => Error.NotFound(
        code: "UserPermission.NotFound",
        description: "Người dùng không có quyền này.");

    // Auth — Unauthorized để API trả 401, không lẫn với lỗi dữ liệu 400
    public static Error InvalidCredentials => Error.Unauthorized(
        code: "User.InvalidCredentials",
        description: "Email hoặc mật khẩu không đúng.");

    public static Error InvalidRefreshToken => Error.Unauthorized(
        code: "User.InvalidRefreshToken",
        description: "Refresh token không hợp lệ.");

    public static Error AccountDisabled => Error.Unauthorized(
        code: "User.AccountDisabled",
        description: "Tài khoản đã bị khóa.");

    public static Error CannotChangeOwnRole => Error.Conflict(
        code: "User.CannotChangeOwnRole",
        description: "Bạn không thể thay đổi vai trò của chính mình.");

    // Phân quyền (BRD: BR-44, BR-48, mục 10)
    public static Error ActorNotFound => Error.Unauthorized(
        code: "User.ActorNotFound",
        description: "Không xác định được người thực hiện.");

    public static Error CannotCreateRole => Error.Forbidden(
        code: "User.CannotCreateRole",
        description: "Bạn không được tạo tài khoản với vai trò này.");

    public static Error CannotAssignRole => Error.Forbidden(
        code: "User.CannotAssignRole",
        description: "Bạn không được gán vai trò này.");

    public static Error CannotManageOwnerAccount => Error.Forbidden(
        code: "User.CannotManageOwnerAccount",
        description: "Không thể thay đổi vai trò hoặc quyền của Owner.");

    public static Error CannotManageAdminAccount => Error.Forbidden(
        code: "User.CannotManageAdminAccount",
        description: "Không thể thay đổi tài khoản Platform Admin qua API tenant.");

    public static Error CannotManageAccount => Error.Forbidden(
        code: "User.CannotManageAccount",
        description: "Bạn không được quản lý tài khoản có vai trò này.");

    public static Error CannotDeactivateSelf => Error.Conflict(
        code: "User.CannotDeactivateSelf",
        description: "Bạn không thể tự khóa tài khoản của mình.");

    public static Error ForbiddenPermissionGrant => Error.Forbidden(
        code: "User.ForbiddenPermissionGrant",
        description: "Chỉ được cấp hoặc thu hồi quyền mà bạn đang có.");

    public static Error OwnerOnlyPermission => Error.Forbidden(
        code: "User.OwnerOnlyPermission",
        description: "Chỉ Owner mới được có quyền staff.manage và tenant.settings.write.");

    public static Error PlatformPermissionNotAllowed => Error.Forbidden(
        code: "User.PlatformPermissionNotAllowed",
        description: "Quyền nền tảng không thể cấp cho tài khoản tenant.");
}
