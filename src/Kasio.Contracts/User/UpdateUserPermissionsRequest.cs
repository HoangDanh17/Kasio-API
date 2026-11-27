namespace Kasio.Contracts.User;

public sealed record UpdateUserPermissionsRequest(IReadOnlyList<PermissionChangeRequest> Changes);
