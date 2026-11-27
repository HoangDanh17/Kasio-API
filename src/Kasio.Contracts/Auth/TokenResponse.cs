namespace Kasio.Contracts.Auth;

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
