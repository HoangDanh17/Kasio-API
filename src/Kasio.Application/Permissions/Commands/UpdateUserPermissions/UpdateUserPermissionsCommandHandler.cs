using ErrorOr;
using Kasio.Application.Common.Interfaces;
using Kasio.Domain.Permissions;
using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Permissions.Commands.UpdateUserPermissions;

public sealed class UpdateUserPermissionsCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IUserPermissionReader permissionReader)
    : IRequestHandler<UpdateUserPermissionsCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(UpdateUserPermissionsCommand request, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(new UserId(request.ActorUserId), cancellationToken);

        if (actor is null)
            return UserErrors.ActorNotFound;

        if (!actor.IsActive)
            return UserErrors.AccountDisabled;

        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        var targetRole = await roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        if (targetRole is null)
            return RoleErrors.NotFound;

        // BR-44: không thay đổi quyền của Owner chính; Platform Admin không quản lý qua API tenant.
        if (string.Equals(targetRole.Name, RoleNames.Owner, StringComparison.Ordinal))
            return UserErrors.CannotManageOwnerAccount;

        if (string.Equals(targetRole.Name, RoleNames.Admin, StringComparison.Ordinal))
            return UserErrors.CannotManageAdminAccount;

        var ids = request.Changes.Select(c => new PermissionId(c.PermissionId)).ToList();

        var found = await permissionRepository.GetByIdsAsync(ids, cancellationToken);

        if (found.Count != ids.Count)
            return PermissionErrors.NotFound;   // có id không tồn tại

        var nameById = found.ToDictionary(permission => permission.Id, permission => permission.Name);
        var actorPermissions = await permissionReader.GetEffectivePermissionCodesAsync(actor.Id, cancellationToken);

        // Validate toàn bộ trước khi mutate để không để lại thay đổi dở dang.
        foreach (var change in request.Changes)
        {
            var permissionName = nameById[new PermissionId(change.PermissionId)];

            switch (PermissionGrantPolicy.Check(permissionName, actorPermissions))
            {
                case PermissionGrantViolation.PlatformPermission:
                    return UserErrors.PlatformPermissionNotAllowed;
                case PermissionGrantViolation.OwnerOnlyPermission:
                    return UserErrors.OwnerOnlyPermission;
                case PermissionGrantViolation.NotHeld:
                    return UserErrors.ForbiddenPermissionGrant;
            }
        }

        foreach (var change in request.Changes)
        {
            var effect = Enum.Parse<PermissionEffect>(change.Effect, ignoreCase: true);
            user.SetPermission(new PermissionId(change.PermissionId), effect);
        }

        return Result.Success;
    }
}
