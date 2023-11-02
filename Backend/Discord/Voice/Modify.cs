using System.Text;
using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord;

public partial class Voice
{
    [SlashCommand("claim", "Claims the current channel if owner has left"), ResponseType(true)]
    public async Task Claim()
    {
        var user = (SocketGuildUser)_arg.User;
        var channel = user.VoiceChannel;
        if (!_discord.TempChannels.ContainsKey(channel.Id))
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Channel is not a temp channel");
            return;
        }
        var ownerId = _discord.TempChannels[channel.Id];
        if (channel.ConnectedUsers.Any(x => x.Id == ownerId))
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Owner is still in the channel");
            return;
        }
        _discord.TempChannels[channel.Id] = user.Id;
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Claimed channel");
    }

    [SlashCommand("rename", "Renames the voice channel."), ResponseType(true)]
    public async Task Rename([SlashCommand("name", "The name that should be applied to the channel")] string name)
    {
        var user = (SocketGuildUser)_arg.User;
        var channel = user.VoiceChannel;
        if (!_discord.TempChannels.ContainsKey(channel.Id))
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Channel is not a temp channel");
            return;
        }
        var ownerId = _discord.TempChannels[channel.Id];
        if (ownerId != user.Id)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "You are not the owner of this channel");
            return;
        }
        await channel.ModifyAsync(x =>
        {
            x.Name = name;
        });
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Renamed channel");
    }

    [SlashCommand("list-regions", "Lists all available voice regions"), ResponseType(true)]
    public async Task ListRegions()
    {
        var regions = await _discord.DiscordClient.GetVoiceRegionsAsync();
        var sb = new StringBuilder();
        foreach (var region in regions)
        {
            sb.AppendLine($"- {region.Name} -> `{region.Id}`");
        }
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = new EmbedBuilder().WithTitle("Voice Regions").WithDescription(sb.ToString()).Build();
        });
    }

    [SlashCommand("move", "Moves the current channel to a new region"), ResponseType(true)]
    public async Task Move([SlashCommand("region", "The region to move to")] string region)
    {
        var user = (SocketGuildUser)_arg.User;
        var channel = user.VoiceChannel;
        if (!_discord.TempChannels.ContainsKey(channel.Id))
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Channel is not a temp channel");
            return;
        }
        var ownerId = _discord.TempChannels[channel.Id];
        if (ownerId != user.Id)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "You are not the owner of this channel");
            return;
        }
        var regions = await _discord.DiscordClient.GetVoiceRegionsAsync();
        var targetRegion = regions.FirstOrDefault(x => string.Equals(x.Name, region, StringComparison.InvariantCultureIgnoreCase)) ?? regions.FirstOrDefault(x => string.Equals(x.Id, region, StringComparison.InvariantCultureIgnoreCase));
        if (targetRegion == null)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Region not found check `/voice list-regions`");
            return;
        }
        await channel.ModifyAsync(x =>
        {
            x.RTCRegion = targetRegion.Id;
        });
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Moved channel");
    }

    [SlashCommand("limit", "Sets the user limit of the channel"), ResponseType(true)]
    public async Task Limit([SlashCommand("limit", "The limit to set")] int limit)
    {
        var user = (SocketGuildUser)_arg.User;
        var channel = user.VoiceChannel;
        if (!_discord.TempChannels.ContainsKey(channel.Id))
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Channel is not a temp channel");
            return;
        }
        var ownerId = _discord.TempChannels[channel.Id];
        if (ownerId != user.Id)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "You are not the owner of this channel");
            return;
        }
        await channel.ModifyAsync(x =>
        {
            x.UserLimit = limit;
        });
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Set limit");
    }

    [SlashCommand("bitrate", "Sets or resets the bitrate of the channel"), ResponseType(true)]
    public async Task Bitrate([SlashCommand("input", "The bitrate to set input in bits or kbps")] int? bitrate)
    {
        var user = (SocketGuildUser)_arg.User;
        var channel = user.VoiceChannel;
        if (!_discord.TempChannels.ContainsKey(channel.Id))
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Channel is not a temp channel");
            return;
        }
        var ownerId = _discord.TempChannels[channel.Id];
        if (ownerId != user.Id)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "You are not the owner of this channel");
            return;
        }
        bitrate ??= 64000;
        if (bitrate is < 1000) bitrate *= 1000;
        if (bitrate is < 8000 or > 384000)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Bitrate must be between 8000 and 384000");
            return;
        }
        await channel.ModifyAsync(x =>
        {
            x.Bitrate = bitrate.Value;
        });
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Set bitrate to {bitrate/1000}kbps");
    }
}
