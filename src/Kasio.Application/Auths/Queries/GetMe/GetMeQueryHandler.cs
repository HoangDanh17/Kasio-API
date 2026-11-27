using ErrorOr;
using Kasio.Contracts.User;
using Kasio.Domain.Repositories;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Auths.Queries.GetMe;

public sealed class GetMeQueryHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository
    ) : IRequestHandler<GetMeQuery, ErrorOr<UserResponse>>
{
    public async Task<ErrorOr<UserResponse>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        var role = await roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        return UserResponse.From(user, role?.Name ?? string.Empty);
    }
}
