using Kasio.Application.Common.Messaging;
using Kasio.Contracts.Role;

namespace Kasio.Application.Roles.Queries.GetAllRole;

public sealed record GetAllRoleQuery() : IQuery<IReadOnlyList<RoleResponse>>;
