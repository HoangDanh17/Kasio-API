using Kasio.Application.Common.Messaging;
using Kasio.Contracts.Common;
using Kasio.Contracts.User;

namespace Kasio.Application.Users.Queries.GetAllUsers;

public sealed record GetAllUsersQuery(int Page = 1, int PageSize = 10, string? Search = null)
    : IQuery<PagedResponse<UserResponse>>;
