using Kasio.Contracts.Role;
using Kasio.Domain.Roles;

namespace Kasio.Application.Roles;

internal static class RoleMapping
{
    public static RoleResponse ToResponse(Role role) => new(
        role.Id.Value,
        role.Name,
        role.Permissions
            .Select(x => new PermissionResponse(x.Id.Value, x.Name, x.Group, x.IsSensitive))
            .ToList());
}
