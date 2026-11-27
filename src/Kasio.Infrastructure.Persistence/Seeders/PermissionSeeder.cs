using Kasio.Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kasio.Infrastructure.Persistence.Seeders;

/// <summary>
/// Đồng bộ danh mục quyền cố định (BRD mục 10) vào PostgreSQL.
/// Chạy idempotent: thêm quyền mới, cập nhật nhóm/cờ nhạy cảm, không xóa quyền đang được tham chiếu.
/// </summary>
public sealed class PermissionSeeder(KasioDbContext dbContext, ILogger<PermissionSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Permissions.ToListAsync(cancellationToken);
        var byName = existing.ToDictionary(permission => permission.Name, StringComparer.Ordinal);

        var added = 0;

        foreach (var definition in PermissionCatalog.Definitions)
        {
            if (byName.ContainsKey(definition.Code))
                continue;

            var created = Permission.Create(
                new PermissionId(PermissionCatalog.IdOf(definition.Code)),
                definition.Code,
                definition.Group,
                definition.IsSensitive);

            if (created.IsError)
            {
                throw new InvalidOperationException(
                    $"Không tạo được quyền {definition.Code}: {created.FirstError.Description}");
            }

            dbContext.Permissions.Add(created.Value);
            added++;
        }

        WarnObsoletePermissions(existing);

        if (added > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seed quyền hoàn tất: thêm {Added}, tổng catalog {Total}.",
            added,
            PermissionCatalog.Definitions.Count);
    }

    private void WarnObsoletePermissions(List<Permission> existing)
    {
        var catalogCodes = PermissionCatalog.Definitions
            .Select(definition => definition.Code)
            .ToHashSet(StringComparer.Ordinal);

        var obsolete = existing
            .Where(permission => !catalogCodes.Contains(permission.Name))
            .Select(permission => permission.Name)
            .ToList();

        if (obsolete.Count == 0)
            return;

        logger.LogWarning(
            "Có {Count} quyền không còn trong PermissionCatalog: {Codes}. Không tự động xóa để tránh mất dữ liệu Role_Permission.",
            obsolete.Count,
            string.Join(", ", obsolete));
    }
}
