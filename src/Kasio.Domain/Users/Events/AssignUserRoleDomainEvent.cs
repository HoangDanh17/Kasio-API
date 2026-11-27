using Kasio.Domain.Common;
using Kasio.Domain.Roles;

namespace Kasio.Domain.Users.Events;

public sealed record AssignUserRoleDomainEvent
    (UserId UserId, Guid PreviousRoleId, RoleId RoleId, bool HadOverrides, DateTime OccurredOnUtc) : IDomainEvent
{
    public string EventType => UserEventTypes.AssignRoleUser;

}
