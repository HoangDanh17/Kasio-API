using System.Security.Claims;
using Kasio.Application.Common.Interfaces;
using Kasio.Domain.Users;
using Microsoft.AspNetCore.Authorization;

namespace Kasio.Api.Authorization;

public sealed class PermissionAuthorizationHandler(IUserPermissionReader reader)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(sub, out var userId))
            return;   // không gọi Succeed nghĩa là bị từ chối

        var codes = await reader.GetEffectivePermissionCodesAsync(new UserId(userId));

        if (codes.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
