using Kasio.Application.Common.Messaging;
using Kasio.Contracts.Role;

namespace Kasio.Application.Roles.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(Guid Id) : IQuery<RoleResponse>;
