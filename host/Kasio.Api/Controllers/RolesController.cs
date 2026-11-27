using Kasio.Application.Roles.Commands.CreateRole;
using Kasio.Application.Roles.Commands.DeleteRole;
using Kasio.Application.Roles.Commands.UpdateRole;
using Kasio.Application.Roles.Queries.GetAllRole;
using Kasio.Application.Roles.Queries.GetRoleById;
using Kasio.Contracts.Role;
using Kasio.Domain.Permissions;
using Kasio.Domain.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasio.Api.Controllers;

[Route("api/roles")]
public sealed class RolesController(ISender mediator) : ApiController
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.RoleRead)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllRoleQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.RoleRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRoleByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.RoleCreate)]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } actorId)
            return Unauthorized();

        var command = new CreateRoleCommand(
            request.Name,
            request.PermissionIds.Select(id => new PermissionId(id)).ToList(),
            actorId);

        var result = await mediator.Send(command, ct);

        return result.Match(
            value => StatusCode(StatusCodes.Status201Created, value),
            errors => Problem(errors));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.RoleUpdate)]
    public async Task<IActionResult> Update(Guid id, UpdateRoleRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } actorId)
            return Unauthorized();

        var command = new UpdateRoleCommand(
            new RoleId(id),
            request.Name,
            request.PermissionIds.Select(pid => new PermissionId(pid)).ToList(),
            actorId);

        var result = await mediator.Send(command, ct);

        return result.Match(value => Ok(value), errors => Problem(errors));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.RoleDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteRoleCommand(id), ct);
        return result.Match(_ => NoContent(), errors => Problem(errors));
    }
}
