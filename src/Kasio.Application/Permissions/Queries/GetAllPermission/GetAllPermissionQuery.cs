using Kasio.Application.Common.Messaging;
using Kasio.Contracts.Role;

namespace Kasio.Application.Permissions.Queries.GetAllPermission;

public sealed record GetAllPermissionQuery : IQuery<List<PermissionResponse>>;
