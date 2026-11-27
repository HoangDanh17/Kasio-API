using System.Security.Cryptography;
using System.Text;

namespace Kasio.Domain.Permissions;

/// <summary>
/// Danh mục quyền cố định của nền tảng. BRD mục 10 là nguồn sự thật;
/// seeder đọc từ đây nên DB luôn khớp policy trong code.
/// </summary>
public static class PermissionCatalog
{
    // Bề mặt quản trị người dùng / vai trò của API
    public const string UserRead = "user.read";
    public const string UserCreate = "user.create";
    public const string UserUpdate = "user.update";
    public const string UserDelete = "user.delete";

    public const string RoleRead = "role.read";
    public const string RoleCreate = "role.create";
    public const string RoleUpdate = "role.update";
    public const string RoleDelete = "role.delete";

    public const string PermissionRead = "permission.read";

    // Tenant & Staff
    public const string TenantSettingsRead = "tenant.settings.read";
    public const string TenantSettingsWrite = "tenant.settings.write";
    public const string StaffManage = "staff.manage";

    // Property & Room
    public const string PropertyRead = "property.read";
    public const string PropertyWrite = "property.write";
    public const string RoomRead = "room.read";
    public const string RoomWrite = "room.write";

    // Tài chính
    public const string TariffRead = "tariff.read";
    public const string TariffWrite = "tariff.write";

    // Tin đăng
    public const string ListingRead = "listing.read";
    public const string ListingWrite = "listing.write";

    // Hợp đồng
    public const string LeaseRead = "lease.read";
    public const string LeaseCreate = "lease.create";
    public const string LeaseRenew = "lease.renew";
    public const string LeaseTemplateManage = "lease.template.manage";
    public const string LeaseTerminate = "lease.terminate";

    // Tài liệu
    public const string DocumentCccdView = "document.cccd.view";

    // Biên bản
    public const string ConditionRead = "condition.read";
    public const string ConditionWrite = "condition.write";

    // Điện nước
    public const string MeterRead = "meter.read";
    public const string MeterWrite = "meter.write";

    // Hóa đơn & thanh toán
    public const string InvoiceRead = "invoice.read";
    public const string InvoiceCreate = "invoice.create";
    public const string PaymentConfirm = "payment.confirm";

    // Ticket & thông báo
    public const string TicketRead = "ticket.read";
    public const string TicketUpdate = "ticket.update";
    public const string NotificationSend = "notification.send";

    // Audit
    public const string AuditRead = "audit.read";

    // Platform Admin
    public const string PlatformOwnerManage = "platform.owner.manage";
    public const string PlatformListingModerate = "platform.listing.moderate";
    public const string PlatformLeaseTemplateManage = "platform.lease.template.manage";
    public const string PlatformSupportAccess = "platform.support.access";

    public static readonly IReadOnlyList<PermissionDefinition> Definitions =
    [
        new(UserRead, "Người dùng", "Xem người dùng", false),
        new(UserCreate, "Người dùng", "Tạo người dùng", false),
        new(UserUpdate, "Người dùng", "Cập nhật người dùng", false),
        new(UserDelete, "Người dùng", "Xóa người dùng", false),

        new(RoleRead, "Vai trò", "Xem vai trò", false),
        new(RoleCreate, "Vai trò", "Tạo vai trò", false),
        new(RoleUpdate, "Vai trò", "Cập nhật vai trò", false),
        new(RoleDelete, "Vai trò", "Xóa vai trò", false),
        new(PermissionRead, "Quyền", "Xem quyền", false),

        new(TenantSettingsRead, "Tenant", "Xem cấu hình tenant", false),
        new(TenantSettingsWrite, "Tenant", "Sửa cấu hình tenant", false),
        new(StaffManage, "Tenant", "Mời/xóa/khóa Staff, gán role", true),

        new(PropertyRead, "Property", "Xem Property", false),
        new(PropertyWrite, "Property", "Tạo/sửa/lưu trữ Property", false),
        new(RoomRead, "Room", "Xem Room", false),
        new(RoomWrite, "Room", "Tạo/sửa/lưu trữ Room", false),

        new(TariffRead, "Tài chính", "Xem bảng giá", false),
        new(TariffWrite, "Tài chính", "Sửa bảng giá", true),

        new(ListingRead, "Tin đăng", "Xem tin của tenant", false),
        new(ListingWrite, "Tin đăng", "Tạo/sửa/gửi tin", false),

        new(LeaseRead, "Hợp đồng", "Xem hợp đồng", false),
        new(LeaseCreate, "Hợp đồng", "Tạo hợp đồng", false),
        new(LeaseRenew, "Hợp đồng", "Gia hạn hợp đồng trên cùng Lease", false),
        new(LeaseTemplateManage, "Hợp đồng", "Tạo/sửa/ngừng hợp đồng mẫu của tenant", false),
        new(LeaseTerminate, "Hợp đồng", "Chấm dứt hợp đồng", true),

        new(DocumentCccdView, "Tài liệu", "Xem ảnh CCCD", true),

        new(ConditionRead, "Biên bản", "Xem biên bản", false),
        new(ConditionWrite, "Biên bản", "Tạo/sửa biên bản", false),

        new(MeterRead, "Điện nước", "Xem chỉ số", false),
        new(MeterWrite, "Điện nước", "Nhập/sửa chỉ số", false),

        new(InvoiceRead, "Hóa đơn", "Xem hóa đơn", false),
        new(InvoiceCreate, "Hóa đơn", "Phát hành/void hóa đơn", false),
        new(PaymentConfirm, "Hóa đơn", "Xác nhận/từ chối thanh toán", true),

        new(TicketRead, "Ticket", "Xem ticket", false),
        new(TicketUpdate, "Ticket", "Cập nhật/trả lời ticket", false),

        new(NotificationSend, "Thông báo", "Gửi thông báo Property", false),

        new(AuditRead, "Audit", "Xem audit tenant", false),

        new(PlatformOwnerManage, "Platform", "Khóa/mở Owner", true),
        new(PlatformListingModerate, "Platform", "Duyệt/từ chối tin", true),
        new(PlatformLeaseTemplateManage, "Platform", "Tạo/sửa/ngừng hợp đồng mẫu hệ thống", true),
        new(PlatformSupportAccess, "Platform", "Support access có lý do", true),
    ];

    public static readonly IReadOnlyDictionary<string, PermissionDefinition> ByCode =
        Definitions.ToDictionary(definition => definition.Code, StringComparer.Ordinal);

    public static readonly IReadOnlyDictionary<string, string> DisplayNames =
        Definitions.ToDictionary(
            definition => definition.Code,
            definition => definition.DisplayName,
            StringComparer.Ordinal);

    public static IEnumerable<string> Codes => Definitions.Select(definition => definition.Code);

    /// <summary>Quyền nền tảng không được cấp cho tenant role (BRD mục 10).</summary>
    public static bool IsPlatform(string code) =>
        code.StartsWith("platform.", StringComparison.Ordinal);

    /// <summary>Chỉ Owner chính được có các quyền này; không cấp qua role tùy chỉnh (BR-48).</summary>
    public static readonly IReadOnlySet<string> OwnerOnly = new HashSet<string>(StringComparer.Ordinal)
    {
        StaffManage,
        TenantSettingsWrite
    };

    /// <summary>
    /// ID xác định theo mã quyền để seed idempotent và khớp giữa các môi trường.
    /// </summary>
    public static Guid IdOf(string code)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return new Guid(hash.AsSpan(0, 16));
    }
}
