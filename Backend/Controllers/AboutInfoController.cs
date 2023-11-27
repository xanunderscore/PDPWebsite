using Discord.WebSocket;
using PDPWebsite.Hubs;

namespace PDPWebsite.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AboutInfoController : ControllerBase
{
    private readonly EnvironmentContainer _container;
    private readonly Database _database;
    private readonly DiscordConnection _discord;
    private readonly IHubContext<MainHub> _hub;

    public AboutInfoController(Database database, EnvironmentContainer container, DiscordConnection discord,
        IHubContext<MainHub> hub)
    {
        _database = database;
        _container = container;
        _discord = discord;
        _hub = hub;
    }

    private SocketGuildUser[] GetDiscordUsers()
    {
        var roles = _container.Get("DISCORD_ABOUT_ROLES").Split(',').Select(ulong.Parse).ToArray();
        var users = new List<SocketGuildUser>();
        foreach (var role in roles) users.AddRange(_discord.Guild!.GetRoleUsers(role).OrderBy(t => t.Id));
        return users.DistinctBy(t => t.Id).ToArray();
    }

    [HttpGet]
    public async Task<IActionResult> GetAboutInfo()
    {
        var aboutInfo = await _database.AboutInfos.ToListAsync();
        var roles = _container.Get("DISCORD_ABOUT_ROLES").Split(',').Select(ulong.Parse).ToArray();
        var users = GetDiscordUsers();
        var ret = new List<AboutInfoExtended>();
        foreach (var socketGuildUser in users)
        {
            if (!socketGuildUser.TryGetHighestRole(roles, out var role))
                continue;
            ret.Add(aboutInfo.Any(t => t.Id == socketGuildUser.Id)
                ? AboutInfoExtended.FromInfo(aboutInfo.First(t => t.Id == socketGuildUser.Id), socketGuildUser, role!)
                : new AboutInfoExtended(socketGuildUser.Id, "", role!.Name, role.Color.ToString()!,
                    socketGuildUser.GetDisplayAvatarUrl(), socketGuildUser.DisplayName, null));
        }

        return Ok(ret);
    }

    [HttpGet]
    [ServiceFilter(typeof(AuthFilter))]
    [Route("users")]
    public async Task<IActionResult> GetUsers()
    {
        var aboutInfo = await _database.AboutInfos.ToListAsync();
        var users = GetDiscordUsers();
        return Ok(users.Select(t => new
        {
            t.Id, Name = aboutInfo.FirstOrDefault(f => f.Id == t.Id)?.VisualName ?? t.DisplayName,
            Avatar = t.GetDisplayAvatarUrl()
        }));
    }

    [HttpPut]
    [ServiceFilter(typeof(AuthFilter))]
    public async Task<IActionResult> PostAboutInfo([FromBody] AboutInfo aboutInfo)
    {
        aboutInfo.VisualName = aboutInfo.VisualName?.Trim();
        aboutInfo.Description = aboutInfo.Description.Trim();
        if (string.IsNullOrWhiteSpace(aboutInfo.Description))
        {
            var saved = _database.AboutInfos.FirstOrDefault(t => t.Id == aboutInfo.Id);
            if (saved != null)
            {
                _database.AboutInfos.Remove(saved);
                await _database.SaveChangesAsync();
                await _hub.Clients.All.SendAsync("AboutInfoDeleted", aboutInfo.Id);
                return Ok();
            }
        }

        if (string.IsNullOrWhiteSpace(aboutInfo.VisualName))
            aboutInfo.VisualName = null;

        if (await _database.AboutInfos.AnyAsync(t => t.Id == aboutInfo.Id))
            _database.AboutInfos.Update(aboutInfo);
        else
            await _database.AboutInfos.AddAsync(aboutInfo);
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("AboutInfoUpdated", aboutInfo);
        return Ok();
    }
}