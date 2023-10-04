using Discord;
using Microsoft.AspNetCore.Mvc;
using PDPWebsite.Models;

namespace PDPWebsite.Controllers;

[ApiController, CorsHeader]
public class AuthController : ControllerBase
{
    private readonly RedisClient _rClient;
    private readonly DiscordConnection _discord;

    public AuthController(RedisClient rClient, DiscordConnection discord)
    {
        _rClient = rClient;
        _discord = discord;
    }

    [HttpPost]
    [Route("/api/auth/login")]
    public async Task<IActionResult> Login(ulong userId)
    {
        var roles = _discord.DiscordClient.GetGuild(1065654204129083432).GetUser(userId).Roles;
        var isHostOrAdmin = roles.Any(r => r.Id == 1065662664434516069 || r.Permissions.Has(GuildPermission.ManageChannels));
        if (!isHostOrAdmin)
        {
            return Unauthorized();
        }
        var token = Guid.NewGuid();
        var loginRecord = new Login(token, userId);
        _rClient.SetObj(token.ToString(), loginRecord);
        return Ok(token);
    }

    [HttpPost]
    [Route("/api/auth/refresh")]
    public async Task<IActionResult> Refresh([FromQuery] Guid token)
    {
        var loginRecord = _rClient.GetObj<Login>(token.ToString());
        if (loginRecord is null)
        {
            return Unauthorized();
        }
        var roles = _discord.DiscordClient.GetGuild(1065654204129083432).GetUser(loginRecord.DiscordId).Roles;
        var isHostOrAdmin = roles.Any(r => r.Id == 1065662664434516069 || r.Permissions.Has(GuildPermission.ManageChannels));
        if (!isHostOrAdmin)
        {
            return Unauthorized();
        }
        _rClient.SetExpire(token.ToString(), TimeSpan.FromDays(7));
        return Ok(token);
    }

    [HttpDelete]
    [Route("/api/auth/logout")]
    public async Task<IActionResult> Logout([FromQuery] Guid token)
    {
        var loginRecord = _rClient.GetObj<Login>(token.ToString());
        if (loginRecord is null)
        {
            return Unauthorized();
        }
        _rClient.SetExpire(token.ToString(), TimeSpan.Zero);
        return Ok();
    }
}
