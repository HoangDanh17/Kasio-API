using ErrorOr;
using Kasio.Domain.Common;
using Kasio.Domain.Permissions;
using Kasio.Domain.Roles;
using Kasio.Domain.Users.Events;

namespace Kasio.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    public RoleId RoleId { get; private set; }
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public Gender Gender { get; private set; }
    public DateTime Dob { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    private readonly List<UserPermission> _permissions = [];
    public IReadOnlyList<UserPermission> Permissions => _permissions;

    private User() { }
    private User
        (UserId id, RoleId roleId, string fullName, string email, string passwordHash,
        string phoneNumber, string address, Gender gender, DateTime dob)
    {
        Id = id;
        RoleId = roleId;
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        PhoneNumber = phoneNumber;
        Address = address;
        Gender = gender;
        // Npgsql (timestamptz) chi chap nhan DateTime Kind=Utc/Local, khong chap nhan Unspecified
        Dob = dob.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dob, DateTimeKind.Utc)
            : dob;
        CreatedAt = DateTime.UtcNow;
    }

    public static ErrorOr<User> Create(RoleId roleId, string fullName, string email, string passwordHash,
        string phoneNumber, string address, Gender gender, DateTime dob)
    {
        if (roleId.Value == Guid.Empty)
            return UserErrors.InvalidRoleId;
        if (string.IsNullOrWhiteSpace(fullName))
            return UserErrors.InvalidFullName;
        if (string.IsNullOrWhiteSpace(email))
            return UserErrors.InvalidEmail;
        if (string.IsNullOrWhiteSpace(passwordHash))
            return UserErrors.InvalidPasswordHash;
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return UserErrors.InvalidPhoneNumber;
        if (string.IsNullOrWhiteSpace(address))
            return UserErrors.InvalidAddress;

        var userId = UserId.New();

        var user = new User(userId, roleId, fullName.Trim(), EmailNormalizer.Normalize(email), passwordHash.Trim(),
            phoneNumber.Trim(), address.Trim(), gender, dob);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.RoleId, user.FullName, user.Email,
            user.PhoneNumber, user.Address, user.Gender, user.Dob, user.IsActive, user.CreatedAt, DateTime.UtcNow));

        return user;
    }

    // Gán vai trò cho user
    public ErrorOr<Success> AssignRole(RoleId roleId)
    {
        if (roleId.Value == Guid.Empty)
            return UserErrors.InvalidRoleId;

        if (roleId == RoleId)
            return Result.Success;

        var previousRoleId = RoleId;
        var hadOverrides = _permissions.Count > 0;

        RoleId = roleId;
        _permissions.Clear();

        RaiseDomainEvent(new AssignUserRoleDomainEvent(Id, previousRoleId.Value, roleId, hadOverrides, DateTime.UtcNow));
        return Result.Success;
    }

    /// <summary>Khóa tài khoản (soft delete). Idempotent: gọi lại khi đã khóa không phát thêm event.</summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        RaiseDomainEvent(new UserDeactivatedDomainEvent(Id, DateTime.UtcNow));
    }

    /// <summary>
    /// Đặt ý định của admin cho một quyền của user.
    /// Inherit xóa dòng ngoại lệ, Allow và Deny tạo hoặc cập nhật dòng.
    /// Chỉ phát event khi trạng thái thực sự thay đổi (BRD 11.1: đổi quyền phải có audit).
    /// </summary>
    public void SetPermission(PermissionId permissionId, PermissionEffect effect)
    {
        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);

        if (effect == PermissionEffect.Inherit)
        {
            if (existing is null)
                return;

            _permissions.Remove(existing);
            RaiseDomainEvent(new UserPermissionRemovedDomainEvent(Id, permissionId, DateTime.UtcNow));

            return;
        }

        var isGranted = effect == PermissionEffect.Allow;

        if (existing is null)
        {
            _permissions.Add(new UserPermission(Id, permissionId, isGranted));
            RaisePermissionOverride(permissionId, isGranted);

            return;
        }

        if (existing.IsGranted == isGranted)
            return;

        existing.SetGranted(isGranted);
        RaisePermissionOverride(permissionId, isGranted);
    }

    private void RaisePermissionOverride(PermissionId permissionId, bool isGranted)
    {
        IDomainEvent domainEvent = isGranted
            ? new UserPermissionGrantedDomainEvent(Id, permissionId, true, DateTime.UtcNow)
            : new UserPermissionDeniedDomainEvent(Id, permissionId, true, DateTime.UtcNow);

        RaiseDomainEvent(domainEvent);
    }

}
