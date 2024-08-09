using MatrixBot.Core;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miya.NET;

public class Setu : MatrixService
{
    public override Task OnReadyAsync(MatrixBotClient client)
    {
        //timer = new(_ => Task.Run(async () => await cacheSetuAsync(client)), default, 0, 1000);
        return Task.CompletedTask;
    }

    [Room.OnMessage]
    public Task LogAsync(Context<Room.Message> ctx)
    {
        Log.Information("收到消息 [{RoomId}][{SenderId}]:{RawMessage}", ctx.RoomId, ctx.Sender, ctx.Content?.Body);
        return Task.CompletedTask;
    }

    [Room.OnMessage(@"^来\s*?(\d+)?\s*?[张份](\w+)?[色涩]图$")]
    public async Task GetByKeywordAsync(Context<Room.Message> ctx)
    {
        var numString = ctx.MatchResult!.Groups[1].Value;
        var num = string.IsNullOrWhiteSpace(numString) ? 1 : Convert.ToInt32(numString);
        if (num > 5)
        {
            await ctx.ReplyAsync(new()
                {
                    { "msgtype" , "m.text" },
                    { "body" , "一次不许看这么多哦❤️杂鱼~" },
                });
            return;
        }
        var keyword = ctx.MatchResult!.Groups[2].Value;
        try
        {
            while (num > 0)
            {
                var resJson = await HttpClientFactory.Default.GetFromJsonAsync<JsonElement>($"https://api.lolicon.app/setu/v2?num={num}&r18=1&size=small&keyword={keyword}");
                var data = resJson.GetProperty("data").EnumerateArray();
                if (!data.Any())
                {
                    await ctx.ReplyAsync(new()
                    {
                        { "msgtype" , "m.text" },
                        { "body" , "一张也没有哦❤️杂鱼~" },
                    });
                    return;
                }
                foreach (var item in data)
                {
                    var url = item.GetProperty("urls").GetProperty("small").GetString()!;
                    var width = item.GetProperty("width").GetDecimal()!;
                    var height = item.GetProperty("height").GetDecimal()!;
                    var mediaInfo = await HttpClientFactory.DownloadAsync(url);
                    if (mediaInfo is null)
                    {
                        continue;
                    }
                    using (mediaInfo.FileStream)
                    {
                        //上传媒体文件
                        mediaInfo.MatrixUrl = await ctx.Client.UploadMediaAsync(mediaInfo.FileName, mediaInfo.FileStream);
                        await ctx.SendAsync(new Dictionary<string, object>()
                        {
                            { "msgtype" , "m.image" },
                            { "body" , mediaInfo.FileName },
                            { "url" , mediaInfo.MatrixUrl },
                            { "info", new Dictionary<string, object?>()
                                {
                                    { "size", mediaInfo.FileSize },
                                    { "mimetype", mediaInfo.MediaType },
                                    { "w",width },
                                    { "h",height },
                                    { "thumbnail_url", mediaInfo.MatrixUrl }
                                }
                            }
                        });
                        num--;
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(1000));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "预加载涩图出错");
        }
    }
}
