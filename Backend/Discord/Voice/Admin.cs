using Discord;

namespace PDPWebsite.Discord;

public partial class Voice
{
    [SlashCommand("setup", "Sets up the interaction buttons for voice"), ResponseType(true)]
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
                .WithButton("Claim", "claim", emote: new Emoji("\uD83D\uDD18")))
            .Build();
        await _arg.Channel.SendMessageAsync(embed: embed, components: components);
        await _arg.DeleteOriginalResponseAsync();
    }
}
