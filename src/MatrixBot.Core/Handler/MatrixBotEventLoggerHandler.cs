using MatrixBot.Core.Event;
using MatrixBot.Core.Handler;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MatrixBot.Core.Common;

internal class MatrixBotEventLoggerHandler : IMatrixBotEventHandler
{
    public Task OnEventAsync(MatrixBotContext ctx)
    {
        Log.Information("收到消息 [{RoomId}][{SenderId}]:{RawMessage}", ctx.RoomId, ctx.Event.Sender, ctx.Event.Content?.Body);
        return Task.CompletedTask;
    }
}