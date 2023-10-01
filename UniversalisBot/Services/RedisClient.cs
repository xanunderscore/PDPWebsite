using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace PDPWebsite.Services;

public class RedisClient
{
    public ConnectionMultiplexer Connection { get; set; } = ConnectionMultiplexer.Connect("localhost");

    private IDatabase GetDatabase() => Connection.GetDatabase();

    public string? Get(string key)
    {
        var db = GetDatabase();
        return db.StringGet(key);
    }

    public void Set(string key, string value)
    {
        var db = GetDatabase();
        db.StringSet(key, value);
    }

    public T? GetObj<T>(string key)
    {
        var db = GetDatabase();
        return db.JSON().Get<T>(key);
    }

    public void SetObj<T>(string key, T value) where T : notnull
    {
        var db = GetDatabase();
        db.JSON().Set(key, "$", value);
    }
}
