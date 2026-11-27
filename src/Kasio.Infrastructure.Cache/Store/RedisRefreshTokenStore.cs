using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using Kasio.Application.Common.Interfaces;
using Kasio.Domain.Users;
using Kasio.Infrastructure.Persistence.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Kasio.Infrastructure.Cache.Store;

public sealed class RedisRefreshTokenStore(RedisConnection connection, IOptions<JwtOptions> options)
    : IRefreshTokenStore
{
    private TimeSpan Lifetime => TimeSpan.FromDays(options.Value.RefreshTokenExpiryDays);

    public async Task<ErrorOr<UserId>> ConsumeAsync(string token, CancellationToken cancellationToken = default)
    {
        var db = await connection.GetDatabaseAsync(cancellationToken).ConfigureAwait(false);
        var hash = Hash(token);

        // GETDEL: lấy và xóa trong một lệnh nguyên tử,
        // hai request cùng token không thể cùng thành công.
        var value = await db.StringGetDeleteAsync(TokenKey(hash));

        if (value.IsNullOrEmpty)
        {
            // Token không còn, nhưng nếu có dấu vết "đã dùng" thì token cũ bị dùng lại.
            var usedBy = await db.StringGetAsync(UsedKey(hash));
            if (!usedBy.IsNullOrEmpty && Guid.TryParse(usedBy.ToString(), out var id))
                await RevokeAllAsync(new UserId(id), cancellationToken);

            return UserErrors.InvalidRefreshToken;
        }

        var userId = new UserId(Guid.Parse(value.ToString()));

        await db.SetRemoveAsync(UserKey(userId), hash);
        await db.StringSetAsync(UsedKey(hash), userId.Value.ToString(), Lifetime);

        return userId;
    }

    public async Task<string> IssueAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var db = await connection.GetDatabaseAsync(cancellationToken).ConfigureAwait(false);
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var hash = Hash(token);

        await db.StringSetAsync(TokenKey(hash), userId.Value.ToString(), Lifetime);
        await db.SetAddAsync(UserKey(userId), hash);
        await db.KeyExpireAsync(UserKey(userId), Lifetime);

        return token;
    }

    public async Task RevokeAllAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var db = await connection.GetDatabaseAsync(cancellationToken).ConfigureAwait(false);
        var hashes = await db.SetMembersAsync(UserKey(userId));

        var keys = hashes
            .Select(h => (RedisKey)TokenKey(h.ToString()))
            .Append(UserKey(userId))
            .ToArray();

        await db.KeyDeleteAsync(keys);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var db = await connection.GetDatabaseAsync(cancellationToken).ConfigureAwait(false);
        var hash = Hash(token);
        var value = await db.StringGetDeleteAsync(TokenKey(hash));

        if (!value.IsNullOrEmpty && Guid.TryParse(value.ToString(), out var id))
            await db.SetRemoveAsync(UserKey(new UserId(id)), hash);
    }

    private static string Hash(string token) =>
       Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string TokenKey(string hash) => $"rt:{hash}";
    private static string UsedKey(string hash) => $"rt:used:{hash}";
    private static string UserKey(UserId id) => $"rt:user:{id.Value}";
}
