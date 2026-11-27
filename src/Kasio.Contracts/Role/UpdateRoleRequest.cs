namespace Kasio.Contracts.Role;

public sealed record UpdateRoleRequest(string Name, IReadOnlyList<Guid> PermissionIds);
