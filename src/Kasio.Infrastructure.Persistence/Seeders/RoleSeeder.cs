using Kasio.Domain.Common;
using Kasio.Domain.Permissions;
using Kasio.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kasio.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seed các role hệ thống và quyền mặc định theo BRD mục 3.4.
/// Chạy sau <see cref="PermissionSeeder"/> và idempotent: role/role-permission được đồng bộ theo catalog.
/// </summary>
public sealed class RoleSeeder(KasioDbContext dbContext, ILogger<RoleSeeder> logger)
{
    private static readonly IReadOnlyDictionary<string, string[]> DesiredPermissions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            // Platform Admin: quản trị nền tảng + bề mặt quản trị user/role của API.
            [RoleNames.Admin] =
            [
                PermissionCatalog.UserRead,
                PermissionCatalog.UserCreate,
                PermissionCatalog.UserUpdate,
                PermissionCatalog.UserDelete,
                PermissionCatalog.RoleRead,
                PermissionCatalog.RoleCreate,
                PermissionCatalog.RoleUpdate,
                PermissionCatalog.RoleDelete,
                PermissionCatalog.PermissionRead,
                PermissionCatalog.PlatformOwnerManage,
                PermissionCatalog.PlatformListingModerate,
                PermissionCatalog.PlatformLeaseTemplateManage,
                PermissionCatalog.PlatformSupportAccess
            ],

            // Owner chính: toàn bộ quyền tenant, gồm các quyền nhạy cảm (BRD 3.3, 3.4).
            [RoleNames.Owner] =
            [
                PermissionCatalog.UserRead,
                PermissionCatalog.UserCreate,
                PermissionCatalog.UserUpdate,
                PermissionCatalog.UserDelete,
                PermissionCatalog.RoleRead,
                PermissionCatalog.RoleCreate,
                PermissionCatalog.RoleUpdate,
                PermissionCatalog.RoleDelete,
                PermissionCatalog.PermissionRead,
                PermissionCatalog.TenantSettingsRead,
                PermissionCatalog.TenantSettingsWrite,
                PermissionCatalog.StaffManage,
                PermissionCatalog.PropertyRead,
                PermissionCatalog.PropertyWrite,
                PermissionCatalog.RoomRead,
                PermissionCatalog.RoomWrite,
                PermissionCatalog.TariffRead,
                PermissionCatalog.TariffWrite,
                PermissionCatalog.ListingRead,
                PermissionCatalog.ListingWrite,
                PermissionCatalog.LeaseRead,
                PermissionCatalog.LeaseCreate,
                PermissionCatalog.LeaseRenew,
                PermissionCatalog.LeaseTemplateManage,
                PermissionCatalog.LeaseTerminate,
                PermissionCatalog.DocumentCccdView,
                PermissionCatalog.ConditionRead,
                PermissionCatalog.ConditionWrite,
                PermissionCatalog.MeterRead,
                PermissionCatalog.MeterWrite,
                PermissionCatalog.InvoiceRead,
                PermissionCatalog.InvoiceCreate,
                PermissionCatalog.PaymentConfirm,
                PermissionCatalog.TicketRead,
                PermissionCatalog.TicketUpdate,
                PermissionCatalog.NotificationSend,
                PermissionCatalog.AuditRead
            ],

            // Manager: Property/Room, tin đăng, hợp đồng (trừ mẫu), biên bản, chỉ số,
            // hóa đơn (trừ xác nhận thanh toán), ticket, thông báo, audit read.
            // Không có quyền nhạy cảm mặc định.
            [RoleNames.Manager] =
            [
                PermissionCatalog.TenantSettingsRead,
                PermissionCatalog.PropertyRead,
                PermissionCatalog.PropertyWrite,
                PermissionCatalog.RoomRead,
                PermissionCatalog.RoomWrite,
                PermissionCatalog.TariffRead,
                PermissionCatalog.ListingRead,
                PermissionCatalog.ListingWrite,
                PermissionCatalog.LeaseRead,
                PermissionCatalog.LeaseCreate,
                PermissionCatalog.LeaseRenew,
                PermissionCatalog.ConditionRead,
                PermissionCatalog.ConditionWrite,
                PermissionCatalog.MeterRead,
                PermissionCatalog.MeterWrite,
                PermissionCatalog.InvoiceRead,
                PermissionCatalog.InvoiceCreate,
                PermissionCatalog.TicketRead,
                PermissionCatalog.TicketUpdate,
                PermissionCatalog.NotificationSend,
                PermissionCatalog.AuditRead
            ],

            // Renter truy cập dữ liệu của chính mình theo Lease (representativeUserId),
            // không dùng RBAC tenant nên không có permission mặc định.
            [RoleNames.Renter] = []
        };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissionByName = await LoadPermissionsAsync(cancellationToken);
        var roles = await LoadRolesAsync(cancellationToken);
        var changed = false;

        foreach (var (roleName, desiredCodes) in DesiredPermissions)
        {
            if (!roles.TryGetValue(roleName, out var role))
            {
                role = CreateRole(roleName);
                changed = true;
            }

            changed |= SyncPermissions(role, roleName, desiredCodes, permissionByName);
        }

        if (!changed)
        {
            logger.LogInformation("Role hệ thống đã đúng dữ liệu, không có thay đổi.");
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed role hệ thống hoàn tất.");
    }

    private async Task<Dictionary<string, Permission>> LoadPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await dbContext.Permissions.ToListAsync(cancellationToken);

        if (permissions.Count == 0)
        {
            throw new InvalidOperationException(
                "Bảng Permissions đang trống. Phải chạy PermissionSeeder trước RoleSeeder.");
        }

        var permissionByName = permissions.ToDictionary(permission => permission.Name, StringComparer.Ordinal);

        var missingCodes = DesiredPermissions.Values
            .SelectMany(codes => codes)
            .Where(code => !permissionByName.ContainsKey(code))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (missingCodes.Count > 0)
        {
            throw new InvalidOperationException(
                $"PermissionCatalog và bảng Permissions lệch nhau, thiếu: {string.Join(", ", missingCodes)}.");
        }

        return permissionByName;
    }

    private async Task<Dictionary<string, Role>> LoadRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles
            .Include(role => role.Permissions)
            .ToListAsync(cancellationToken);

        return roles.ToDictionary(role => role.Name, StringComparer.Ordinal);
    }

    private Role CreateRole(string roleName)
    {
        var created = Role.Create(roleName);

        if (created.IsError)
        {
            throw new InvalidOperationException(
                $"Không tạo được role {roleName}: {created.FirstError.Description}");
        }

        var role = created.Value;
        dbContext.Roles.Add(role);
        logger.LogInformation("Đã tạo role hệ thống {Role}.", roleName);

        return role;
    }

    private bool SyncPermissions(
        Role role,
        string roleName,
        string[] desiredCodes,
        Dictionary<string, Permission> permissionByName)
    {
        var desiredPermissions = desiredCodes
            .Select(code => permissionByName[code])
            .ToList();

        var desiredIds = desiredPermissions
            .Select(permission => permission.Id)
            .ToHashSet();

        var currentIds = role.Permissions
            .Select(permission => permission.Id)
            .ToHashSet();

        var changed = false;

        foreach (var permission in desiredPermissions.Where(permission => !currentIds.Contains(permission.Id)))
        {
            role.AddPermission(permission);
            changed = true;
        }

        foreach (var permission in role.Permissions.ToList())
        {
            if (desiredIds.Contains(permission.Id))
                continue;

            role.RemovePermission(permission);
            changed = true;
        }

        // Domain event của seeding không được publish: CAP chưa chạy ở bước này.
        ((IAggregateRoot)role).ClearDomainEvents();

        logger.LogInformation(
            "Role {Role}: {Granted}/{Expected} quyền.",
            roleName,
            role.Permissions.Count,
            desiredCodes.Length);

        return changed;
    }
}
