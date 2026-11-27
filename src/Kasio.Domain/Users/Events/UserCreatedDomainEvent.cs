using Kasio.Domain.Common;
using Kasio.Domain.Roles;

namespace Kasio.Domain.Users.Events;

/// <summary>
/// Sự kiện tạo user. Không mang PasswordHash: hash chỉ cần cho write model,
/// auth đọc write DB trực tiếp (BRD: query handler không đọc PostgreSQL, auth thì có).
/// </summary>
public sealed record class UserCreatedDomainEvent
    (UserId Id, RoleId RoleId, string FullName, string Email,
    string PhoneNumber, string Address, Gender Gender, DateTime Dob, bool IsActive,
    DateTime CreatedAt, DateTime OccurredOnUtc) : IDomainEvent
{
    public string EventType => UserEventTypes.UserCreated;
}
