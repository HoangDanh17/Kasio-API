using Kasio.Application.Common.Interfaces;

namespace Kasio.Infrastructure.Persistence.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;
    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool VerifyPassword(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
