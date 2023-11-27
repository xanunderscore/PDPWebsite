using System.Reflection;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using PDPWebsite.Discord;

namespace PDPWebsite.Services;

public partial class DiscordConnection
{
    private async Task CreateCommands()
    {
        try
        {
            foreach (var discordClientGuild in DiscordClient.Guilds)
            {
                var commands = await DiscordClient.Rest.GetGuildApplicationCommands(discordClientGuild.Id);
                foreach (var restGuildCommand in commands) await restGuildCommand.DeleteAsync();
            }
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            await Log(new LogMessage(LogSeverity.Error, "UniversalisBot", json, exception));
        }

        _slashCommandProcessors = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetInterfaces().Any(x => x == typeof(ISlashCommandProcessor)) && t.IsClass).ToArray();

        var commandBuilders = new List<SlashCommandBuilder>();

        foreach (var slashCommandProcessor in _slashCommandProcessors)
        {
            var slashCommandBuiler = new SlashCommandBuilder();
            slashCommandBuiler.WithName(slashCommandProcessor.Name.SanitizeName());
            var slashCommand = slashCommandProcessor.GetCustomAttribute<SlashCommandAttribute>();
            slashCommand?.SetBuilder(slashCommandBuiler);
            var methods = slashCommandProcessor.GetMethods();
            foreach (var methodInfo in methods)
            {
                if (methodInfo.ReturnType != typeof(Task))
                    continue;
                var slashCommandAttribute = methodInfo.GetCustomAttribute<SlashCommandAttribute>();
                var slashCommandOptionBuilder = new SlashCommandOptionBuilder();
                slashCommandOptionBuilder.WithType(ApplicationCommandOptionType.SubCommand);
                slashCommandOptionBuilder.WithName(methodInfo.Name.SanitizeName());
                slashCommandAttribute?.SetBuilder(slashCommandOptionBuilder);
                foreach (var parameterInfo in methodInfo.GetParameters())
                {
                    var slashCommandParamBuilder = new SlashCommandOptionBuilder();
                    var slashCommandParamAttribute = parameterInfo.GetCustomAttribute<SlashCommandAttribute>();
                    slashCommandParamBuilder.WithName(parameterInfo.Name!.SanitizeName());
                    slashCommandParamAttribute?.SetBuilder(slashCommandParamBuilder);
                    var required = Nullable.GetUnderlyingType(parameterInfo.ParameterType) == null;
                    var paramType = required
                        ? parameterInfo.ParameterType
                        : Nullable.GetUnderlyingType(parameterInfo.ParameterType)!;
                    var slashCommandOptionType = paramType switch
                    {
                        { IsEnum: true } => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(string) => ApplicationCommandOptionType.String,
                        { } t when t == typeof(bool) => ApplicationCommandOptionType.Boolean,
                        { } t when t == typeof(int) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(ulong) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(long) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(uint) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(short) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(ushort) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(byte) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(sbyte) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(double) => ApplicationCommandOptionType.Number,
                        { } t when t == typeof(float) => ApplicationCommandOptionType.Number,
                        { } t when t == typeof(decimal) => ApplicationCommandOptionType.Number,
                        { } t when t == typeof(DateTime) => ApplicationCommandOptionType.String,
                        { } t when t == typeof(DateTimeOffset) => ApplicationCommandOptionType.String,
                        { } t when t == typeof(TimeSpan) => ApplicationCommandOptionType.String,
                        { } t when t == typeof(SocketRole) => ApplicationCommandOptionType.Role,
                        { } t when t == typeof(SocketUser) => ApplicationCommandOptionType.User,
                        { } t when t == typeof(SocketChannel) => ApplicationCommandOptionType.Channel,
                        { } t when t == typeof(Attachment) => ApplicationCommandOptionType.Attachment,
                        _ => throw new ArgumentOutOfRangeException(nameof(paramType), paramType,
                            $"Could not match type with {paramType.Name}")
                    };
                    if (paramType.IsEnum)
                    {
                        //check if enum has more than 25 values
                        var values = Enum.GetValues(paramType);
                        if (values.Length > 25)
                        {
                            _logger.LogError(
                                $"Enum {paramType.Name} has more than 25 values, this is not supported by discord.");
                            goto enumEscape;
                        }

                        foreach (var value in values) slashCommandParamBuilder.AddChoice(value.ToString()!, (int)value);
                    }

                    slashCommandParamBuilder.WithType(slashCommandOptionType);
                    slashCommandParamBuilder.WithRequired(required);
                    slashCommandOptionBuilder.AddOption(slashCommandParamBuilder);
                }

                slashCommandBuiler.AddOption(slashCommandOptionBuilder);
            }

            commandBuilders.Add(slashCommandBuiler);
            enumEscape: ;
        }

        try
        {
            foreach (var discordClientGuild in DiscordClient.Guilds)
            foreach (var commandBuilder in commandBuilders)
                await DiscordClient.Rest.CreateGuildCommand(commandBuilder.Build(), discordClientGuild.Id);
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            await Log(new LogMessage(LogSeverity.Error, "UniversalisBot", json, exception));
        }
    }

    private async Task SlashCommandExecuted(SocketSlashCommand arg)
    {
        var command = arg.Data.Name;
        var subCommand = arg.Data.Options.FirstOrDefault()?.Name;
        var type = _slashCommandProcessors.FirstOrDefault(t => t.IsSameCommand(command));
        if (type == null)
        {
            await arg.RespondAsync($"No command found for {command}... good job you found a bug in discord.",
                ephemeral: true);
            return;
        }

        var channels = type.GetCustomAttributes<AllowedChannelAttribute>().Select(t => t.ChannelId).ToArray();
        if (channels.Any() && !channels.Contains(arg.Channel.Id) &&
            arg.ChannelId != ulong.Parse(_environmentContainer.Get("DISCORD_ADMIN_CHANNEL")))
        {
            await arg.RespondAsync(
                $"This command can only be used in the following channels: {string.Join(", ", channels.Select(t => $"<#{t}>"))}",
                ephemeral: true);
            return;
        }

        var instance = ActivatorUtilities.CreateInstance(_provider, type, arg);
        var method = type.GetMethods().FirstOrDefault(t => t.IsSameCommand(subCommand!));
        if (method == null)
        {
            await arg.RespondAsync($"No sub command found for {subCommand}... good job you found a bug in discord.",
                ephemeral: true);
            return;
        }

        var responseType = method.GetCustomAttribute<ResponseTypeAttribute>() ?? new ResponseTypeAttribute();
        var args = new List<object>();
        try
        {
            await arg.RespondAsync("Thinking...", isTTS: responseType.IsTts, ephemeral: responseType.IsEphemeral);
            var parameters = method.GetParameters();
            var paramsOptions = arg.Data.Options.First().Options;
            foreach (var parameter in parameters)
            foreach (var paramOption in paramsOptions)
                if (parameter.IsSameCommand(paramOption.Name))
                {
                    var typeSafe = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                    args.Add((typeSafe switch
                    {
                        { IsEnum: true } => Enum.GetValues(typeSafe).GetValue(Convert.ToInt32(paramOption.Value)),
                        _ when typeSafe == typeof(string) => paramOption.Value,
                        _ when typeSafe == typeof(bool) => paramOption.Value,
                        _ when typeSafe == typeof(int) => Convert.ToInt32(paramOption.Value),
                        _ when typeSafe == typeof(ulong) => Convert.ToUInt64(paramOption.Value),
                        _ when typeSafe == typeof(long) => paramOption.Value,
                        _ when typeSafe == typeof(uint) => Convert.ToUInt32(paramOption.Value),
                        _ when typeSafe == typeof(short) => Convert.ToInt16(paramOption.Value),
                        _ when typeSafe == typeof(ushort) => Convert.ToUInt16(paramOption.Value),
                        _ when typeSafe == typeof(byte) => Convert.ToByte(paramOption.Value),
                        _ when typeSafe == typeof(sbyte) => Convert.ToSByte(paramOption.Value),
                        _ when typeSafe == typeof(double) => paramOption.Value,
                        _ when typeSafe == typeof(float) => paramOption.Value,
                        _ when typeSafe == typeof(decimal) => paramOption.Value,
                        _ when typeSafe == typeof(DateTime) => DateTime.Parse((string)paramOption.Value),
                        _ when typeSafe == typeof(DateTimeOffset) => DateTimeOffset.Parse((string)paramOption.Value),
                        _ when typeSafe == typeof(TimeSpan) => TimeSpan.Parse((string)paramOption.Value),
                        _ when typeSafe == typeof(SocketRole) => paramOption.Value,
                        _ when typeSafe == typeof(SocketUser) => paramOption.Value,
                        _ when typeSafe == typeof(SocketChannel) => paramOption.Value,
                        _ when typeSafe == typeof(Attachment) => paramOption.Value,
                        _ => throw new ArgumentOutOfRangeException(typeSafe.Name, typeSafe,
                            $"Could not match type with {typeSafe.Name}")
                    })!);
                }

            while (args.Count != parameters.Length)
                args.Add(null!);

            if (method.ReturnType == typeof(Task))
                await ((Task?)method.Invoke(instance, args.ToArray()))!;
            else
                method.Invoke(instance, args.ToArray());
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                $"SlashCommandExecuted failed while executing: `/{command} {subCommand}` with args: {string.Join(", ", args.Select(t => t.ToString()))}");
        }
    }
}