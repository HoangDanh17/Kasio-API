namespace Kasio.Domain.Roles;

public readonly record struct RoleId(Guid Value)
{
    public static RoleId New() => new(Guid.NewGuid());

}
