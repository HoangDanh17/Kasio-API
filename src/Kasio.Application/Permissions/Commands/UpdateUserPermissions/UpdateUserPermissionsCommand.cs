using ErrorOr;
using Kasio.Application.Common.Messaging;

namespace Kasio.Application.Permissions.Commands.UpdateUserPermissions;

public sealed record UpdateUserPermissionsCommand(
    Guid UserId,
    IReadOnlyList<PermissionChangeItem> Changes,
    Guid ActorUserId)
    : ICommand<Success>;
