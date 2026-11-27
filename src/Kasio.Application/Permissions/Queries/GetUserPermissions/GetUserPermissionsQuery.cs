using Kasio.Application.Common.Messaging;
using Kasio.Contracts.User;

namespace Kasio.Application.Permissions.Queries.GetUserPermissions;

public sealed record GetUserPermissionsQuery(Guid UserId)
: IQuery<IReadOnlyList<UserPermissionStateResponse>>;
