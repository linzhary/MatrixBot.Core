using MatrixBot.Core.Event;

namespace MatrixBot.Core.Handler;

internal class ActionMatrixBotEventHandler(Func<MatrixBotContext, Task> onEventAsync) : IMatrixBotEventHandler
{
    public async Task OnEventAsync(MatrixBotContext ctx)
    {
        await onEventAsync.Invoke(ctx);
    }
}

public interface IMatrixBotEventHandler
{
    public Task OnEventAsync(MatrixBotContext ctx);
}
