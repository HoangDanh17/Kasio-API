namespace Kasio.Domain.Users;

/// <summary>
/// Email là định danh duy nhất của User; luôn lưu và tra cứu ở dạng lowercase để
/// "Admin@x.com" và "admin@x.com" không tạo thành hai tài khoản (BRD 3.2).
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
