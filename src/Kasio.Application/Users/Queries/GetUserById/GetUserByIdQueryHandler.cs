using ErrorOr;
using Kasio.Contracts.User;
using Kasio.Domain.Repositories;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository
    ) : IRequestHandler<GetUserByIdQuery, ErrorOr<UserResponse>>
{
    public async Task<ErrorOr<UserResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.Id), cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        var role = await roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        return UserResponse.From(user, role?.Name ?? string.Empty);
    }
}
