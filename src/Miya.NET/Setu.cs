using MatrixBot.Core;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;

namespace Miya.NET;

/// <summary>
/// 涩图
/// </summary>
public class Setu : MatrixService
{
    [FromService]
    public MatrixBotClient Client { get; set; } = default!;

    [Room.OnMessage]
    public Task LogAsync(Context<Message> ctx)
    {
        Log.Information("收到消息 [{RoomId}][{SenderId}]:{RawMessage}", ctx.RoomId, ctx.Sender, ctx.Content?.Body);
        return Task.CompletedTask;
    }

    [Room.OnMessage(@"^来\s*?(\d+)?\s*?[张份](\w+)?[色涩]图$")]
    public async Task GetByKeywordAsync(Context<Message> ctx)
    {
        var numString = ctx.MatchResult!.Groups[1].Value;
        var num = string.IsNullOrWhiteSpace(numString) ? 1 : Convert.ToInt32(numString);
        if (num > 5)
        {
            await ctx.ReplyAsync(new TextMessage("一次不许看这么多哦❤️杂鱼~"));
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
                    await ctx.ReplyAsync(new TextMessage("一张也没有哦❤️杂鱼~"));
                    return;
                }
                foreach (var item in data)
                {
                    var url = item.GetProperty("urls").GetProperty("small").GetString()!;
                    var width = item.GetProperty("width").GetDecimal()!;
                    var height = item.GetProperty("height").GetDecimal()!;
                    var media = await HttpClientFactory.DownloadAsync(url);
                    if (media is null)
                    {
                        continue;
                    }
                    using (media.FileStream)
                    {
                        //上传媒体文件
                        media.MatrixUrl = await ctx.Client.UploadMediaAsync(media.FileName, media.FileStream);
                        await ctx.SendAsync(new ImageMessage(media, width, height));
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
