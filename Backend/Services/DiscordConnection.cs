using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using PDPWebsite.Discord;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace PDPWebsite.Services;

public class DiscordConnection : IDisposable
{
    public DiscordSocketClient DiscordClient { get; }
    public SocketGuild? Guild { get; private set; }
    public SocketTextChannel? LogChannel;
    /// <summary>
    /// Key: VoiceChannelId
    /// Value: UserId
    /// </summary>
    public Dictionary<ulong, ulong> TempChannels;
    private readonly EnvironmentContainer _environmentContainer;
    private readonly IServiceProvider _provider;
    private readonly ILogger<DiscordConnection> _logger;
    private readonly RedisClient _redisClient;
    private readonly CancellationTokenSource _cts = new();
    private Type[] _slashCommandProcessors = Array.Empty<Type>();
    private NLog.LogLevel _logLevel = NLog.LogLevel.Warn;
    private SocketVoiceChannel _tempVoiceChannel = null!;
    private List<Game> Games { get; } = new()
    {
        new("Universalis", ActivityType.Watching),
        new("with the market"),
        new("with the economy"),
        new("the schedule", ActivityType.Watching)
    };

    public static Action? OnReady { get; set; }
    public static DiscordConnection? Instance { get; set; }

    public DiscordConnection(ILogger<DiscordConnection> logger, EnvironmentContainer environmentContainer, RedisClient redisClient, IServiceProvider provider)
    {
        DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        });
        DiscordClient.Log += Log;
        DiscordClient.Ready += Ready;
        DiscordClient.SlashCommandExecuted += SlashCommandExecuted;
        DiscordClient.UserVoiceStateUpdated += UserVoiceStateUpdated;
        DiscordClient.ButtonExecuted += ButtonExecuted;
        _logger = logger;
        _environmentContainer = environmentContainer;
        _provider = provider;
        _redisClient = redisClient;
        TempChannels = _redisClient.GetObj<Dictionary<ulong, ulong>>("discord_temp_channels") ?? new Dictionary<ulong, ulong>();
        Instance = this;
    }

    private async Task ButtonExecuted(SocketMessageComponent arg)
    {
        _logger.LogTrace($"ButtonExecuted: {arg}");
        switch (arg.Data.CustomId)
        {
            case "rename":
                var renameTextField = new TextInputBuilder()
                    .WithPlaceholder("Enter a new name for your voice channel")
                    .WithMinLength(1)
                    .WithMaxLength(100)
                    .WithCustomId("rename_id")
                    .Build();
                var components = new ComponentBuilder()
                    .AddRow(new ActionRowBuilder().WithComponents(new List<IMessageComponent>{renameTextField})).Build();
                await arg.FollowupAsync("Further input required:", components: components);
                break;
            case "claim":
                var user = (SocketGuildUser)arg.User;
                var channel = user.VoiceChannel;
                if (!TempChannels.ContainsKey(channel.Id))
                {
                    await arg.RespondAsync("Channel is not a temp channel");
                    return;
                }
                var ownerId = TempChannels[channel.Id];
                if (channel.ConnectedUsers.Any(x => x.Id == ownerId))
                {
                    await arg.RespondAsync("Owner is still in the channel");
                    return;
                }
                TempChannels[channel.Id] = user.Id;
                await arg.RespondAsync("Claimed channel");
                break;
            default:
                await arg.RespondAsync($"Could not find processor for button with id {arg.Data.CustomId}", ephemeral: true);
                break;
        }
    }

    private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        try
        {
            if (before.VoiceChannel == after.VoiceChannel)
                return;
            _logger.LogTrace($"UserVoiceStateUpdated: {user}, {before}, {after}");
            if (before.VoiceChannel != null && (after.VoiceChannel == null || before.VoiceChannel.Id != after.VoiceChannel.Id))
            {
                _logger.LogTrace($"User: {user} disconnected from voice channel {before}");
                if (before.VoiceChannel.ConnectedUsers.Count == 0 && TempChannels.ContainsKey(before.VoiceChannel.Id))
                {
                    _logger.LogTrace($"This was the last user that left a temp channel");
                    await before.VoiceChannel.DeleteAsync();
                }
            }
            if (after.VoiceChannel?.Id == _tempVoiceChannel.Id)
            {
                _logger.LogTrace($"User: {user} connected to temp voice setup.");
                var name = _redisClient.GetObj<string>($"voice_name_{user.Id}") ?? user.Username;
                var channel = await Guild!.CreateVoiceChannelAsync(name, x =>
                {
                    x.CategoryId = _tempVoiceChannel.CategoryId;
                    x.PermissionOverwrites = new List<Overwrite>
                    {
                    new(Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(connect: PermValue.Allow, viewChannel: PermValue.Allow, speak: PermValue.Allow, sendMessages: PermValue.Allow)),
                    new(user.Id, PermissionTarget.User, new OverwritePermissions(connect: PermValue.Allow, viewChannel: PermValue.Allow, speak: PermValue.Allow, sendMessages: PermValue.Allow))
                    };
                });
                TempChannels.Add(channel.Id, user.Id);
                await ((SocketGuildUser)user).ModifyAsync(x => x.Channel = channel);
            }
        }
        catch (HttpException exception)
        {
            _logger.LogError(exception, "UserVoiceStateUpdated");
        }
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
#pragma warning disable CS4014
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
            await Task.Delay(TimeSpan.FromMinutes(30), _cts.Token);
            SetActivity();
        }
        catch (TaskCanceledException)
        {
        }
#pragma warning restore CS4014
    }

    private async Task Ready()
    {
#pragma warning disable CS4014
        SetActivity();
        CreateCommands();
#pragma warning restore CS4014
        LogChannel = (SocketTextChannel)await DiscordClient.GetChannelAsync(ulong.Parse(_environmentContainer.Get("DISCORD_LOG_CHANNEL")));
        Guild = DiscordClient.GetGuild(ulong.Parse(_environmentContainer.Get("DISCORD_GUILD")));
        _tempVoiceChannel = (SocketVoiceChannel)await DiscordClient.GetChannelAsync(ulong.Parse(_environmentContainer.Get("DISCORD_TEMP_VOICE")));
        var regions = await DiscordClient.GetVoiceRegionsAsync();
        var sb = new StringBuilder();
        foreach (var region in regions)
        {
            sb.AppendLine($"- {region.Name} -> `{region.Id}`");
        }
        _logger.LogInformation(sb.ToString());
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

        _slashCommandProcessors = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Any(x => x == typeof(ISlashCommandProcessor)) && t.IsClass).ToArray();

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
        if (channels.Any() && !channels.Contains(arg.Channel.Id) && arg.Channel.Id != _tempVoiceChannel.Id)
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
        _redisClient.SetObj("discord_temp_channels", TempChannels);
        _cts.Cancel();
        await DiscordClient.StopAsync();
        await DiscordClient.LogoutAsync();
        await DiscordClient.DisposeAsync();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public bool ShouldLog(NLog.LogLevel logEventLevel)
    {
        return logEventLevel < _logLevel;
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

static partial class DiscordExtensions
{
    public static Regex CapitalLetters = CapitalLettersGenerator();

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

    public static bool TryGetHighestRole(this SocketGuildUser user, IEnumerable<ulong> roleList, out SocketRole? role)
    {
        var k = user.Roles;
        role = null;
        foreach (var roleId in roleList)
        {
            if (k.TryGetRole(roleId, out role))
                return true;
        }
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