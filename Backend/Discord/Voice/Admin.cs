using System.Text;
using Discord;

namespace PDPWebsite.Discord;

public partial class VoiceAdmin
{
    [SlashCommand("setup", "Sets up the interaction buttons for voice")]
    [ResponseType(true)]
    public async Task Setup()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Voice Setup")
            .WithDescription("Click the button below to modify your temp voice channel")
            .WithColor(Color.Blue)
            .Build();
        var components = new ComponentBuilder()
            .AddRow(new ActionRowBuilder()
                .WithButton("Name", "rename", emote: new Emoji("\uD83D\uDCDD"))
                .WithButton("Claim", "claim", emote: new Emoji("\uD83D\uDD18"))
                .WithButton("Change VC Region", "change_region", emote: new Emoji("\uD83D\uDD09")))
            .Build();
        await _arg.Channel.SendMessageAsync(embed: embed, components: components);
        await _arg.DeleteOriginalResponseAsync();
    }

    [SlashCommand("current", "Shows current temp voices in memory")]
    [ResponseType(true)]
    public async Task Current()
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in _discord.TempChannels) sb.AppendLine($"Channel: <#{key}> Owner: <@{value}>");
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = new EmbedBuilder().WithTitle("Temp Voices stored in memory").WithDescription(sb.ToString())
                .Build();
            msg.Content = null;
        });
    }
}
