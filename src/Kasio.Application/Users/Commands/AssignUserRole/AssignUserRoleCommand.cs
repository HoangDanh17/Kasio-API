using ErrorOr;
using Kasio.Application.Common.Messaging;

namespace Kasio.Application.Users.Commands.AssignUserRole;

public sealed record AssignUserRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid ActorUserId
) : ICommand<Success>;
