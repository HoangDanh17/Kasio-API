using StackExchange.Redis;

namespace Kasio.Infrastructure.Cache;

/// <summary>
/// Kết nối Redis một lần và bất đồng bộ: request đầu tiên không bị chặn bởi Connect,
/// và app vẫn khởi động được khi Redis chưa lên (AbortOnConnectFail = false).
/// DI tạo đúng một instance nên ConnectAsync chỉ chạy một lần kể cả khi nhiều request cùng tới.
/// </summary>
public sealed class RedisConnection : IAsyncDisposable
{
    private readonly Lazy<Task<IConnectionMultiplexer>> _connection;

    public RedisConnection(string connectionString)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        _connection = new Lazy<Task<IConnectionMultiplexer>>(
            async () => await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false));
    }

    public async Task<IDatabase> GetDatabaseAsync(CancellationToken cancellationToken) =>
        (await _connection.Value.WaitAsync(cancellationToken).ConfigureAwait(false)).GetDatabase();

    public async ValueTask DisposeAsync()
    {
        if (!_connection.IsValueCreated || !_connection.Value.IsCompletedSuccessfully)
            return;

        var multiplexer = await _connection.Value.ConfigureAwait(false);
        await multiplexer.DisposeAsync().ConfigureAwait(false);
    }
}
