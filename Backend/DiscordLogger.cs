using Discord;
using Discord.WebSocket;
using NLog.Targets;

namespace PDPWebsite;

[Target("Discord")]
public class DiscordLogger : TargetWithLayout
{
    private DiscordConnection? Connection => DiscordConnection.Instance;
    private Queue<LogEventInfo> _logQueue = new();

    public DiscordLogger()
    {
        DiscordConnection.OnReady += () =>
        {
            Task.Run(() =>
            {
                while (_logQueue.TryDequeue(out var logEvent))
                {
                    if (Connection?.LogChannel is null)
                    {
                        _logQueue.Enqueue(logEvent);
                        continue;
                    }
                    Write(logEvent);
                }
            });
        };
    }

    private async Task WriteDiscord(LogEventInfo logEvent)
    {
        var message = Layout.Render(logEvent);
        var embed = new EmbedBuilder();
        embed.WithTitle(logEvent.Level.ToString());
        embed.WithDescription(message.Replace("™", "#").Replace("---", Environment.NewLine));
        embed.WithColor(logEvent.Level.Ordinal switch
        {
            0 => Color.Blue,
            1 => Color.DarkTeal,
            2 => Color.LightGrey,
            3 => Color.Orange,
            4 => Color.Red,
            5 => Color.Purple,
            _ => Color.Default
        });
        await Connection!.LogChannel!.SendMessageAsync(embed: embed.Build());
    }

    protected override void Write(LogEventInfo logEvent)
    {
        if (Connection?.LogChannel is null)
        {
            _logQueue.Enqueue(logEvent);
            return;
        }
        if (Connection.ShouldLog(logEvent.Level))
            return;
        _ = WriteDiscord(logEvent);
    }
}
