using System.Reflection;
using Discord;

namespace PDPWebsite.Discord;

public interface ISlashCommandProcessor
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
public class SlashCommandAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }

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

    public void SetBuilder(SlashCommandBuilder builder)
    {
        builder.WithName(Name);
        if (!string.IsNullOrWhiteSpace(Description))
            builder.WithDescription(Description);
    }

    public void SetBuilder(SlashCommandOptionBuilder builder)
    {
        builder.WithName(Name);
        if (!string.IsNullOrWhiteSpace(Description))
            builder.WithDescription(Description);
    }
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