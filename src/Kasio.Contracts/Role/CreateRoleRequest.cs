namespace Kasio.Contracts.Role;

public sealed record CreateRoleRequest(string Name, IReadOnlyList<Guid> PermissionIds);
