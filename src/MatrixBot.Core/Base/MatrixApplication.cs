namespace MatrixBot.Core;

public abstract class MatrixApplication
{
    /// <summary>
    /// BOT初始化成功时会调用该方法
    /// </summary>
    /// <returns></returns>
    public virtual Task OnReadyAsync(MatrixBotClient client)
    {
        return Task.CompletedTask;
    }
}
