using Kasio.Application.Common.Messaging;
using Kasio.Contracts.Role;
using Kasio.Domain.Permissions;
using Kasio.Domain.Roles;

namespace Kasio.Application.Roles.Commands.UpdateRole;

public record class UpdateRoleCommand(RoleId Id, string Name, IReadOnlyList<PermissionId> PermissionIds, Guid ActorUserId) : ICommand<RoleResponse>;
