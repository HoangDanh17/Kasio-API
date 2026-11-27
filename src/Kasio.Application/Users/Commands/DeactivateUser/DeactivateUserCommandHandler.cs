using ErrorOr;
using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Users.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository) : IRequestHandler<DeactivateUserCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(new UserId(request.ActorUserId), cancellationToken);

        if (actor is null)
            return UserErrors.ActorNotFound;

        if (!actor.IsActive)
            return UserErrors.AccountDisabled;

        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        if (request.UserId == request.ActorUserId)
            return UserErrors.CannotDeactivateSelf;

        var actorRole = await roleRepository.GetByIdAsync(actor.RoleId, cancellationToken);

        if (actorRole is null)
            return RoleErrors.NotFound;

        var targetRole = await roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        if (targetRole is null)
            return RoleErrors.NotFound;

        // Owner/Admin không bị khóa qua API tenant (BR-44: không thay đổi quyền Owner chính).
        if (string.Equals(targetRole.Name, RoleNames.Owner, StringComparison.Ordinal))
            return UserErrors.CannotManageOwnerAccount;

        if (string.Equals(targetRole.Name, RoleNames.Admin, StringComparison.Ordinal))
            return UserErrors.CannotManageAdminAccount;

        if (!RolePolicy.CanManageAccount(actorRole.Name, targetRole.Name))
            return UserErrors.CannotManageAccount;

        user.Deactivate();

        return Result.Success;
    }
}
