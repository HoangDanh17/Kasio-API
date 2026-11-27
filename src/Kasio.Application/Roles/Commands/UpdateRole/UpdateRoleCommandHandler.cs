using ErrorOr;
using Kasio.Application.Common.Interfaces;
using Kasio.Contracts.Role;
using Kasio.Domain.Permissions;
using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IUserRepository userRepository,
    IUserPermissionReader permissionReader
) : IRequestHandler<UpdateRoleCommand, ErrorOr<RoleResponse>>
{
    public async Task<ErrorOr<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var actor = await LoadOwnerActorAsync(request.ActorUserId, cancellationToken);

        if (actor.IsError)
            return actor.Errors;

        var role = await roleRepository.GetByIdAsync(request.Id, cancellationToken);

        if (role is null)
            return RoleErrors.NotFound;

        if (RoleNames.IsSystem(role.Name))
            return RoleErrors.SystemRole;

        var duplicateName = await roleRepository.GetByNameAsync(request.Name, cancellationToken);

        if (duplicateName is not null && duplicateName.Id != role.Id)
            return RoleErrors.AlreadyExists;

        // lay list permission id distinct de tranh truong hop truyen trung lap permission id
        var requestedIds = request.PermissionIds.Distinct().ToList();

        var permissions = await permissionRepository.GetByIdsAsync(requestedIds, cancellationToken);

        if (permissions.Count != requestedIds.Count)
            return RoleErrors.SomePermissionsNotFound;

        var granted = await RolePermissionValidator.ValidateAsync(
            actor.Value, permissions, permissionReader, cancellationToken);

        if (granted.IsError)
            return granted.Errors;

        var updateResult = role.Update(request.Name);

        if (updateResult.IsError)
            return updateResult.Errors;

        var applied = ApplyPermissions(role, permissions);

        if (applied.IsError)
            return applied.Errors;

        return RoleMapping.ToResponse(role);
    }

    private static ErrorOr<Success> ApplyPermissions(Role role, IReadOnlyList<Permission> permissions)
    {
        var desiredIds = permissions.Select(p => p.Id).ToHashSet();

        foreach (var permission in role.Permissions.ToList())
        {
            if (desiredIds.Contains(permission.Id))
                continue;

            var removed = role.RemovePermission(permission);

            if (removed.IsError)
                return removed.Errors;
        }

        foreach (var permission in permissions)
        {
            if (role.Permissions.Any(p => p.Id == permission.Id))
                continue;

            var added = role.AddPermission(permission);

            if (added.IsError)
                return added.Errors;
        }

        return Result.Success;
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

        if (!string.Equals(actorRole.Name, RoleNames.Owner, StringComparison.Ordinal))
            return RoleErrors.OwnerOnly;

        return actor;
    }
}
