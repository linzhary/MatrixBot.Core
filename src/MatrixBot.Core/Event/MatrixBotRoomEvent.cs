using System.Text.Json.Serialization;

namespace MatrixBot.Core.Event;

public class MatrixBotRoomEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;
    [JsonPropertyName("sender")]
    public string Sender { get; set; } = default!;
    [JsonPropertyName("content")]
    public MatrixBotRoomEventContent? Content { get; set; }
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = default!;
}

public class MatrixBotRoomEventContent
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    [JsonPropertyName("msgtype")]
    public string? MsgType { get; set; }
    [JsonPropertyName("format")]
    public string? Format { get; set; }
    [JsonPropertyName("formatted_body")]
    public string? FormattedBody { get; set; }
    [JsonPropertyName("membership")]
    public string? Membership { get; set; }
    [JsonPropertyName("displayname")]
    public string? DisplayName { get; set; }
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

}

