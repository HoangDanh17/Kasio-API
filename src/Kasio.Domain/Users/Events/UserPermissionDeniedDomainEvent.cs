using Kasio.Domain.Common;
using Kasio.Domain.Permissions;

namespace Kasio.Domain.Users.Events;

public sealed record class UserPermissionDeniedDomainEvent
    (UserId UserId, PermissionId PermissionId, bool IsException, DateTime OccurredOnUtc) : IDomainEvent
{
    public string EventType => UserEventTypes.UserPermissionDenied;
}
