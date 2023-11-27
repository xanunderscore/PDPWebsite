using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Services;

public static partial class DiscordExtensions
{
    private static readonly Regex CapitalLetters = CapitalLettersGenerator();

    public static async Task DownloadAllImages(this DiscordSocketClient client, ulong id)
    {
        var victoryPoses = (SocketTextChannel)await client.GetChannelAsync(id);
        var messages = await victoryPoses.GetMessagesAsync(1000).FlattenAsync();
        foreach (var message in messages)
        foreach (var messageAttachment in message.Attachments)
        {
            var filenameFiltered = messageAttachment.Filename.Replace("SPOILER_", "");
            var name = !filenameFiltered.StartsWith("image") ? filenameFiltered : message.Id + "_" + filenameFiltered;
            await using var stream = new MemoryStream();
            await messageAttachment.DownloadAsync(stream);
            var path = Path.Combine("victoryposes", name);
            if (messageAttachment.IsSpoiler()) path = Path.Combine("victoryposes", "spoilers", name);
            path = Path.Combine(Directory.GetCurrentDirectory(), path);
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (File.Exists(path))
                File.Delete(path);
            await using var fileStream = File.Create(path);
            await stream.CopyToAsync(fileStream);
            fileStream.Flush();
        }
    }

    public static async Task DownloadAsync(this IAttachment attachment, Stream stream)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(attachment.Url);
        await response.Content.CopyToAsync(stream);
        stream.Position = 0;
    }

    public static IEnumerable<SocketGuildUser> GetRoleUsers(this SocketGuild guild, ulong roleId)
    {
        var role = guild.GetRole(roleId);
        return role.Members;
    }

    public static bool TryGetHighestRole(this SocketGuildUser user, IEnumerable<ulong> roleList, out SocketRole? role)
    {
        var k = user.Roles;
        role = null;
        foreach (var roleId in roleList)
            if (k.TryGetRole(roleId, out role))
                return true;
        return false;
    }

    public static bool TryGetRole(this IEnumerable<SocketRole> roles, ulong roleId, out SocketRole? role)
    {
        role = roles.FirstOrDefault(t => t.Id == roleId);
        return role != null;
    }

    public static string SanitizeName(this string value)
    {
        var matches = CapitalLetters.Matches(value);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            value = value.Replace(match.Value, "-" + match.Value.ToLower());
        }

        return value.TrimStart('-');
    }

    [GeneratedRegex("[A-Z]", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex CapitalLettersGenerator();
}