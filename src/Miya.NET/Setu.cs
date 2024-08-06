using MatrixBot.Core;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miya.NET;

public class Setu : MatrixApplication
{
    private Timer timer = default!;
    private readonly ConcurrentQueue<Dictionary<string, object>> setuMessageQueue = new();
    private async Task cacheSetuAsync(MatrixBotClient client)
    {
        timer.Change(Timeout.Infinite, Timeout.Infinite);
        if (setuMessageQueue.Count >= 7) return;
        Log.Information("涩图缓存数量不足7张，开始加载涩图缓存……");
        var resJson = await HttpClientFactory.Default.GetFromJsonAsync<JsonElement>("https://api.lolicon.app/setu/v2?num=5&r18=1&size=regular");
        foreach (var item in resJson.GetProperty("data").EnumerateArray())
        {
            var url = item.GetProperty("urls").GetProperty("regular").GetString()!;
            var fileInfo = await HttpClientFactory.DownloadAsync(url);
            if (fileInfo is null)
            {
                continue;
            }
            using (fileInfo.FileStream)
            {
                //上传媒体文件
                var content_uri = await client.UploadMediaAsync(fileInfo.FileName, fileInfo.FileStream);
                setuMessageQueue.Enqueue(new()
                {
                    { "msgtype" , "m.image" },
                    { "body" , fileInfo.FileName },
                    { "url" , content_uri },
                    { "info", new Dictionary<string, object>()
                        {
                            { "size", fileInfo.FileSize },
                            { "mimetype", fileInfo.MediaType },
                        }
                    }
                });
            }
        }
        // 停止定时器
        timer.Change(0, 1000);
    }
    public override Task OnReadyAsync(MatrixBotClient client)
    {
        timer = new(_ => Task.Run(async () => await cacheSetuAsync(client)), default, 0, 1000);
        return Task.CompletedTask;
    }

    [Room.OnMessage]
    public Task LogAsync(Context<Room.Message> ctx)
    {
        Log.Information("收到消息 [{RoomId}][{SenderId}]:{RawMessage}", ctx.RoomId, ctx.Sender, ctx.Content?.Body);
        return Task.CompletedTask;
    }

    [Room.OnRegex(@"^来(\d+)?[张份][色涩]图$")]
    public async Task GetAsync(Context<Room.Message> ctx)
    {
        var numString = ctx.MatchResult!.Groups[1].Value;
        var num = string.IsNullOrWhiteSpace(numString) ? 1 : Convert.ToInt32(numString);
        if (num > 5)
        {
            await ctx.ReplyAsync(new()
                {
                    { "msgtype" , "m.text" },
                    { "body" , "一次不许看这么多哦~" },
                });
            return;
        }
        while (setuMessageQueue.Count < num)
        {
            await cacheSetuAsync(ctx.Client);
        }
        for (var i = 0; i < num; i++)
        {
            if (setuMessageQueue.TryDequeue(out var message))
            {
                await ctx.ReplyAsync(message);
            }
            else
            {
                i--;
            }
        }
    }
}
