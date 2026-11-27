using ErrorOr;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using Xunit;

namespace Kasio.UnitTests;

public sealed class UserEmailTests
{
    private static ErrorOr<User> CreateUser(string email) => User.Create(
        new RoleId(Guid.NewGuid()),
        "Nguyen Van A",
        email,
        "hash",
        "0901234567",
        "Ha Noi",
        Gender.Male,
        new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    [Theory]
    [InlineData("  Admin@Example.COM ", "admin@example.com")]
    [InlineData("ADMIN@EXAMPLE.COM", "admin@example.com")]
    [InlineData("admin@example.com", "admin@example.com")]
    public void Create_stores_email_lowercased_and_trimmed(string input, string expected)
    {
        var result = CreateUser(input);

        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value.Email);
    }

    [Fact]
    public void Normalize_is_idempotent()
    {
        var once = EmailNormalizer.Normalize("  Mixed@Case.Com ");

        Assert.Equal(once, EmailNormalizer.Normalize(once));
    }
}
