namespace MatrixBot.Core;

public abstract class MatrixService
{
    /// <summary>
    /// BOT初始化时会调用该方法
    /// </summary>
    /// <returns></returns>
    public virtual Task OnReadyAsync(MatrixBotClient client)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// BOT持久化时会调用该方法
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public virtual Task OnSaveAsync(MatrixBotClient client)
    {
        return Task.CompletedTask;
    }
}
