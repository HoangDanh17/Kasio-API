using ErrorOr;
using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Users.Commands.AssignUserRole;

public sealed class AssignUserRoleCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository) : IRequestHandler<AssignUserRoleCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(new UserId(request.ActorUserId), cancellationToken);

        if (actor is null)
            return UserErrors.ActorNotFound;

        if (!actor.IsActive)
            return UserErrors.AccountDisabled;

        var actorRole = await roleRepository.GetByIdAsync(actor.RoleId, cancellationToken);

        if (actorRole is null)
            return RoleErrors.NotFound;

        var targetRole = await roleRepository.GetByIdAsync(new RoleId(request.RoleId), cancellationToken);

        if (targetRole is null)
            return RoleErrors.NotFound;

        // Không ai gán được Platform Admin; Owner chỉ gán được role dưới mình.
        if (!RolePolicy.CanManageAccount(actorRole.Name, targetRole.Name))
            return UserErrors.CannotAssignRole;

        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        var currentRole = await roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        var currentRoleName = currentRole?.Name;

        if (string.Equals(currentRoleName, RoleNames.Owner, StringComparison.Ordinal))
            return UserErrors.CannotManageOwnerAccount;

        if (string.Equals(currentRoleName, RoleNames.Admin, StringComparison.Ordinal))
            return UserErrors.CannotManageAdminAccount;

        if (request.UserId == request.ActorUserId && user.RoleId != targetRole.Id)
            return UserErrors.CannotChangeOwnRole;

        var assigned = user.AssignRole(targetRole.Id);

        if (assigned.IsError)
            return assigned.Errors;

        return Result.Success;
    }
}
