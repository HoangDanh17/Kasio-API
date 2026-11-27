using Kasio.Domain.Permissions;
using Xunit;

namespace Kasio.UnitTests;

public sealed class PermissionGrantPolicyTests
{
    private static IReadOnlySet<string> Held(params string[] codes) =>
        new HashSet<string>(codes, StringComparer.Ordinal);

    [Fact]
    public void Rejects_platform_permission_even_when_actor_holds_it() =>
        Assert.Equal(
            PermissionGrantViolation.PlatformPermission,
            PermissionGrantPolicy.Check(
                PermissionCatalog.PlatformOwnerManage,
                Held(PermissionCatalog.PlatformOwnerManage)));

    [Fact]
    public void Rejects_owner_only_permission_even_when_actor_holds_it() =>
        Assert.Equal(
            PermissionGrantViolation.OwnerOnlyPermission,
            PermissionGrantPolicy.Check(
                PermissionCatalog.StaffManage,
                Held(PermissionCatalog.StaffManage, PermissionCatalog.TenantSettingsWrite)));

    [Fact]
    public void Rejects_permission_actor_does_not_hold() =>
        Assert.Equal(
            PermissionGrantViolation.NotHeld,
            PermissionGrantPolicy.Check(
                PermissionCatalog.PropertyWrite,
                Held(PermissionCatalog.PropertyRead)));

    [Fact]
    public void Allows_permission_actor_holds() =>
        Assert.Equal(
            PermissionGrantViolation.None,
            PermissionGrantPolicy.Check(
                PermissionCatalog.PropertyWrite,
                Held(PermissionCatalog.PropertyWrite)));
}
