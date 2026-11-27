using ErrorOr;
using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using MediatR;

namespace Kasio.Application.Roles.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository
) : IRequestHandler<DeleteRoleCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var roleId = new RoleId(request.Id);

        var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
            return RoleErrors.NotFound;

        if (RoleNames.IsSystem(role.Name))
            return RoleErrors.SystemRole;

        if (await userRepository.ExistsRoleInUseAsync(roleId, cancellationToken))
            return RoleErrors.RoleInUse;

        await roleRepository.DeleteAsync(role, cancellationToken);

        return Result.Success;
    }
}
