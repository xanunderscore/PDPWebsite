using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord;

[SlashCommand("voice", "Temp Voice related commands")]
[AllowedChannel(1065927404238942259)]
public partial class Voice : ISlashCommandProcessor
{
    private readonly SocketSlashCommand _arg;
    private readonly DiscordConnection _discord;
    private readonly RedisClient _redisClient;

    public Voice(SocketSlashCommand arg, DiscordConnection discord, RedisClient redis)
    {
        _arg = arg;
        _discord = discord;
        _redisClient = redis;
    }
}

[SlashCommand("voice-debug", "Temp Voice Debug related commands", GuildPermission.ManageChannels)]
[AllowedChannel(1065927404238942259)]
public partial class VoiceAdmin : ISlashCommandProcessor
{
    private readonly SocketSlashCommand _arg;
    private readonly DiscordConnection _discord;

    public VoiceAdmin(SocketSlashCommand arg, DiscordConnection discord)
    {
        _arg = arg;
        _discord = discord;
    }
}