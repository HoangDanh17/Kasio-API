using ErrorOr;
using Kasio.Contracts.Role;
using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using MediatR;

namespace Kasio.Application.Roles.Queries.GetRoleById;

public sealed class GetRoleByIdQueryHandler(
    IRoleRepository roleRepository) : IRequestHandler<GetRoleByIdQuery, ErrorOr<RoleResponse>>
{
    public async Task<ErrorOr<RoleResponse>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(new RoleId(request.Id), cancellationToken);
        if (role is null)
            return RoleErrors.NotFound;

        return new RoleResponse(role.Id.Value,
            role.Name,
            [.. role.Permissions.Select(x => new PermissionResponse(x.Id.Value, x.Name, x.Group, x.IsSensitive))]);
    }
}
