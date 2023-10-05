using System.Linq;

namespace PDPWebsite.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AboutInfoController : ControllerBase
{
    private readonly Database _database;
    private readonly DiscordConnection _discord;

    public AboutInfoController(Database database, DiscordConnection discord)
    {
        _database = database;
        _discord = discord;
    }

    [HttpGet]
    public async Task<IActionResult> GetAboutInfo()
    {
        var aboutInfo = await _database.AboutInfos.ToListAsync();
        var users = _discord.Guild!.GetRoleUsers(1065654859094822993).Concat(_discord.Guild!.GetRoleUsers(1065988868152766527)).Concat(_discord.Guild!.GetRoleUsers(1158395243494899742)).Concat(_discord.Guild!.GetRoleUsers(1065662664434516069)).DistinctBy(t => t.Id).ToArray();
        var ret = new List<AboutInfoExtended>();
        foreach (var socketGuildUser in users)
        {
            if (!socketGuildUser.TryGetHighestRole(out var role))
                continue;
            ret.Add(aboutInfo.Any(t => t.Id == socketGuildUser.Id)
                ? AboutInfoExtended.FromInfo(aboutInfo.First(t => t.Id == socketGuildUser.Id), socketGuildUser, role!)
                : new AboutInfoExtended(socketGuildUser.Id, "", role!.Name, role.Color.ToString()!, socketGuildUser.GetAvatarUrl(), socketGuildUser.DisplayName, null));
        }
        return Ok(ret);
    }

    [HttpPut, ServiceFilter(typeof(AuthFilter))]
    public async Task<IActionResult> PostAboutInfo([FromBody] AboutInfo aboutInfo)
    {
        aboutInfo.VisualName = aboutInfo.VisualName?.Trim();
        aboutInfo.Description = aboutInfo.Description.Trim();

        if(string.IsNullOrWhiteSpace(aboutInfo.VisualName))
            aboutInfo.VisualName = null;

        if (await _database.AboutInfos.AnyAsync(t => t.Id == aboutInfo.Id))
        {
            _database.AboutInfos.Update(aboutInfo);
        }
        else
        {
            await _database.AboutInfos.AddAsync(aboutInfo);
        }
        await _database.SaveChangesAsync();
        return Ok();
    }
}