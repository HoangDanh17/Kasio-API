using ErrorOr;
using Kasio.Application.Common.Interfaces;
using Kasio.Contracts.User;
using Kasio.Domain.Repositories;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher
    ) : IRequestHandler<CreateUserCommand, ErrorOr<UserResponse>>
{
    public async Task<ErrorOr<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(new UserId(request.ActorUserId), cancellationToken);

        if (actor is null)
            return UserErrors.ActorNotFound;

        if (!actor.IsActive)
            return UserErrors.AccountDisabled;

        var actorRole = await roleRepository.GetByIdAsync(actor.RoleId, cancellationToken);

        if (actorRole is null)
            return RoleErrors.NotFound;

        var role = await roleRepository.GetByIdAsync(new RoleId(request.RoleId), cancellationToken);

        if (role is null)
            return RoleErrors.NotFound;

        // BRD 3.4: Admin tạo Owner; Owner chỉ tạo được role dưới mình; không ai tạo được Platform Admin.
        if (!RolePolicy.CanCreateAccount(actorRole.Name, role.Name))
            return UserErrors.CannotCreateRole;

        var checkExistEmail = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (checkExistEmail is not null)
            return UserErrors.DuplicateEmail;

        var passwordHash = passwordHasher.HashPassword(request.Password);

        var result = User.Create(
            role.Id,
            request.FullName,
            request.Email,
            passwordHash,
            request.PhoneNumber,
            request.Address,
            request.Gender,
            request.Dob
        );

        if (result.IsError)
            return result.Errors;

        await userRepository.AddAsync(result.Value, cancellationToken);

        return UserResponse.From(result.Value, role.Name);
    }
}
