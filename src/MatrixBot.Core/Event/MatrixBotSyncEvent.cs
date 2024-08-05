using System.Text.Json.Serialization;

namespace MatrixBot.Core.Event;

internal class MatrixBotSyncEvent
{
    [JsonPropertyName("next_batch")]
    public string? NextBatch { get; set; }

    [JsonPropertyName("rooms")]
    public _Rooms? Rooms { get; set; }

    public class _Rooms
    {
        [JsonPropertyName("join")]
        public Dictionary<string, _Join>? Join { get; set; }

        public class _Join
        {
            [JsonPropertyName("timeline")]
            public _TimeLine? TimeLine { get; set; }

            public class _TimeLine
            {
                [JsonPropertyName("events")]
                public List<MatrixBotRoomEvent>? Events { get; set; }
            }
        }
    }
}

