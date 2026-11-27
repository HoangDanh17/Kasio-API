using Kasio.Application.Common.Messaging;
using Kasio.Contracts.Role;
using Kasio.Domain.Permissions;

namespace Kasio.Application.Roles.Commands.CreateRole;

public record class CreateRoleCommand(string Name, IReadOnlyList<PermissionId> PermissionIds, Guid ActorUserId) : ICommand<RoleResponse>;
