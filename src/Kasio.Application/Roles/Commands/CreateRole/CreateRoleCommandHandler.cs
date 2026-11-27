using ErrorOr;
using Kasio.Application.Common.Interfaces;
using Kasio.Contracts.Role;
using Kasio.Domain.Permissions;
using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IUserRepository userRepository,
    IUserPermissionReader permissionReader
    ) : IRequestHandler<CreateRoleCommand, ErrorOr<RoleResponse>>
{
    public async Task<ErrorOr<RoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var actor = await LoadOwnerActorAsync(request.ActorUserId, cancellationToken);

        if (actor.IsError)
            return actor.Errors;

        if (RoleNames.IsSystem(request.Name))
            return RoleErrors.SystemRole;

        var existingRole = await roleRepository.GetByNameAsync(request.Name, cancellationToken);

        if (existingRole is not null)
            return RoleErrors.AlreadyExists;

        var requestedIds = request.PermissionIds.Distinct().ToList();

        var permissions = await permissionRepository.GetByIdsAsync(requestedIds, cancellationToken);

        if (permissions.Count != requestedIds.Count)
            return RoleErrors.SomePermissionsNotFound;

        var granted = await RolePermissionValidator.ValidateAsync(
            actor.Value, permissions, permissionReader, cancellationToken);

        if (granted.IsError)
            return granted.Errors;

        var roleResult = Role.Create(request.Name);

        if (roleResult.IsError)
            return roleResult.Errors;

        var role = roleResult.Value;

        foreach (var permission in permissions)
        {
            var result = role.AddPermission(permission);

            if (result.IsError)
                return result.Errors;
        }

        await roleRepository.AddAsync(role, cancellationToken);

        return RoleMapping.ToResponse(role);
    }

    private async Task<ErrorOr<User>> LoadOwnerActorAsync(Guid actorId, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(new UserId(actorId), cancellationToken);

        if (actor is null)
            return UserErrors.ActorNotFound;

        if (!actor.IsActive)
            return UserErrors.AccountDisabled;

        var actorRole = await roleRepository.GetByIdAsync(actor.RoleId, cancellationToken);

        if (actorRole is null)
            return RoleErrors.NotFound;

        // BRD 3.4: chỉ Owner tạo role tùy chỉnh; Platform Admin không quản lý role tenant.
        if (!string.Equals(actorRole.Name, RoleNames.Owner, StringComparison.Ordinal))
            return RoleErrors.OwnerOnly;

        return actor;
    }
}
