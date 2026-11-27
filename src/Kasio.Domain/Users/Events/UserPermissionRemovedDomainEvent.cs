using Kasio.Domain.Common;
using Kasio.Domain.Permissions;

namespace Kasio.Domain.Users.Events;

public sealed record class UserPermissionRemovedDomainEvent
    (UserId UserId, PermissionId PermissionId, DateTime OccurredOnUtc) : IDomainEvent
{
    public string EventType => UserEventTypes.UserPermissionRemoved;
}
