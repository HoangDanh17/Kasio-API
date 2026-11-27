using Microsoft.AspNetCore.Authorization;

namespace Kasio.Api.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
