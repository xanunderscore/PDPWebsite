using System.Net.WebSockets;
using System.Reflection;
using System.Text.RegularExpressions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using PDPWebsite.Discord;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace PDPWebsite.Services;

public class DiscordConnection : IDisposable
{
    public static UniversalisClient UniversalisClient { get; private set; } = null!;
    public DiscordSocketClient DiscordClient { get; }
    public SocketGuild? Guild { get; private set; }
    public SocketTextChannel? LogChannel;
    private EnvironmentContainer _environmentContainer;
    private readonly IServiceProvider _provider;
    private readonly ILogger _logger;
    private readonly GameClient _gameClient;
    private CancellationTokenSource _cts = new();
    private Type[] _slashCommandProcessors = Array.Empty<Type>();
    private NLog.LogLevel _logLevel = NLog.LogLevel.Warn;
    private List<Game> Games { get; } = new()
    {
        new("Universalis", ActivityType.Watching),
        new("with the market"),
        new("with the economy"),
        new("Catching errors", ActivityType.CustomStatus),
        new("the schedule", ActivityType.Watching)
    };

    public static Action? OnReady { get; set; }
    public static DiscordConnection? Instance { get; set; }

    public DiscordConnection(ILogger<DiscordConnection> logger, EnvironmentContainer environmentContainer, GameClient client, IServiceProvider provider)
    {
        UniversalisClient = new UniversalisClient();

        DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        });
        DiscordClient.Log += Log;
        DiscordClient.Ready += Ready;
        DiscordClient.SlashCommandExecuted += SlashCommandExecuted;
        _logger = logger;
        _environmentContainer = environmentContainer;
        _provider = provider;
        _gameClient = client;
        Instance = this;
    }

    public async Task Start()
    {
        await DiscordClient.LoginAsync(TokenType.Bot, _environmentContainer.Get("DISCORD_TOKEN"));
        await DiscordClient.StartAsync();
    }

    public Task Log(LogMessage arg)
    {
        if (arg.Exception is WebSocketException or WebSocketClosedException or GatewayReconnectException || arg.Exception?.InnerException is WebSocketException or WebSocketClosedException or GatewayReconnectException)
            return Task.CompletedTask;

        _logger.Log(arg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        }, arg.Exception, arg.Message);
        return Task.CompletedTask;
    }

    public async Task SetActivity()
    {
        try
        {
            if (DiscordClient.ConnectionState != ConnectionState.Connected)
            {
                if (_cts.IsCancellationRequested)
                    return;
                await Task.Delay(1000, _cts.Token);
                SetActivity();
            }

            var next = Games[Random.Shared.Next(Games.Count)];
            await DiscordClient.SetActivityAsync(next);
            await Task.Delay(60000, _cts.Token);
            SetActivity();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task Ready()
    {
        SetActivity();
        CreateCommands();
        LogChannel = (SocketTextChannel)await DiscordClient.GetChannelAsync(1156096156124844084);
        Guild = DiscordClient.GetGuild(1065654204129083432);
        OnReady?.Invoke();
    }

    private async Task CreateCommands()
    {
        try
        {
            foreach (var discordClientGuild in DiscordClient.Guilds)
            {
                var commands = await DiscordClient.Rest.GetGuildApplicationCommands(discordClientGuild.Id);
                foreach (var restGuildCommand in commands)
                {
                    await restGuildCommand.DeleteAsync();
                }
            }
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            await Log(new LogMessage(LogSeverity.Error, "UniversalisBot", json, exception));
        }

        _slashCommandProcessors = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Any(t => t == typeof(ISlashCommandProcessor))).ToArray();

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
                    var paramType = required ? parameterInfo.ParameterType : Nullable.GetUnderlyingType(parameterInfo.ParameterType)!;
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
                        _ => throw new ArgumentOutOfRangeException(nameof(paramType), paramType, $"Could not match type with {paramType.Name}")
                    };
                    if (paramType.IsEnum)
                    {
                        //check if enum has more than 25 values
                        var values = Enum.GetValues(paramType);
                        if (values.Length > 25)
                        {
                            _logger.LogError($"Enum {paramType.Name} has more than 25 values, this is not supported by discord.");
                            goto enumEscape;
                        }
                        foreach (var value in values)
                        {
                            slashCommandParamBuilder.AddChoice(value.ToString()!, (int)value);
                        }
                    }
                    slashCommandParamBuilder.WithType(slashCommandOptionType);
                    slashCommandParamBuilder.WithRequired(required);
                    slashCommandOptionBuilder.AddOption(slashCommandParamBuilder);
                }
                slashCommandBuiler.AddOption(slashCommandOptionBuilder);
            }
            commandBuilders.Add(slashCommandBuiler);
        enumEscape:;
        }

        try
        {
            foreach (var discordClientGuild in DiscordClient.Guilds)
            {
                foreach (var commandBuilder in commandBuilders)
                {
                    await DiscordClient.Rest.CreateGuildCommand(commandBuilder.Build(), discordClientGuild.Id);
                }
            }
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
            await arg.RespondAsync($"No command found for {command}... good job you found a bug in discord.", ephemeral: true);
            return;
        }
        var channels = type.GetCustomAttributes<AllowedChannelAttribute>().Select(t => t.ChannelId).ToArray();
        if (channels.Any() && !channels.Contains(arg.Channel.Id) && arg.Channel.Id != 1126938489322221598)
        {
            await arg.RespondAsync($"This command can only be used in the following channels: {string.Join(", ", channels.Select(t => $"<#{t}>"))}", ephemeral: true);
            return;
        }
        var instance = ActivatorUtilities.CreateInstance(_provider, type, arg);
        var method = type.GetMethods().FirstOrDefault(t => t.IsSameCommand(subCommand!));
        if (method == null)
        {
            await arg.RespondAsync($"No sub command found for {subCommand}... good job you found a bug in discord.", ephemeral: true);
            return;
        }
        var responseType = method.GetCustomAttribute<ResponseTypeAttribute>() ?? new ResponseTypeAttribute();
        await arg.RespondAsync("Thinking...", isTTS: responseType.IsTts, ephemeral: responseType.IsEphemeral);
        var parameters = method.GetParameters();
        var args = new List<object?>();
        var paramsOptions = arg.Data.Options.First().Options;
        foreach (var parameter in parameters)
        {
            foreach (var paramOption in paramsOptions)
            {
                if (parameter.IsSameCommand(paramOption.Name))
                {
                    args.Add(parameter.ParameterType switch
                    {
                        { IsEnum: true } => Enum.GetValues(parameter.ParameterType).GetValue(Convert.ToInt32(paramOption.Value)),
                        { } t when t == typeof(string) => paramOption.Value,
                        { } t when t == typeof(bool) => paramOption.Value,
                        { } t when t == typeof(int) => Convert.ToInt32(paramOption.Value),
                        { } t when t == typeof(ulong) => Convert.ToUInt64(paramOption.Value),
                        { } t when t == typeof(long) => paramOption.Value,
                        { } t when t == typeof(uint) => Convert.ToUInt32(paramOption.Value),  
                        { } t when t == typeof(short) => Convert.ToInt16(paramOption.Value),
                        { } t when t == typeof(ushort) => Convert.ToUInt16(paramOption.Value),
                        { } t when t == typeof(byte) => Convert.ToByte(paramOption.Value),
                        { } t when t == typeof(sbyte) => Convert.ToSByte(paramOption.Value),
                        { } t when t == typeof(double) => paramOption.Value,
                        { } t when t == typeof(float) => paramOption.Value,
                        { } t when t == typeof(decimal) => paramOption.Value,
                        { } t when t == typeof(DateTime) => DateTime.Parse((string)paramOption.Value),
                        { } t when t == typeof(DateTimeOffset) => DateTimeOffset.Parse((string)paramOption.Value),
                        { } t when t == typeof(TimeSpan) => TimeSpan.Parse((string)paramOption.Value),
                        { } t when t == typeof(SocketRole) => paramOption.Value,
                        { } t when t == typeof(SocketUser) => paramOption.Value,
                        { } t when t == typeof(SocketChannel) => paramOption.Value,
                        { } t when t == typeof(Attachment) => paramOption.Value,
                        _ => throw new ArgumentOutOfRangeException(nameof(parameter.ParameterType), parameter.ParameterType, $"Could not match type with {parameter.ParameterType.Name}")
                    });
                }
            }
        }
        while (args.Count != parameters.Length)
            args.Add(null);

        if (method.ReturnType == typeof(Task))
            await ((Task?)method.Invoke(instance, args.ToArray()))!;
        else
            method.Invoke(instance, args.ToArray());
    }

    public async Task DisposeAsync()
    {
        _cts.Cancel();
        await DiscordClient.StopAsync();
        await DiscordClient.LogoutAsync();
        await DiscordClient.DisposeAsync();
        UniversalisClient.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public bool ShouldLog(NLog.LogLevel logEventLevel)
    {
        return logEventLevel <= _logLevel;
    }

    public void SetLogLevel(LogLevel level)
    {
        _logLevel = level switch
        {
            LogLevel.Critical => _logLevel = NLog.LogLevel.Fatal,
            LogLevel.Error => _logLevel = NLog.LogLevel.Error,
            LogLevel.Warning => _logLevel = NLog.LogLevel.Warn,
            LogLevel.Information => _logLevel = NLog.LogLevel.Info,
            LogLevel.Debug => _logLevel = NLog.LogLevel.Debug,
            LogLevel.Trace => _logLevel = NLog.LogLevel.Trace,
            _ => _logLevel = NLog.LogLevel.Info
        };
    }

    public NLog.LogLevel GetLogLevel() => _logLevel;
}

static class DiscordExtensions
{
    public static Regex CapitalLetters = new("[A-Z]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static async Task DownloadAllImages(this DiscordSocketClient client, ulong id)
    {
        var victoryPoses = (SocketTextChannel)await client.GetChannelAsync(id);
        var messages = await victoryPoses.GetMessagesAsync(1000).FlattenAsync();
        foreach (var message in messages)
        {
            foreach (var messageAttachment in message.Attachments)
            {
                var filenameFiltered = messageAttachment.Filename.Replace("SPOILER_", "");
                var name = !filenameFiltered.StartsWith("image") ? filenameFiltered : message.Id + "_" + filenameFiltered;
                await using var stream = new MemoryStream();
                await messageAttachment.DownloadAsync(stream);
                var path = Path.Combine("victoryposes", name);
                if (messageAttachment.IsSpoiler())
                {
                    path = Path.Combine("victoryposes", "spoilers", name);
                }
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

    public static bool TryGetHighestRole(this SocketGuildUser user, out SocketRole? role)
    {
        var k = user.Roles;
        return k.TryGetRole(1065654859094822993, out role) || k.TryGetRole(1065988868152766527, out role) || k.TryGetRole(1158395243494899742, out role) || k.TryGetRole(1065662664434516069, out role);
    }

    public static bool TryGetRole(this IEnumerable<SocketRole> roles, ulong roleId, out SocketRole? role)
    {
        role = roles.FirstOrDefault(t => t.Id == roleId);
        return role != null;
    }

    public static void SanitizeName(this SlashCommandOptionBuilder builder)
    {
        var name = builder.Name;
        var matches = CapitalLetters.Matches(name);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            name = name.Replace(match.Value, "-" + match.Value.ToLower());
        }
        builder.WithName(name.ToLower());
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
}