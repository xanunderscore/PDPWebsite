using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace PDPWebsite.Services;

public class RedisClient
{
    private static TimeSpan expireConstant = new(7, 0, 0, 0);

    public ConnectionMultiplexer Connection { get; set; } = ConnectionMultiplexer.Connect("localhost");

    private IDatabase GetDatabase() => Connection.GetDatabase();

    public string? Get(string key)
    {
        var db = GetDatabase();
        return db.StringGet(key);
    }

    public void Set(string key, string value, TimeSpan? expire = null)
    {
        var db = GetDatabase();
        db.StringSet(key, value, expire ?? expireConstant);
    }

    public T? GetObj<T>(string key)
    {
        var db = GetDatabase();
        return db.JSON().Get<T>(key);
    }

    public void SetObj<T>(string key, T value, TimeSpan? expire = null) where T : notnull
    {
        var db = GetDatabase();
        db.JSON().Set(key, "$", value);
        SetExpire(key, expire ?? expireConstant);
    }

    public void SetExpire(string key, TimeSpan timeSpan)
    {
        var db = GetDatabase();
        db.KeyExpire(key, timeSpan);
    }
}
