using Kasio.Domain.Users;

namespace Kasio.Application.Common.Interfaces;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
