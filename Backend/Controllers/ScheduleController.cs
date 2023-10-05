namespace PDPWebsite.Controllers;

[ApiController, ServiceFilter(typeof(AuthFilter))]
[Route("/api/[controller]/")]
public class ScheduleController : ControllerBase
{
    private readonly Database _database;

    public ScheduleController(Database database)
    {
        _database = database;
    }

    [HttpGet]
    [Route("all")]
    public async Task<IActionResult> GetSchedule()
    {
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules);
    }
}
