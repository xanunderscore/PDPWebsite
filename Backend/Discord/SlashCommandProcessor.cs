using System.Reflection;
using Discord;

namespace PDPWebsite.Discord;

public interface ISlashCommandProcessor
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
public class SlashCommandAttribute : Attribute
{
    public SlashCommandAttribute(string name)
    {
        Name = name;
        Description = "";
    }

    public SlashCommandAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public SlashCommandAttribute(string name, string description, GuildPermission permission) : this(name, description)
    {
        Permission = permission;
    }

    public string Name { get; }
    public string Description { get; }
    public GuildPermission? Permission { get; }

    public void SetBuilder(SlashCommandBuilder builder)
    {
        builder.WithName(Name);
        if (!string.IsNullOrWhiteSpace(Description))
            builder.WithDescription(Description);
        builder.WithDefaultMemberPermissions(Permission);
    }

    public void SetBuilder(SlashCommandOptionBuilder builder)
    {
        builder.WithName(Name);
        if (!string.IsNullOrWhiteSpace(Description))
            builder.WithDescription(Description);
        if (Permission.HasValue)
            throw new InvalidOperationException("Permission cannot be set on a sub command or argument");
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class ResponseTypeAttribute : Attribute
{
    public ResponseTypeAttribute(bool isEphemeral = false, bool isTts = false)
    {
        IsEphemeral = isEphemeral;
        IsTts = isTts;
    }

    public bool IsEphemeral { get; }
    public bool IsTts { get; }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AllowedChannelAttribute : Attribute
{
    public AllowedChannelAttribute(ulong channelId)
    {
        ChannelId = channelId;
    }

    public ulong ChannelId { get; }
}

public static class Extensions
{
    public static bool IsSameCommand(this Type type, string name)
    {
        var attr = type.GetCustomAttribute<SlashCommandAttribute>();
        return attr != null ? attr.Name.SanitizeName() == name : type.Name.SanitizeName() == name;
    }

    public static bool IsSameCommand(this MethodInfo type, string name)
    {
        var attr = type.GetCustomAttribute<SlashCommandAttribute>();
        return attr != null ? attr.Name.SanitizeName() == name : type.Name.SanitizeName() == name;
    }

    public static bool IsSameCommand(this ParameterInfo type, string name)
    {
        var attr = type.GetCustomAttribute<SlashCommandAttribute>();
        return attr != null ? attr.Name.SanitizeName() == name : type.Name!.SanitizeName() == name;
    }
}