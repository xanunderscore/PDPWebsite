using System.Collections.Concurrent;
using System.Net.WebSockets;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;
using NLogLevel = NLog.LogLevel;

namespace PDPWebsite.Services;

public partial class DiscordConnection : IDisposable
{
    public DiscordSocketClient DiscordClient { get; }
    public SocketGuild? Guild { get; private set; }
    public SocketTextChannel? LogChannel;
    /// <summary>
    /// Key: VoiceChannelId
    /// Value: UserId
    /// </summary>
    public ConcurrentDictionary<ulong, ulong> TempChannels;
    private readonly EnvironmentContainer _environmentContainer;
    private readonly IServiceProvider _provider;
    private readonly ILogger<DiscordConnection> _logger;
    private readonly RedisClient _redisClient;
    private readonly CancellationTokenSource _cts = new();
    private Type[] _slashCommandProcessors = Array.Empty<Type>();
    private NLogLevel _logLevel = NLogLevel.Warn;
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
        TempChannels = _redisClient.GetObj<ConcurrentDictionary<ulong, ulong>>("discord_temp_channels") ?? new ConcurrentDictionary<ulong, ulong>();
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
            LogSeverity.Critical => MSLogLevel.Critical,
            LogSeverity.Error => MSLogLevel.Error,
            LogSeverity.Warning => MSLogLevel.Warning,
            LogSeverity.Info => MSLogLevel.Information,
            LogSeverity.Verbose => MSLogLevel.Trace,
            LogSeverity.Debug => MSLogLevel.Debug,
            _ => MSLogLevel.Information
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
        OnReady?.Invoke();
    }

    public async Task DisposeAsync()
    {
        await DiscordClient.StopAsync();
        await DiscordClient.LogoutAsync();
        await DiscordClient.DisposeAsync();
        _redisClient.SetObj("discord_temp_channels", TempChannels);
        _cts.Cancel();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public bool ShouldLog(NLogLevel logEventLevel)
    {
        return logEventLevel < _logLevel;
    }

    public void SetLogLevel(MSLogLevel level)
    {
        _logLevel = level switch
        {
            MSLogLevel.Critical => _logLevel = NLogLevel.Fatal,
            MSLogLevel.Error => _logLevel = NLogLevel.Error,
            MSLogLevel.Warning => _logLevel = NLogLevel.Warn,
            MSLogLevel.Information => _logLevel = NLogLevel.Info,
            MSLogLevel.Debug => _logLevel = NLogLevel.Debug,
            MSLogLevel.Trace => _logLevel = NLogLevel.Trace,
            _ => _logLevel = NLogLevel.Info
        };
    }

    public NLogLevel GetLogLevel() => _logLevel;
}