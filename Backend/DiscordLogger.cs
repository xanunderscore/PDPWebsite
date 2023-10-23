using Discord;
using Discord.WebSocket;
using NLog.Targets;

namespace PDPWebsite;

[Target("Discord")]
public class DiscordLogger : TargetWithLayout
{
    private DiscordConnection? Connection => DiscordConnection.Instance;
    private Queue<LogEventInfo> _logQueue = new();
    private Queue<Embed> _embeds = new();

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
            Task.Run(async () =>
            {
                while (Connection?.LogChannel is null)
                {
                    await Task.Delay(1000);
                }
                while (Connection.DiscordClient.LoginState == LoginState.LoggedIn)
                {
                    while (Connection.DiscordClient.ConnectionState != ConnectionState.Connected)
                    {
                        await Task.Delay(1000);
                    }
                    var embeds = new List<Embed>();
                    while (_embeds.TryDequeue(out var embed) && embeds.Count != 10)
                    {
                        embeds.Add(embed);
                    }
                    if (embeds.Count <= 0)
                        continue;
                    await Connection.LogChannel.SendMessageAsync(embeds: embeds.ToArray());
                    await Task.Delay(1000);
                }
            });
        };
    }

    private async Task WriteDiscord(LogEventInfo logEvent)
    {
        var message = Layout.Render(logEvent);
        var embed = new EmbedBuilder();
        embed.WithTitle(logEvent.Level.ToString());
        embed.WithDescription(message.Replace("™", "#").Replace("---", Environment.NewLine).Replace("``````", ""));
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
        _embeds.Enqueue(embed.Build());
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
