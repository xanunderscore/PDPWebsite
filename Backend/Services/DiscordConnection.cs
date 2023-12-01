using System.Collections.Concurrent;
using System.Net.WebSockets;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using LogLevel = NLog.LogLevel;

namespace PDPWebsite.Services;

public partial class DiscordConnection : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly EnvironmentContainer _environmentContainer;
    private readonly ILogger<DiscordConnection> _logger;
    private readonly IServiceProvider _provider;
    private readonly RedisClient _redisClient;
    private LogLevel _logLevel = LogLevel.Warn;
    private Type[] _slashCommandProcessors = Array.Empty<Type>();
    private SocketVoiceChannel _tempVoiceChannel = null!;
    public SocketTextChannel? LogChannel;

    /// <summary>
    ///     Key: VoiceChannelId
    ///     Value: UserId
    /// </summary>
    public ConcurrentDictionary<ulong, ulong> TempChannels;

    public DiscordConnection(ILogger<DiscordConnection> logger, EnvironmentContainer environmentContainer,
        RedisClient redisClient, IServiceProvider provider)
    {
        DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            SuppressUnknownDispatchWarnings = true,
            AlwaysDownloadUsers = true,
            ConnectionTimeout = 10000,
            DefaultRetryMode = RetryMode.AlwaysRetry
        });
        DiscordClient.Log += Log;
        DiscordClient.Ready += Ready;
        DiscordClient.SlashCommandExecuted += SlashCommandExecuted;
        DiscordClient.UserVoiceStateUpdated += UserVoiceStateUpdated;
        DiscordClient.ButtonExecuted += MessageInteractionExecuted;
        DiscordClient.SelectMenuExecuted += MessageInteractionExecuted;
        DiscordClient.ModalSubmitted += DiscordClientOnModalSubmitted;
        DiscordClient.PresenceUpdated += (_, _, _) => Task.CompletedTask;
        DiscordClient.GuildScheduledEventStarted += _ => Task.CompletedTask;
        DiscordClient.InviteCreated += _ => Task.CompletedTask;
        _logger = logger;
        _environmentContainer = environmentContainer;
        _provider = provider;
        _redisClient = redisClient;
        TempChannels = _redisClient.GetObj<ConcurrentDictionary<ulong, ulong>>("discord_temp_channels") ??
                       new ConcurrentDictionary<ulong, ulong>();
        Instance = this;
    }

    public DiscordSocketClient DiscordClient { get; }
    public SocketGuild? Guild { get; private set; }

    private List<Game> Games { get; } = new()
    {
        new Game("Universalis", ActivityType.Watching),
        new Game("with the market"),
        new Game("with the economy"),
        new Game("the schedule", ActivityType.Watching)
    };

    public static Action? OnReady { get; set; }
    public static DiscordConnection? Instance { get; set; }

    public async ValueTask DisposeAsync()
    {
        await DiscordClient.StopAsync();
        await DiscordClient.LogoutAsync();
        await DiscordClient.DisposeAsync();
        _redisClient.SetObj("discord_temp_channels", TempChannels);
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }

    public async Task Start()
    {
        if (!bool.Parse(_environmentContainer.Get("DISCORD_DISABLE")))
        {
            await DiscordClient.LoginAsync(TokenType.Bot, _environmentContainer.Get("DISCORD_TOKEN"));
            await DiscordClient.StartAsync();
        }
    }

    public Task Log(LogMessage arg)
    {
        if (arg.Exception is WebSocketException or WebSocketClosedException or GatewayReconnectException ||
            arg.Exception?.InnerException is WebSocketException or WebSocketClosedException
                or GatewayReconnectException)
            return Task.CompletedTask;

        _logger.Log(arg.Severity switch
        {
            LogSeverity.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
            LogSeverity.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            LogSeverity.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            LogSeverity.Info => Microsoft.Extensions.Logging.LogLevel.Information,
            LogSeverity.Verbose => Microsoft.Extensions.Logging.LogLevel.Trace,
            LogSeverity.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        }, arg.Exception, "{message}", arg.Message);
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
        LogChannel =
            (SocketTextChannel)await DiscordClient.GetChannelAsync(
                ulong.Parse(_environmentContainer.Get("DISCORD_LOG_CHANNEL")));
        Guild = DiscordClient.GetGuild(ulong.Parse(_environmentContainer.Get("DISCORD_GUILD")));
        _tempVoiceChannel =
            (SocketVoiceChannel)await DiscordClient.GetChannelAsync(
                ulong.Parse(_environmentContainer.Get("DISCORD_TEMP_VOICE")));
        OnReady?.Invoke();
    }

    public bool ShouldLog(LogLevel logEventLevel)
    {
        return logEventLevel < _logLevel;
    }

    public void SetLogLevel(Microsoft.Extensions.Logging.LogLevel level)
    {
        _logLevel = level switch
        {
            Microsoft.Extensions.Logging.LogLevel.Critical => _logLevel = LogLevel.Fatal,
            Microsoft.Extensions.Logging.LogLevel.Error => _logLevel = LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Warning => _logLevel = LogLevel.Warn,
            Microsoft.Extensions.Logging.LogLevel.Information => _logLevel = LogLevel.Info,
            Microsoft.Extensions.Logging.LogLevel.Debug => _logLevel = LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Trace => _logLevel = LogLevel.Trace,
            _ => _logLevel = LogLevel.Info
        };
    }

    public LogLevel GetLogLevel()
    {
        return _logLevel;
    }
}