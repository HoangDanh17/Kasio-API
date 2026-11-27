using ErrorOr;
using Kasio.Domain.Common;

namespace Kasio.Domain.Permissions;

public sealed class Permission : AggregateRoot<PermissionId>
{
    public string Name { get; private set; } = null!;
    public string Group { get; private set; } = null!;
    public bool IsSensitive { get; private set; }

    private Permission() { }

    public static ErrorOr<Permission> Create(
        PermissionId id, string name, string group, bool isSensitive)
    {
        if (id.Value == Guid.Empty)
            return PermissionErrors.InvalidId;

        if (string.IsNullOrWhiteSpace(name))
            return PermissionErrors.InvalidName;

        if (string.IsNullOrWhiteSpace(group))
            return PermissionErrors.InvalidGroup;

        return new Permission
        {
            Id = id,
            Name = name.Trim(),
            Group = group.Trim(),
            IsSensitive = isSensitive
        };
    }
}
