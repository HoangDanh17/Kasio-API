namespace Kasio.Contracts.Role;

public record PermissionResponse(
    Guid Id,
    string Name,
    string Group,
    bool IsSensitive
);
