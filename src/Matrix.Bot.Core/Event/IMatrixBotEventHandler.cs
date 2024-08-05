using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Bot.Core.Event;

public class MatrixBotEventArgs
{
    public string RoomId { get; set; } = default!;
    public MatrixBotRoomEvent Event { get; set; } = default!;
}


public interface IMatrixBotEventHandler
{
    public Task OnEventAsync(MatrixBot bot, MatrixBotEventArgs args);
}
