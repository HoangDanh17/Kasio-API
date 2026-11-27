namespace Kasio.Domain.Permissions;

/// <summary>
/// Một mục trong danh mục quyền cố định của nền tảng (BRD mục 10).
/// </summary>
/// <param name="Code">Mã quyền dùng trong policy và bảng Permission.</param>
/// <param name="Group">Nhóm nghiệp vụ dùng để hiển thị và lọc.</param>
/// <param name="DisplayName">Mô tả tiếng Việt cho UI.</param>
/// <param name="IsSensitive">Quyền nhạy cảm phải bật riêng và ghi audit khi cấp/thu hồi.</param>
public sealed record PermissionDefinition(
    string Code,
    string Group,
    string DisplayName,
    bool IsSensitive);
