using Kasio.Domain.Users;

namespace Kasio.Contracts.User;

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    string PhoneNumber,
    string Address,
    Gender Gender,
    DateTime Dob,
    Guid RoleId);
