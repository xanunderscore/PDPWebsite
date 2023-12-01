using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Services;

public partial class DiscordConnection
{
    private async Task DiscordClientOnModalSubmitted(SocketModal arg)
    {
        _logger.LogTrace("DiscordClientOnModalSubmitted: {arg}", arg);
        var user = (SocketGuildUser)arg.User;
        var channel = user.VoiceChannel;
        ulong ownerId;
        switch (arg.Data.CustomId)
        {
            case "rename":
                if (channel == null)
                {
                    await arg.RespondAsync("Must be in a voice channel", ephemeral: true);
                    return;
                }

                if (!TempChannels.ContainsKey(channel!.Id))
                {
                    await arg.RespondAsync("Channel is not a temp channel", ephemeral: true);
                    return;
                }

                ownerId = TempChannels[channel.Id];
                if (ownerId != user.Id)
                {
                    await arg.RespondAsync("You are not the owner of this channel", ephemeral: true);
                    return;
                }

                var name = arg.Data.Components.First(t => t.CustomId == "name").Value;
                var names = _redisClient.GetObj<Dictionary<ulong, string>>("voice_names") ??
                            new Dictionary<ulong, string>();
                names[user.Id] = name;
                _redisClient.SetObj("voice_names", names);
                await channel.ModifyAsync(x => x.Name = name);
                await arg.RespondAsync("Channel name updated", ephemeral: true);
                break;
            default:
                await arg.RespondAsync($"Could not find processor for modal with id {arg.Data.CustomId}",
                    ephemeral: true);
                break;
        }
    }

    private async Task MessageInteractionExecuted(SocketMessageComponent arg)
    {
        _logger.LogTrace("MessageInteractionExecuted: {customId}", arg.Data.CustomId);
        var user = (SocketGuildUser)arg.User;
        var channel = user.VoiceChannel;
        ulong ownerId;
        switch (arg.Data.CustomId)
        {
            case "rename":
                if (channel == null)
                {
                    await arg.RespondAsync("Must be in a voice channel", ephemeral: true);
                    return;
                }

                if (!TempChannels.ContainsKey(channel!.Id))
                {
                    await arg.RespondAsync("Channel is not a temp channel", ephemeral: true);
                    return;
                }

                ownerId = TempChannels[channel.Id];
                if (ownerId != user.Id)
                {
                    await arg.RespondAsync("You are not the owner of this channel", ephemeral: true);
                    return;
                }

                var renameTextField = new TextInputBuilder()
                    .WithLabel("Name")
                    .WithPlaceholder("Enter a new name for your voice channel")
                    .WithMinLength(1)
                    .WithMaxLength(100)
                    .WithCustomId("name");
                await arg.RespondWithModalAsync(new ModalBuilder().WithCustomId("rename").WithTitle("Voice Rename")
                    .AddTextInput(renameTextField).Build());
                break;
            case "claim":
                if (channel == null)
                {
                    await arg.RespondAsync("Must be in a voice channel", ephemeral: true);
                    return;
                }

                if (!TempChannels.ContainsKey(channel!.Id))
                {
                    await arg.RespondAsync("Channel is not a temp channel", ephemeral: true);
                    return;
                }

                ownerId = TempChannels[channel.Id];
                if (channel.ConnectedUsers.Any(x => x.Id == ownerId))
                {
                    await arg.RespondAsync("Owner is still in the channel", ephemeral: true);
                    return;
                }

                TempChannels[channel.Id] = user.Id;
                await arg.RespondAsync("Claimed channel", ephemeral: true);
                break;
            case "change_region":
                if (channel == null)
                {
                    await arg.RespondAsync("Must be in a voice channel", ephemeral: true);
                    return;
                }

                if (!TempChannels.ContainsKey(channel!.Id))
                {
                    await arg.RespondAsync("Channel is not a temp channel", ephemeral: true);
                    return;
                }

                ownerId = TempChannels[channel.Id];
                if (ownerId != user.Id)
                {
                    await arg.RespondAsync("You are not the owner of this channel", ephemeral: true);
                    return;
                }

                var regions = await DiscordClient.GetVoiceRegionsAsync();
                var regionSelect = new SelectMenuBuilder()
                    .WithCustomId("change_vc_region")
                    .WithPlaceholder("Select a region")
                    .WithOptions(regions.Select(t => new SelectMenuOptionBuilder(t.Name, t.Id)).ToList());
                await arg.RespondAsync("Select a voice region:",
                    components: new ComponentBuilder().WithSelectMenu(regionSelect).Build(), ephemeral: true);
                break;
            case "change_vc_region":
                if (channel == null)
                {
                    await arg.RespondAsync("Must be in a voice channel", ephemeral: true);
                    return;
                }

                if (!TempChannels.ContainsKey(channel!.Id))
                {
                    await arg.RespondAsync("Channel is not a temp channel", ephemeral: true);
                    return;
                }

                ownerId = TempChannels[channel.Id];
                if (ownerId != user.Id)
                {
                    await arg.RespondAsync("You are not the owner of this channel", ephemeral: true);
                    return;
                }

                var regionId = arg.Data.Values.First();
                await channel.ModifyAsync(t => t.RTCRegion = regionId);
                await arg.RespondAsync("Changed region", ephemeral: true);
                break;
            default:
                await arg.RespondAsync($"Could not find processor for button with id {arg.Data.CustomId}",
                    ephemeral: true);
                break;
        }
    }
}