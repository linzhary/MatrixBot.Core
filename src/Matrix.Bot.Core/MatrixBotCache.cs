using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Matrix.Bot.Core;

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
    private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
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
            var obj = JsonSerializer.Deserialize<MatrixBotCache>(configJson, _serializerOptions);
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
        var json = JsonSerializer.Serialize(this);
        await File.WriteAllTextAsync(FILE_NAME, json);
    }
}
