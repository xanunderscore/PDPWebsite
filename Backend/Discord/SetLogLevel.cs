using Discord;
using Discord.WebSocket;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace PDPWebsite.Discord;

[SlashCommand("log-level", "Sets the log level for the bot", GuildPermission.ManageChannels)]
public class SetLogLevel : ISlashCommandProcessor
{
    private readonly DiscordConnection _connection;
    private readonly SocketSlashCommand _arg;

    public SetLogLevel(DiscordConnection connection, SocketSlashCommand arg)
    {
        _connection = connection;
        _arg = arg;
    }

    [SlashCommand("set", "The log level to set"), ResponseType(true)]
    public async Task Set([SlashCommand("level", "The log level to set")] LogLevel level)
    {
        _connection.SetLogLevel(level);
        await _arg.ModifyOriginalResponseAsync(opt => opt.Content = $"Set log level to {level}");
    }

    [SlashCommand("get", "Gets the current log level"), ResponseType(true)]
    public async Task Get()
    {
        var level = _connection.GetLogLevel();
        await _arg.ModifyOriginalResponseAsync(opt => opt.Content = $"Current log level is {level}");
    }
}