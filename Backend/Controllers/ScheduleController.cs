using PDPWebsite.Hubs;

namespace PDPWebsite.Controllers;

[ApiController]
[Route("/api/[controller]/")]
public class ScheduleController : ControllerBase
{
    private readonly Database _database;
    private readonly DiscordConnection _discord;
    private readonly IHubContext<MainHub> _hub;

    public ScheduleController(Database database, DiscordConnection discord, IHubContext<MainHub> hub)
    {
        _database = database;
        _discord = discord;
        _hub = hub;
    }

    private DateTimeOffset GetThisWeek() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"));

    [HttpGet]
    [Route("week")]
    public async Task<IActionResult> GetWeekSchedule()
    {
        var first = GetThisWeek();
        first = first.DayOfWeek switch
        {
            DayOfWeek.Wednesday => first.AddDays(-1),
            DayOfWeek.Thursday => first.AddDays(-2),
            DayOfWeek.Friday => first.AddDays(-3),
            DayOfWeek.Saturday => first.AddDays(-4),
            DayOfWeek.Sunday => first.AddDays(-5),
            DayOfWeek.Monday => first.AddDays(-6),
            _ => first
        };
        var last = first.AddDays(7);
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Where(t => t.At >= first && t.At < last).Select(t => ScheduleHttp.FromSchedule(t, _discord, _database)));
    }

    [HttpGet]
    [Route("nextweek")]
    public async Task<IActionResult> GetNextWeekSchedule()
    {
        var first = GetThisWeek();
        first = first.DayOfWeek switch
        {
            DayOfWeek.Wednesday => first.AddDays(-1),
            DayOfWeek.Thursday => first.AddDays(-2),
            DayOfWeek.Friday => first.AddDays(-3),
            DayOfWeek.Saturday => first.AddDays(-4),
            DayOfWeek.Sunday => first.AddDays(-5),
            DayOfWeek.Monday => first.AddDays(-6),
            _ => first
        };
        first = first.AddDays(7);
        var last = first.AddDays(7);
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Where(t => t.At >= first && t.At < last).Select(t => ScheduleHttp.FromSchedule(t, _discord, _database)));
    }


    [HttpGet, ServiceFilter(typeof(AuthFilter))]
    [Route("all")]
    public async Task<IActionResult> GetSchedule()
    {
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Select(t => ScheduleHttp.FromSchedule(t, _discord, _database)));
    }

    [HttpPost, ServiceFilter(typeof(AuthFilter))]
    [Route("add")]
    public async Task<IActionResult> AddSchedule([FromBody] ScheduleHttp scheduleHttp)
    {
        var schedule = scheduleHttp.GetSchedule();
        var added = _database.Schedules.Add(schedule);
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleAdded", new { Schedule = ScheduleHttp.FromSchedule(added.Entity, _discord, _database), NextWeek = GetThisWeek().AddDays(7) >= schedule.At });
        return Ok();
    }

    [HttpPut, ServiceFilter(typeof(AuthFilter))]
    [Route("update")]
    public async Task<IActionResult> UpdateSchedule([FromBody] ScheduleHttp schedule)
    {
        await _database.Schedules.Where(t => t.Id == schedule.Id).ExecuteUpdateAsync(prop =>
            prop
                .SetProperty(k => k.Name, schedule.Name)
                .SetProperty(k => k.HostId, schedule.HostId)
                .SetProperty(k => k.Duration, schedule.Duration)
                .SetProperty(k => k.At, schedule.At)
            );
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleUpdated", new { Schedule = ScheduleHttp.FromSchedule(schedule.GetSchedule(), _discord, _database), NextWeek = GetThisWeek().AddDays(7) >= schedule.At });
        return Ok();
    }

    [HttpDelete, ServiceFilter(typeof(AuthFilter))]
    [Route("delete")]
    public async Task<IActionResult> DeleteSchedule([FromBody] Guid remove)
    {
        await _database.Schedules.Where(t => t.Id == remove).ExecuteDeleteAsync();
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleDeleted", remove);
        return Ok();
    }
}

public record ScheduleHttp(Guid? Id, string Name, ulong HostId, string? HostName, TimeSpan Duration, DateTime At)
{
    public static ScheduleHttp FromSchedule(Schedule schedule, DiscordConnection discord, Database database)
    {
        return new ScheduleHttp(schedule.Id, schedule.Name, schedule.HostId, database.AboutInfos.FirstOrDefault(t => t.Id == schedule.HostId)?.VisualName ?? discord.Guild!.GetUser(schedule.HostId).DisplayName, schedule.Duration, schedule.At);
    }
}

public static class ScheduleExtensions
{
    public static Schedule GetSchedule(this ScheduleHttp schedule)
    {
        return new Schedule(null, schedule.Name, schedule.HostId, schedule.Duration, schedule.At);
    }
}