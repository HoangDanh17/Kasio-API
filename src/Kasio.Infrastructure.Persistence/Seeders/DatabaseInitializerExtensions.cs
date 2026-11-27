using Kasio.Application.Common.Interfaces;
using Kasio.Domain.Common;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kasio.Infrastructure.Persistence.Seeders;

public static class DatabaseInitializerExtensions
{
    /// <summary>
    /// Migrate PostgreSQL rồi seed danh mục quyền, role hệ thống và tài khoản admin.
    /// Chạy trước khi API nhận request; idempotent nên gọi được ở mọi lần khởi động.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        await using var scope = host.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializerExtensions).FullName!);

        var dbContext = services.GetRequiredService<KasioDbContext>();

        logger.LogInformation("Áp dụng migration PostgreSQL...");
        await dbContext.Database.MigrateAsync(cancellationToken);

        await services.GetRequiredService<PermissionSeeder>().SeedAsync(cancellationToken);
        await services.GetRequiredService<RoleSeeder>().SeedAsync(cancellationToken);
        await SeedAdminUserAsync(services, logger, cancellationToken);

        logger.LogInformation("Khởi tạo dữ liệu nền hoàn tất.");
    }

    internal static async Task SeedAdminUserAsync(
        IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var configuredEmail = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(configuredEmail) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Bỏ qua seed admin: thiếu SeedAdmin:Email hoặc SeedAdmin:Password.");
            return;
        }

        var email = EmailNormalizer.Normalize(configuredEmail);

        var dbContext = services.GetRequiredService<KasioDbContext>();

        if (await dbContext.Users.AnyAsync(user => user.Email == email, ct))
            return;

        var adminRole = await dbContext.Roles
            .FirstOrDefaultAsync(role => role.Name == RoleNames.Admin, ct);

        if (adminRole is null)
            throw new InvalidOperationException($"Role hệ thống '{RoleNames.Admin}' chưa được seed.");

        var hasher = services.GetRequiredService<IPasswordHasher>();

        var fullName = configuration["SeedAdmin:FullName"];
        var phoneNumber = configuration["SeedAdmin:PhoneNumber"];
        var address = configuration["SeedAdmin:Address"];

        var created = User.Create(
            adminRole.Id,
            string.IsNullOrWhiteSpace(fullName) ? "Quản trị viên" : fullName,
            email,
            hasher.HashPassword(password),
            string.IsNullOrWhiteSpace(phoneNumber) ? "0000000000" : phoneNumber,
            string.IsNullOrWhiteSpace(address) ? "N/A" : address,
            Gender.Male,
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        if (created.IsError)
            throw new InvalidOperationException($"Không tạo được admin: {created.FirstError.Description}");

        // Seeding chạy trước khi CAP khởi động nên không publish domain event.
        ((IAggregateRoot)created.Value).ClearDomainEvents();
        dbContext.Users.Add(created.Value);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Đã tạo tài khoản admin {Email}.", email);
    }
}
