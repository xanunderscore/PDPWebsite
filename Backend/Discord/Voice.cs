using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord;

[SlashCommand("voice", "Temp Voice related commands"), AllowedChannel(1065927404238942259)]
public partial class Voice : ISlashCommandProcessor
{
    private ILogger<Market> _logger;
    private SocketSlashCommand _arg;
    private DiscordConnection _discord;
    private RedisClient _redisClient;

    public Voice(SocketSlashCommand arg, ILogger<Market> logger, DiscordConnection discord, RedisClient redis)
    {
        _arg = arg;
        _logger = logger;
        _discord = discord;
        _redisClient = redis;
    }
}

[SlashCommand("voice-debug", "Temp Voice Debug related commands", GuildPermission.ManageChannels), AllowedChannel(1065927404238942259)]
public partial class VoiceAdmin : ISlashCommandProcessor
{
    private ILogger<Market> _logger;
    private SocketSlashCommand _arg;
    private DiscordConnection _discord;
    private RedisClient _redisClient;

    public VoiceAdmin(SocketSlashCommand arg, ILogger<Market> logger, DiscordConnection discord, RedisClient redis)
    {
        _arg = arg;
        _logger = logger;
        _discord = discord;
        _redisClient = redis;
    }
}