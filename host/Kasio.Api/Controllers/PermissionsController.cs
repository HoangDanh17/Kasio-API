using Kasio.Application.Permissions.Queries.GetAllPermission;
using Kasio.Domain.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasio.Api.Controllers;

[Route("api/permissions")]
public sealed class PermissionsController(ISender mediator) : ApiController
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.PermissionRead)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllPermissionQuery(), ct);
        return Ok(result);
    }
}
