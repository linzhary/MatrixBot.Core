using System.Text.Json.Serialization;
using System.Text.Json;

namespace MatrixBot.Core;

public class M
{
    public class Room
    {
        public const string Message = "m.room.message";
    }
}

public class MatrixEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;
    [JsonPropertyName("sender")]
    public string Sender { get; set; } = default!;
    [JsonPropertyName("content")]
    public JsonElement? Content { get; set; }
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = default!;
}
