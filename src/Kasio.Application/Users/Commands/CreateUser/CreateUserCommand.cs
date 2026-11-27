using Kasio.Application.Common.Messaging;
using Kasio.Contracts.User;
using Kasio.Domain.Users;

namespace Kasio.Application.Users.Commands.CreateUser;

public record class CreateUserCommand
    (string FullName, string Email, string Password, string PhoneNumber, string Address, Gender Gender, DateTime Dob, Guid RoleId, Guid ActorUserId) : ICommand<UserResponse>;
