using System.Text.Json;
using System.Text.RegularExpressions;

namespace MatrixBot.Core;

public static class Context
{
    private static readonly Dictionary<string, Func<MatrixBotClient, string, MatrixEvent, object>> _converters = [];
    static Context()
    {
        _converters.Add(M.Room.Message, (client, roomId, evt) => new Context<Room.Message>(client, roomId, evt));
    }
    internal static object? TryConvert(MatrixBotClient client, string roomId, MatrixEvent evt)
    {
        var converter = _converters.GetValueOrDefault(evt.Type);
        return converter?.Invoke(client, roomId, evt);
    }
}
public class Context<T>
{
    public string RoomId { get; set; } = default!;
    public string EventId { get; set; } = default!;
    public string Sender { get; set; } = default!;
    public T? Content { get; set; } 
    public MatrixBotClient Client { get; set; } = default!;
    public Match? MatchResult { get; internal set; }
    internal Context(MatrixBotClient client, string roomId, MatrixEvent e)
    {
        Client = client;
        RoomId = roomId;
        EventId = e.EventId;
        Sender = e.Sender;
        if (e.Content.HasValue)
        {
            Content = e.Content.Value.Deserialize<T>(Global.JsonSerializerOptions);
        }
    }
    

    public async Task ReplyAsync(Dictionary<string, object> args)
    {
        var replyContent = new Dictionary<string, object?>()
        {
            {
                "m.relates_to",
                new Dictionary<string, object?>()
                {
                    {"m.in_reply_to",new{ event_id = EventId} }
                }
            },
            {
                "m.mentions",
                new Dictionary<string, object?>()
                {
                    {"m.user_ids", new { Sender } }
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
