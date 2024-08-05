using Serilog;
using System.Text.Json;

namespace MatrixBot.Core;

public class MatrixBotCache
{
    public string? ServerUrl { get; set; }
    public string? DeviceId { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? UserId { get; set; }
    public string? AccessToken { get; set; }
    public string? Since { get; set; }

    private const string FILE_NAME = "client.cache";
    /// <summary>
    /// 从磁盘加载配置文件
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <param name="userName"></param>
    internal async Task<bool> TryLoadFromDiskAsync(string serverUrl, string userName, string password)
    {
        ServerUrl = serverUrl;
        UserName = userName;
        Password = password;
        if (!File.Exists(FILE_NAME)) return false;
        try
        {
            var configJson = await File.ReadAllTextAsync(FILE_NAME);
            var obj = JsonSerializer.Deserialize<MatrixBotCache>(configJson, Global.JsonSerializerOptions);
            if (obj is null) return false;
            if (obj.ServerUrl is not null && obj.ServerUrl != serverUrl) return false;
            if (obj.UserName is not null && obj.UserName != userName) return false;

            UserId = obj.UserId;
            DeviceId = obj.DeviceId;
            AccessToken = obj.AccessToken;
            Since = obj.Since;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "反序列化配置文件失败");
            return false;
        }
        return true;
    }

    internal async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(this, Global.JsonSerializerOptions);
        await File.WriteAllTextAsync(FILE_NAME, json);
    }
}
