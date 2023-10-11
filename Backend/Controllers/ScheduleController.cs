namespace PDPWebsite.Controllers;

[ApiController, ServiceFilter(typeof(AuthFilter))]
[Route("/api/[controller]/")]
public class ScheduleController : ControllerBase
{
    private readonly Database _database;
    private readonly DiscordConnection _discord;
    private readonly IHubContext _hub;

    public ScheduleController(Database database, DiscordConnection discord, IHubContext hub)
    {
        _database = database;
        _discord = discord;
        _hub = hub;
    }

    [HttpGet]
    [Route("all")]
    public async Task<IActionResult> GetSchedule()
    {
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Select(t => ScheduleHttp.FromSchedule(t, _discord)));
    }

    [HttpPost]
    [Route("add")]
    public async Task<IActionResult> AddSchedule([FromBody] Schedule schedule)
    {

        await _database.Schedules.AddAsync(schedule);
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleAdded", ScheduleHttp.FromSchedule(schedule, _discord));
        return Ok();
    }

    [HttpPut]
    [Route("update")]
    public async Task<IActionResult> UpdateSchedule([FromBody] Schedule schedule)
    {
        _database.Schedules.Update(schedule);
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleUpdated", ScheduleHttp.FromSchedule(schedule, _discord));
        return Ok();
    }

    [HttpDelete]
    [Route("delete")]
    public async Task<IActionResult> DeleteSchedule([FromBody] Schedule schedule)
    {
        _database.Schedules.Remove(schedule);
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleDeleted", schedule.Id);
        return Ok();
    }
}

public record ScheduleHttp(Guid Id, string Name, ulong HostId, string HostName, TimeSpan Duration, DateTime At)
{
    public static ScheduleHttp FromSchedule(Schedule schedule, DiscordConnection discord)
    {
        return new ScheduleHttp(schedule.Id, schedule.Name, schedule.HostId, discord.Guild!.GetUser(schedule.HostId).DisplayName, schedule.Duration, schedule.At);
    }
}