namespace Kasio.Contracts.User;

public sealed record PermissionChangeRequest(Guid PermissionId, string Effect);
