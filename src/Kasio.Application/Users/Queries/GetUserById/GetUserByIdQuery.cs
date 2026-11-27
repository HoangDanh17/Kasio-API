using Kasio.Application.Common.Messaging;
using Kasio.Contracts.User;

namespace Kasio.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserResponse>;
