using System.Text.Json;
using System.Text.Json.Serialization;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

namespace PDPWebsite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly GameClient _client;

    public StatusController(GameClient client)
    {
        _client = client;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var statuses = _client.GetSheet<Status>()?.Where(t => !string.IsNullOrWhiteSpace(t.Name) && t.Icon != 0)
            .Select(t => (StatusHttp)t).ToList();
        if (statuses == null)
            return NotFound();
        return Ok(statuses);
    }
}

[JsonConverter(typeof(StatusHttpConverter))]
public record StatusHttp(SeString Name, SeString Description, bool CanDispel, uint Icon)
{
    public static implicit operator StatusHttp(Status status)
    {
        return new StatusHttp(status.Name, status.Description, status.CanDispel, status.Icon);
    }
}

public class StatusHttpConverter : JsonConverter<StatusHttp>
{
    public override StatusHttp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, StatusHttp value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("description",
            value.Description); // currently will return bad unicode data due to not using the new SeString parser that will be added after 3.15.2
        writer.WriteBoolean("canDispel", value.CanDispel);
        writer.WriteString("icon", value.Icon.ToString().PadLeft(6, '0'));
        writer.WriteString("iconGroup", (value.Icon / 1000 * 1000).ToString().PadLeft(6, '0'));
        writer.WriteEndObject();
    }
}
