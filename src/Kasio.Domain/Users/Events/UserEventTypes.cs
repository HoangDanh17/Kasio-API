namespace Kasio.Domain.Users.Events;

public static class UserEventTypes
{
    public const string UserCreated = "User.Created";
    public const string UserDeactivated = "User.Deactivated";
    public const string AssignRoleUser = "User.AssignRole";
    public const string UserPermissionGranted = "UserPermission.Granted";
    public const string UserPermissionDenied = "UserPermission.Denied";
    public const string UserPermissionRemoved = "UserPermission.Removed";
}
