using ErrorOr;
using Kasio.Contracts.Role;
using Kasio.Domain.Repositories;
using MediatR;

namespace Kasio.Application.Permissions.Queries.GetAllPermission;

public sealed class GetAllPermissionQueryHandler(
    IPermissionRepository permissionRepository) : IRequestHandler<GetAllPermissionQuery, ErrorOr<List<PermissionResponse>>>
{
    public async Task<ErrorOr<List<PermissionResponse>>> Handle(GetAllPermissionQuery request, CancellationToken cancellationToken)
    {
        var permissions = await permissionRepository.GetAllAsync(cancellationToken);
        return permissions
            .Select(p => new PermissionResponse(p.Id.Value, p.Name, p.Group, p.IsSensitive))
            .ToList();
    }
}
