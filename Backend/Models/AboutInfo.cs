using Discord.WebSocket;

namespace PDPWebsite.Models;

public record AboutInfo(ulong Id, string Description, string? VisualName)
{
    public ulong Id { get; set; } = Id;
    public string Description { get; set; } = Description;
    public string? VisualName { get; set; } = VisualName;

    public void Deconstruct(out ulong Id, out string Description, out string? VisualName)
    {
        Id = this.Id;
        Description = this.Description;
        VisualName = this.VisualName;
    }
}

public record AboutInfoExtended(ulong Id, string Description, string RoleName, string RoleColor, string Avatar, string OriginalName, string? VisualName)
{
    public static explicit operator AboutInfo(AboutInfoExtended aboutInfoExtended)
    {
        return new AboutInfo(aboutInfoExtended.Id, aboutInfoExtended.Description, aboutInfoExtended.VisualName);
    }

    public static AboutInfoExtended FromInfo(AboutInfo aboutInfo, SocketGuildUser user, SocketRole role)
    {
        return new AboutInfoExtended(aboutInfo.Id, aboutInfo.Description, role.Name, role.Color.ToString()!, user.GetAvatarUrl(), user.DisplayName, aboutInfo.VisualName);
    }
}
