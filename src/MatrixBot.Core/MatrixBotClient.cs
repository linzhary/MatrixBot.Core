using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace MatrixBot.Core;

public class MatrixBotClient : IDisposable
{
    internal readonly HttpClient _httpClient;
    internal readonly Storage _storage;
    private readonly MatrixAppContainer _container = new();
    private readonly int _syncTimeout;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="syncTimeout">同步超时时间</param>
    /// <param name="httpTimeout">请求超时时间</param>
    public MatrixBotClient(int syncTimeout = 30000, int httpTimeout = 60000)
    {
        _syncTimeout = syncTimeout;
        // 配置 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code) // 选择一个主题
            .CreateLogger();

        _httpClient = HttpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMilliseconds(httpTimeout);
        _storage = new Storage();
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <returns></returns>
    private async Task ConnectAsync()
    {
        _httpClient.BaseAddress = new Uri($"{_storage.ServerUrl!.Trim('/')}/_matrix/");
        if (string.IsNullOrEmpty(_storage.AccessToken))
        {
            var res = await _httpClient.PostAsJsonAsync("login", new
            {
                device_id = _storage.DeviceId,
                identifier = new
                {
                    type = "m.id.user",
                    user = _storage.UserName
                },
                _storage.Password,
                type = "m.login.password",
            });
            res.EnsureSuccessStatusCode();
            var resJson = await res.Content.ReadFromJsonAsync<JsonElement>();
            _storage.AccessToken = resJson.GetProperty("access_token").GetString();
            _storage.DeviceId = resJson.GetProperty("device_id").GetString();
            _storage.UserId = resJson.GetProperty("user_id").GetString();
            var _ = _storage.SaveAsync();
        }
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _storage.AccessToken);

    }

    /// <summary>
    /// 启动BOT
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    public async Task RunAsync(string serverUrl, string userName, string password)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await _storage.TryLoadFromDiskAsync(serverUrl, userName, password);
            await ConnectAsync();
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                await cancellationTokenSource.CancelAsync();
                await _storage.SaveAsync();
            };
            Log.Information("Started MatrixBot.");

            await Task.Run(() => _container.InitAsync(this, cancellationTokenSource.Token));

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await SyncAsync(cancellationTokenSource.Token);
                }
                catch (HttpRequestException ex)
                {
                    Log.Error(ex, "Error Occured");
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Occured");
        }
        Log.Information("Stopped MatrixBot.");
    }

    /// <summary>
    /// 同步数据
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        var queryMap = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(_storage.Since))
        {
            queryMap.Add("since", _storage.Since);
        }
        queryMap.Add("timeout", _syncTimeout.ToString());

        var queryString = string.Join('&', queryMap.Select(x => $"{x.Key}={x.Value}"));
        var responseMessage = await _httpClient.GetAsync($"client/v3/sync?_=1&{queryString}", cancellationToken);
        responseMessage.EnsureSuccessStatusCode();

        var response = await responseMessage.Content.ReadFromJsonAsync<MatrixSyncResponse>(Global.JsonSerializerOptions, cancellationToken) ?? throw new HttpRequestException();
        if (_storage.Since != response.NextBatch)
        {
            _storage.Since = response.NextBatch;
            var _ = _storage.SaveAsync();
        }
        if (response.Rooms is null) return;
        if (response.Rooms.Join is null) return;
        foreach (var room in response.Rooms.Join)
        {
            if (room.Value is null) continue;
            if (room.Value.TimeLine is null) continue;
            if (room.Value.TimeLine.Events is null) continue;
            foreach (var e in room.Value.TimeLine.Events)
            {
               if(! _container.TryGetService(e.Type, out var appServices)) continue;
                var ctx = Context.TryConvert(this, room.Key, e);
                var appServiceEnumerator = appServices.GetEnumerator();
                while (!cancellationToken.IsCancellationRequested && appServiceEnumerator.MoveNext())
                {
                    if (appServiceEnumerator.Current!.RuleMatchers.Any(x => x.IsMatch(ctx)))
                    {
                        var ret = appServiceEnumerator.Current.Delegate.DynamicInvoke(ctx);
                        if (ret is Task retTask)
                        {
                            await retTask.ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 扫描应用
    /// </summary>
    /// <param name="assembly"></param>
    public void ScanApplication(Assembly assembly)
    {
        var applicationTypes = assembly.GetExportedTypes()
            .Where(x => x.IsAssignableTo(typeof(MatrixApplication)))
            .ToList();

        foreach (var applicationType in applicationTypes)
        {
            _container.Register(applicationType);
        }
    }

    /// <summary>
    /// 发送原始消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="roomId"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public async Task<string> SendRawMessageAsync<T>(string roomId, T content)
    {
        var res = await _httpClient.PostAsJsonAsync(
            $"client/v3/rooms/{Uri.EscapeDataString(roomId)}/send/m.room.message",
            content,
            Global.JsonSerializerOptions
            );

        res.EnsureSuccessStatusCode();

        var resJson = await res.Content.ReadFromJsonAsync<JsonElement>();

        return resJson.GetProperty("event_id").GetString()!;
    }

    /// <summary>
    /// 上传媒体文件
    /// </summary>
    /// <param name="name"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task<string> UploadMediaAsync(string fileName, Stream fileStream)
    {
        string content_uri;
        {
            var res = await _httpClient.PostAsync($"media/v1/create", default);
            res.EnsureSuccessStatusCode();
            var resJson = await res.Content.ReadFromJsonAsync<JsonElement>(Global.JsonSerializerOptions);
            content_uri = resJson.GetProperty("content_uri").GetString()?.Replace("mxc://", string.Empty)!;
        }
        {
            var res = await _httpClient.PutAsync($"media/v3/upload/{content_uri}?fileName={fileName}", new StreamContent(fileStream));
            res.EnsureSuccessStatusCode();
        }
        return $"mxc://{content_uri}";
    }

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                _httpClient.Dispose();
            }

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            disposedValue = true;
        }
    }

    // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
    // ~MatrixBotClient()
    // {
    //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
