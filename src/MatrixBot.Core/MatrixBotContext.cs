using MatrixBot.Core.Event;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Reflection;

namespace MatrixBot.Core;

public class MatrixBotContext
{
    public string RoomId { get; set; } = default!;
    public MatrixBotRoomEvent Event { get; set; } = default!;
    public MatrixBotClient Client { get; set; } = default!;

    [DoesNotReturn]
    public async Task ReplyAsync(Dictionary<string, object> args)
    {
        var replyContent = new Dictionary<string, object?>()
        {
            {
                "m.relates_to",
                new Dictionary<string, object?>()
                {
                    {"m.in_reply_to",new{ event_id = Event.EventId} }
                }
            },
            {
                "m.mentions",
                new Dictionary<string, object?>()
                {
                    {"m.user_ids", new { Event.Sender } }
                }
            }
        };
        foreach (var item in args)
        {
            replyContent.Add(item.Key, item.Value);
        }
        await Client.SendRawMessageAsync(RoomId, replyContent);
    }
}
