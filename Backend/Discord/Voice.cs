using Discord.WebSocket;

namespace PDPWebsite.Discord;

[SlashCommand("voice", "Temp Voice related commands"), AllowedChannel(1065927404238942259)]
public partial class Voice : ISlashCommandProcessor
{
    private ILogger<Market> _logger;
    private SocketSlashCommand _arg;
    private DiscordConnection _discord;

    public Voice(SocketSlashCommand arg, ILogger<Market> logger, DiscordConnection discord)
    {
        _arg = arg;
        _logger = logger;
        _discord = discord;
    }
}
