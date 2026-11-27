using Kasio.Application.Common.Messaging;
using Kasio.Contracts.User;

namespace Kasio.Application.Auths.Queries.GetMe;

public sealed record GetMeQuery(Guid UserId) : IQuery<UserResponse>;
