using ErrorOr;
using Kasio.Application.Common.Interfaces;
using Kasio.Contracts.User;
using Kasio.Domain.Users;
using MediatR;

namespace Kasio.Application.Permissions.Queries.GetUserPermissions;

public sealed class GetUserPermissionsQueryHandler(IUserPermissionReader reader)
 : IRequestHandler<GetUserPermissionsQuery, ErrorOr<IReadOnlyList<UserPermissionStateResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<UserPermissionStateResponse>>> Handle(
        GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        var states = await reader.GetStatesAsync(new UserId(request.UserId), cancellationToken);

        if (states is null)
            return UserErrors.NotFound;

        return states.ToList();
    }
}
