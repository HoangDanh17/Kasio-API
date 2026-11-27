using Kasio.Domain.Common;
using Kasio.Domain.Permissions;
using Kasio.Domain.Roles;
using Kasio.Domain.Roles.Events;
using Kasio.Domain.Users;
using Kasio.Domain.Users.Events;
using Xunit;

namespace Kasio.UnitTests;

public sealed class DomainEventTests
{
    private static User NewUser()
    {
        var result = User.Create(
            new RoleId(Guid.NewGuid()),
            "Nguyen Van A",
            "a@example.com",
            "hash",
            "0901234567",
            "Ha Noi",
            Gender.Male,
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.False(result.IsError);

        var user = result.Value;
        ((IAggregateRoot)user).ClearDomainEvents();   // bỏ UserCreated để test sạch

        return user;
    }

    [Fact]
    public void Allow_raises_granted_event()
    {
        var user = NewUser();
        var permissionId = PermissionId.New();

        user.SetPermission(permissionId, PermissionEffect.Allow);

        var granted = Assert.IsType<UserPermissionGrantedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(permissionId, granted.PermissionId);
        Assert.True(granted.IsException);
        Assert.Equal(DateTimeKind.Utc, granted.OccurredOnUtc.Kind);
    }

    [Fact]
    public void Deny_raises_denied_event()
    {
        var user = NewUser();
        var permissionId = PermissionId.New();

        user.SetPermission(permissionId, PermissionEffect.Deny);

        var denied = Assert.IsType<UserPermissionDeniedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(permissionId, denied.PermissionId);
        Assert.Equal(DateTimeKind.Utc, denied.OccurredOnUtc.Kind);
    }

    [Fact]
    public void Flipping_allow_to_deny_raises_denied_event()
    {
        var user = NewUser();
        var permissionId = PermissionId.New();
        user.SetPermission(permissionId, PermissionEffect.Allow);
        ((IAggregateRoot)user).ClearDomainEvents();

        user.SetPermission(permissionId, PermissionEffect.Deny);

        Assert.IsType<UserPermissionDeniedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.False(Assert.Single(user.Permissions).IsGranted);
    }

    [Fact]
    public void Inherit_removes_override_and_raises_removed_event()
    {
        var user = NewUser();
        var permissionId = PermissionId.New();
        user.SetPermission(permissionId, PermissionEffect.Allow);
        ((IAggregateRoot)user).ClearDomainEvents();

        user.SetPermission(permissionId, PermissionEffect.Inherit);

        Assert.IsType<UserPermissionRemovedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Empty(user.Permissions);
    }

    [Fact]
    public void Repeating_same_effect_raises_nothing()
    {
        var user = NewUser();
        var permissionId = PermissionId.New();
        user.SetPermission(permissionId, PermissionEffect.Allow);
        ((IAggregateRoot)user).ClearDomainEvents();

        user.SetPermission(permissionId, PermissionEffect.Allow);

        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void Inherit_without_override_raises_nothing()
    {
        var user = NewUser();

        user.SetPermission(PermissionId.New(), PermissionEffect.Inherit);

        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void Deactivate_sets_inactive_and_raises_event()
    {
        var user = NewUser();

        user.Deactivate();

        Assert.False(user.IsActive);
        var deactivated = Assert.IsType<UserDeactivatedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(DateTimeKind.Utc, deactivated.OccurredOnUtc.Kind);
    }

    [Fact]
    public void Deactivate_twice_raises_only_once()
    {
        var user = NewUser();
        user.Deactivate();
        ((IAggregateRoot)user).ClearDomainEvents();

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void Role_created_event_uses_utc()
    {
        var role = Role.Create("manager").Value;

        var created = Assert.IsType<RoleCreatedDomainEvent>(Assert.Single(role.DomainEvents));

        Assert.Equal(DateTimeKind.Utc, created.OccurredOnUtc.Kind);
        Assert.True((DateTime.UtcNow - created.OccurredOnUtc).Duration() < TimeSpan.FromMinutes(1));
    }
}
