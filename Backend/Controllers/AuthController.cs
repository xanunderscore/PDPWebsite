using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly RedisClient _rClient;
    private readonly DiscordConnection _discord;

    public AuthController(RedisClient rClient, DiscordConnection discord)
    {
        _rClient = rClient;
        _discord = discord;
    }

    [HttpGet]
    [Route("/api/auth/login")]
    public async Task<IActionResult> Login(ulong userId)
    {
        var user = _discord.DiscordClient.GetGuild(1065654204129083432).GetUser(userId);
        var isAllowed = user.TryGetHighestRole(out _);
        if (!isAllowed)
        {
            return Unauthorized();
        }
        var token = Guid.NewGuid();
        var loginRecord = new Login(token, userId);
        _rClient.SetObj(token.ToString(), loginRecord);
        return Ok(UserReturn.FromUser(user, token.ToString()));
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
        var user = _discord.DiscordClient.GetGuild(1065654204129083432).GetUser(loginRecord.DiscordId);
        var isAllowed = user.TryGetHighestRole(out _);
        if (!isAllowed)
        {
            return Unauthorized();
        }
        return Ok(UserReturn.FromUser(user, token));
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
        var user = _discord.DiscordClient.GetGuild(1065654204129083432).GetUser(loginRecord.DiscordId);
        var isAllowed = user.TryGetHighestRole(out _);
        if (!isAllowed)
        {
            return Unauthorized();
        }
        _rClient.SetExpire(token, TimeSpan.FromDays(7));
        return Ok(UserReturn.FromUser(user, token));
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
    public static UserReturn FromUser(SocketGuildUser user, string token)
    {
        user.TryGetHighestRole(out var role);
        return new UserReturn(user.Id, user.DisplayName, user.GetAvatarUrl(), role!.Name, token);
    }
}
