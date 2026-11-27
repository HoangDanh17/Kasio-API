using Kasio.Domain.Common;

namespace Kasio.Domain.Users.Events;

/// <summary>Tài khoản bị khóa (soft delete): mất hiệu lực ngay, dữ liệu vẫn còn.</summary>
public sealed record class UserDeactivatedDomainEvent(UserId UserId, DateTime OccurredOnUtc) : IDomainEvent
{
    public string EventType => UserEventTypes.UserDeactivated;
}
