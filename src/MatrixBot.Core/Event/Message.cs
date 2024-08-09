using System.Text.Json.Serialization;

namespace MatrixBot.Core;

public class Message : IMessage
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    [JsonPropertyName("msgtype")]
    public string? MsgType { get; set; }
    [JsonPropertyName("format")]
    public string? Format { get; set; }
    [JsonPropertyName("formatted_body")]
    public string? FormattedBody { get; set; }
}
