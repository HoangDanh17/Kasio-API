using Kasio.Domain.Roles;

namespace Kasio.Contracts.Role;

public record RoleResponse(
    Guid Id,
    string Name,
    IReadOnlyList<PermissionResponse> Permissions
)
{
    public bool IsSystem => RoleNames.IsSystem(Name);
}
