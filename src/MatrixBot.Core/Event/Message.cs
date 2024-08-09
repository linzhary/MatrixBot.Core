using System.Text.Json.Serialization;

namespace MatrixBot.Core;

public class Message : IMessage
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = default!
    [JsonPropertyName("msgtype")]
    public string MsgType { get; set; } = default!;
    [JsonPropertyName("format")]
    public string? Format { get; set; }
    [JsonPropertyName("formatted_body")]
    public string? FormattedBody { get; set; }
    [JsonPropertyName("m.relates_to")]
    public _RelatesTo? RelatesTo { get; set; }
    [JsonPropertyName("m.mentions")]
    public _Mentions? Mentions { get; set; }
    public class _RelatesTo
    {
        [JsonPropertyName("m.in_reply_to")]
        public _InReplyTo? InReplyTo { get; set; }
        public class _InReplyTo
        {
            [JsonPropertyName("event_id")]
            public string? EventId { get; set; }
        }
    }

    public class _Mentions
    {
        [JsonPropertyName("m.user_ids")]
        public _UserIds? UserIds { get; set; }
        public class _UserIds
        {
            [JsonPropertyName("sender")]
            public string? Sender { get; set; }
        }
    }
}
