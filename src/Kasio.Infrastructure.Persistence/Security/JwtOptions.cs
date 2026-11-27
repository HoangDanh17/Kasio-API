namespace Kasio.Infrastructure.Persistence.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = "Kasio";
    public string Audience { get; init; } = "Kasio.Client";
    public string Secret { get; init; } = null!;
    public int ExpiryMinutes { get; init; } = 60;
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
