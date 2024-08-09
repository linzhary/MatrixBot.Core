using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MatrixBot.Core
{
    public class Room
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class OnMessage([StringSyntax(StringSyntaxAttribute.Regex)] string pattern = ".*") : Attribute, IRuleMatcher, ITypeMatcher
        {
            public string EventType => "m.room.message";

            public virtual bool IsMatch(object? args)
            {
                if (args is Context<Message> ctx && ctx is { Content.Body: not null })
                {
                    ctx.MatchResult = Regex.Match(ctx.Content.Body, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    return ctx.MatchResult.Success;
                }
                return false;
            }
        }

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
            [JsonPropertyName("membership")]
            public string? Membership { get; set; }
            [JsonPropertyName("displayname")]
            public string? DisplayName { get; set; }
            [JsonPropertyName("avatar_url")]
            public string? AvatarUrl { get; set; }

        }
        public static class MessageType
        {

        }
    }
}
