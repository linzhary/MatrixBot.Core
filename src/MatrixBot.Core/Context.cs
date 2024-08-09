using System.Text.Json;
using System.Text.RegularExpressions;

namespace MatrixBot.Core;

public abstract class Context
{
    public string RoomId { get; set; } = default!;
    public string Sender { get; set; } = default!;
    public string EventId { get; set; } = default!;
    public Match? MatchResult { get; internal set; }
    public MatrixBotClient Client { get; internal set; } = default!;
    internal Context(MatrixBotClient client, string roomId, MatrixEvent e)
    {
        Client = client;
        RoomId = roomId;
        Sender = e.Sender;
        EventId = e.EventId;
    }


    private static readonly Dictionary<string, Func<MatrixBotClient, string, MatrixEvent, Context>> _converters = [];
    static Context()
    {
        _converters.Add(M.Room.Message, (client, roomId, evt) => new Context<Message>(client, roomId, evt));
    }
    internal static Context? TryConvert(MatrixBotClient client, string roomId, MatrixEvent evt)
    {
        var converter = _converters.GetValueOrDefault(evt.Type);
        return converter?.Invoke(client, roomId, evt);
    }
}
public class Context<T> : Context
{
    public T? Content { get; set; }
    internal Context(MatrixBotClient client, string roomId, MatrixEvent e) : base(client, roomId, e)
    {
        if (e.Content.HasValue)
        {
            Content = e.Content.Value.Deserialize<T>(Global.JsonSerializerOptions);
        }
    }
    public async Task ReplyAsync<TMessage>(TMessage message) where TMessage : Message
    {
        message.RelatesTo = new()
        {
            InReplyTo = new()
            {
                EventId = EventId
            }
        };
        message.Mentions = new()
        {
            UserIds = new()
            {
                Sender = Sender
            }
        };
        await Client.SendRawMessageAsync(RoomId, message);
    }
    public async Task SendAsync<TMessage>(TMessage message) where TMessage : IMessage
    {
        await Client.SendRawMessageAsync(RoomId, message);
    }
}
