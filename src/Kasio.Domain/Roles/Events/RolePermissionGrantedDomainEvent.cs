using Kasio.Domain.Common;
using Kasio.Domain.Permissions;

namespace Kasio.Domain.Roles.Events;

public sealed record class RolePermissionGrantedDomainEvent
    (RoleId RoleId, PermissionId PermissionId, DateTime OccurredOnUtc) : IDomainEvent
{
    public string EventType => RoleEventTypes.RolePermissionGranted;
}
