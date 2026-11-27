namespace Kasio.Domain.Roles;

/// <summary>
/// Ai được tạo tài khoản với role nào và ai được gán role nào.
/// MVP chưa có Tenant/membership nên chỉ xét thứ hạng role; phạm vi tenant và
/// Property scope (BRD 3.2) sẽ bổ sung khi có mô hình tenant.
/// </summary>
public static class RolePolicy
{
    /// <summary>Tạo tài khoản: Admin chỉ tạo Owner; Owner chỉ tạo được role dưới mình.</summary>
    public static bool CanCreateAccount(string actorRole, string targetRole)
    {
        if (string.Equals(targetRole, RoleNames.Admin, StringComparison.Ordinal))
            return false;

        if (string.Equals(actorRole, RoleNames.Admin, StringComparison.Ordinal))
            return string.Equals(targetRole, RoleNames.Owner, StringComparison.Ordinal);

        return string.Equals(actorRole, RoleNames.Owner, StringComparison.Ordinal)
            && !string.Equals(targetRole, RoleNames.Owner, StringComparison.Ordinal);
    }

    /// <summary>Quản lý tài khoản: không ai quản được Admin; Owner chỉ quản được role dưới mình.</summary>
    public static bool CanManageAccount(string actorRole, string targetRole)
    {
        if (string.Equals(targetRole, RoleNames.Admin, StringComparison.Ordinal))
            return false;

        if (string.Equals(actorRole, RoleNames.Admin, StringComparison.Ordinal))
            return true;

        return string.Equals(actorRole, RoleNames.Owner, StringComparison.Ordinal)
            && !string.Equals(targetRole, RoleNames.Owner, StringComparison.Ordinal);
    }
}
