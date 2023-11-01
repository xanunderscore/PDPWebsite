using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly RedisClient _rClient;
    private readonly DiscordConnection _discord;
    private readonly EnvironmentContainer _container;

    public AuthController(RedisClient rClient, EnvironmentContainer container, DiscordConnection discord)
    {
        _rClient = rClient;
        _discord = discord;
        _container = container;
    }

    [HttpGet]
    [Route("/api/auth/login")]
    public async Task<IActionResult> Login(ulong userId)
    {
        var user = _discord.Guild?.GetUser(userId);
        var roles = _container.Get("DISCORD_ABOUT_ROLES").Split(',').Select(ulong.Parse).ToArray();
        if (user is null)
        {
            return Unauthorized();
        }
        var isAllowed = user.TryGetHighestRole(roles, out var role);
        if (!isAllowed)
        {
            return Unauthorized();
        }
        var token = Guid.NewGuid();
        var loginRecord = new Login(token, userId);
        _rClient.SetObj(token.ToString(), loginRecord);
        return Ok(UserReturn.FromUser(user, role, token.ToString()));
    }

    [HttpGet, ServiceFilter(typeof(AuthFilter))]
    [Route("/api/auth/me")]
    public async Task<IActionResult> Me()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Split(" ").Last();
        var loginRecord = _rClient.GetObj<Login>(token);
        if (loginRecord is null)
        {
            return Unauthorized();
        }
        var user = _discord.Guild?.GetUser(loginRecord.DiscordId);
        var roles = _container.Get("DISCORD_ABOUT_ROLES").Split(',').Select(ulong.Parse).ToArray();
        if (user is null)
        {
            return Unauthorized();
        }
        var isAllowed = user.TryGetHighestRole(roles, out var role);
        if (!isAllowed)
        {
            return Unauthorized();
        }
        return Ok(UserReturn.FromUser(user, role, token));
    }

    [HttpPost, ServiceFilter(typeof(AuthFilter))]
    [Route("/api/auth/refresh")]
    public async Task<IActionResult> Refresh()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Split(" ").Last();
        var loginRecord = _rClient.GetObj<Login>(token);
        if (loginRecord is null)
        {
            return Unauthorized();
        }
        var user = _discord.Guild?.GetUser(loginRecord.DiscordId);
        var roles = _container.Get("DISCORD_ABOUT_ROLES").Split(',').Select(ulong.Parse).ToArray();
        if (user is null)
        {
            return Unauthorized();
        }
        var isAllowed = user.TryGetHighestRole(roles, out var role);
        if (!isAllowed)
        {
            return Unauthorized();
        }
        _rClient.SetExpire(token, TimeSpan.FromDays(7));
        return Ok(UserReturn.FromUser(user, role, token));
    }

    [HttpDelete]
    [Route("/api/auth/logout")]
    public async Task<IActionResult> Logout(string token)
    {
        var loginRecord = _rClient.GetObj<Login>(token);
        if (loginRecord is null)
        {
            return Unauthorized();
        }
        _rClient.SetExpire(token, TimeSpan.Zero);
        return Ok();
    }
}

public record UserReturn(ulong Id, string Name, string AvatarUrl, string Role, string Token)
{
    public static UserReturn FromUser(SocketGuildUser user, SocketRole? role, string token)
    {
        return new UserReturn(user.Id, user.DisplayName, user.GetAvatarUrl(), role!.Name, token);
    }
}
