using MatrixBot.Core;
using System.Net.Http.Json;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Xml.Linq;

var client = new MatrixBotClient();

client.Register(async (ctx) =>
{
    if (ctx.Event.Content?.Body is "来份涩图" or "来份色图")
    {
        using var httpClient = new HttpClient();
        var resJson = await httpClient.GetFromJsonAsync<JsonElement>("https://api.lolicon.app/setu/v2?r18=1&size=regular");
        foreach (var item in resJson.GetProperty("data").EnumerateArray())
        {
            var url = item.GetProperty("urls").GetProperty("regular").GetString()!;
            var name = Path.GetFileName(url)!;
            var content_uri = await client.UploadMediaFileAsync(name, url);

            await ctx.ReplyAsync(new()
            {
                { "msgtype" , "m.image" },
                { "body" , name },
                { "url" , content_uri },
                { "info", 
                    new Dictionary<string, string>() 
                    {
                        { "thumbnail_url",content_uri }
                    } 
                }
            });
            break;
        }
    }
    else if (ctx.Event.Content?.Body?.Contains("测试") is true)
    {
        await ctx.ReplyAsync(new()
            {
                { "msgtype" , "m.text" },
                { "body" , "测试成功" },
            });
    }
});

await client.RunAsync(
    serverUrl: "https://chat.pcrbot.com",
    userName: "mia",
    password: "950819Lqh#"
    );

