namespace Kasio.Domain.Roles;

public static class RoleNames
{
    /// <summary>Platform Admin — vai trò nền tảng, không thuộc tenant (BRD 3.1).</summary>
    public const string Admin = "admin";

    /// <summary>Chủ trọ chính; sở hữu tenant và toàn bộ quyền tenant (BRD 3.4).</summary>
    public const string Owner = "owner";

    /// <summary>Quản lý Property được giao; không có quyền nhạy cảm mặc định.</summary>
    public const string Manager = "manager";

    /// <summary>Khách thuê đại diện; quyền truy cập theo Lease, không theo RBAC tenant.</summary>
    public const string Renter = "renter";

    private static readonly HashSet<string> SystemRoles = new(StringComparer.Ordinal)
    {
        Admin,
        Owner,
        Manager,
        Renter
    };

    public static bool IsSystem(string name) => SystemRoles.Contains(name);
}
