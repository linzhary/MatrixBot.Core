using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Matrix.Bot.Core.Event;

public class MatrixBotRoomEvent
{
    [JsonPropertyName("content")]
    public MatrixBotRoomEventContent? Content { get; set; }
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }
    [JsonPropertyName("sender")]
    public string? Sender { get; set; }
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

