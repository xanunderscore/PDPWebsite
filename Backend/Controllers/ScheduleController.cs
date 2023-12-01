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

    private static DateTimeOffset GetThisWeek()
    {
        return TimeZoneInfo.ConvertTime((DateTimeOffset)DateTimeOffset.UtcNow.UtcDateTime,
            TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"));
    }

    private static Tuple<DateTimeOffset, DateTimeOffset> GetWeek(bool nextWeek = false)
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
        first = new DateTimeOffset(first.Year, first.Month, first.Day, 0, 0, 0, first.Offset);
        return nextWeek
            ? Tuple.Create(first.AddDays(7), first.AddDays(14))
            : new Tuple<DateTimeOffset, DateTimeOffset>(first, first.AddDays(7));
    }

    [HttpGet]
    [Route("week")]
    public async Task<IActionResult> GetWeekSchedule()
    {
        var (first, last) = GetWeek();
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Where(t => t.At >= first && t.At < last)
            .Select(t => ScheduleHttp.FromSchedule(t, _discord, _database)));
    }

    [HttpGet]
    [Route("nextweek")]
    public async Task<IActionResult> GetNextWeekSchedule()
    {
        var (first, last) = GetWeek(true);
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Where(t => t.At >= first && t.At < last)
            .Select(t => ScheduleHttp.FromSchedule(t, _discord, _database)));
    }


    [HttpGet]
    [ServiceFilter(typeof(AuthFilter))]
    [Route("all")]
    public async Task<IActionResult> GetSchedule()
    {
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Select(t => ScheduleHttp.FromSchedule(t, _discord, _database)));
    }

    [HttpPost]
    [ServiceFilter(typeof(AuthFilter))]
    [Route("add")]
    public async Task<IActionResult> AddSchedule([FromBody] ScheduleHttp scheduleHttp)
    {
        var schedule = scheduleHttp.GetSchedule();
        var added = _database.Schedules.Add(schedule);
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleAdded",
            new
            {
                Schedule = ScheduleHttp.FromSchedule(added.Entity, _discord, _database),
                NextWeek = GetWeek(true).Item1 <= schedule.At
            });
        return Ok();
    }

    [HttpPut]
    [ServiceFilter(typeof(AuthFilter))]
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
        var scheduleReturn = await _database.Schedules.FirstAsync(t => t.Id == schedule.Id);
        await _hub.Clients.All.SendAsync("ScheduleUpdated",
            new
            {
                Schedule = ScheduleHttp.FromSchedule(scheduleReturn, _discord, _database),
                NextWeek = GetWeek(true).Item1 <= schedule.At
            });
        return Ok();
    }

    [HttpDelete]
    [ServiceFilter(typeof(AuthFilter))]
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
        return new ScheduleHttp(schedule.Id, schedule.Name, schedule.HostId,
            database.AboutInfos.FirstOrDefault(t => t.Id == schedule.HostId)?.VisualName ??
            discord.Guild!.GetUser(schedule.HostId).DisplayName, schedule.Duration, schedule.At);
    }
}

public static class ScheduleExtensions
{
    public static Schedule GetSchedule(this ScheduleHttp schedule)
    {
        return new Schedule(null, schedule.Name, schedule.HostId, schedule.Duration, schedule.At);
    }
}
