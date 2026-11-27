using Kasio.Domain.Common;

namespace Kasio.Domain.Roles.Events;

public sealed record RoleUpdatedDomainEvent(RoleId Id, string Name, DateTime OccurredOnUtc) : IDomainEvent
{
    public string EventType => RoleEventTypes.RoleUpdated;
}
