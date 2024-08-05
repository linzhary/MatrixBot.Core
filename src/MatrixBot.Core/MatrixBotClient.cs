using MatrixBot.Core.Common;
using MatrixBot.Core.Event;
using MatrixBot.Core.Handler;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MatrixBot.Core;

public class LoggingHandler() : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Log.Debug("Request: {Method} {Uri}", request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
            Log.Debug("Request Content: {RequestContent}", requestContent);
        }

        var response = await base.SendAsync(request, cancellationToken);

        Log.Debug("Response: {StatusCode}", response.StatusCode);

        if (response.Content != null)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Debug("Response Content: {ResponseJson}", responseContent);
        }

        return response;
    }
}

public class MatrixBotClient
{
    internal readonly HttpClient _httpClient;
    internal readonly MatrixBotCache _botConfig;
    private readonly int _syncTimeout;
    private readonly List<IMatrixBotEventHandler> _eventHandlers = [
        new MatrixBotEventLoggerHandler()
        ];
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

        _httpClient = new HttpClient(new LoggingHandler())
        {
            Timeout = TimeSpan.FromMilliseconds(httpTimeout)
        };
        _botConfig = new MatrixBotCache();
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <returns></returns>
    private async Task ConnectAsync()
    {
        _httpClient.BaseAddress = new Uri($"{_botConfig.ServerUrl!.Trim('/')}/_matrix/");
        if (string.IsNullOrEmpty(_botConfig.AccessToken))
        {
            var res = await _httpClient.PostAsJsonAsync("login", new
            {
                device_id = _botConfig.DeviceId,
                identifier = new
                {
                    type = "m.id.user",
                    user = _botConfig.UserName
                },
                _botConfig.Password,
                type = "m.login.password",
            });
            res.EnsureSuccessStatusCode();
            var resJson = await res.Content.ReadFromJsonAsync<JsonElement>();
            _botConfig.AccessToken = resJson.GetProperty("access_token").GetString();
            _botConfig.DeviceId = resJson.GetProperty("device_id").GetString();
            _botConfig.UserId = resJson.GetProperty("user_id").GetString();
            var _ = _botConfig.SaveAsync();
        }
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _botConfig.AccessToken);
    }

    /// <summary>
    /// 注册一个处理插件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public void Register<T>() where T : IMatrixBotEventHandler
    {
        _eventHandlers.Add(Activator.CreateInstance<T>());
    }

    /// <summary>
    /// 注册一个处理器
    /// </summary>
    /// <param name="onEventAsync"></param>
    public void Register(Func<MatrixBotContext, Task> onEventAsync)
    {
        _eventHandlers.Add(new ActionMatrixBotEventHandler(onEventAsync));
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
            await _botConfig.TryLoadFromDiskAsync(serverUrl, userName, password);
            await ConnectAsync();
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                await cancellationTokenSource.CancelAsync();
                await _botConfig.SaveAsync();
            };
            Log.Information("Started MatrixBot.");
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
        if (!string.IsNullOrEmpty(_botConfig.Since))
        {
            queryMap.Add("since", _botConfig.Since);
        }
        queryMap.Add("timeout", _syncTimeout.ToString());

        var queryString = string.Join('&', queryMap.Select(x => $"{x.Key}={x.Value}"));
        var responseMessage = await _httpClient.GetAsync($"client/v3/sync?_=1&{queryString}", cancellationToken);
        responseMessage.EnsureSuccessStatusCode();

        var response = await responseMessage.Content.ReadFromJsonAsync<MatrixBotSyncEvent>(Global.JsonSerializerOptions, cancellationToken) ?? throw new HttpRequestException();
        if (_botConfig.Since != response.NextBatch)
        {
            _botConfig.Since = response.NextBatch;
            var _ = _botConfig.SaveAsync();
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
                if (e.Sender != _botConfig.UserId)
                {
                    var ctx = new MatrixBotContext
                    {
                        RoomId = room.Key,
                        Event = e,
                        Client = this
                    };
                    await Task.Run(async () =>
                    {
                        foreach (var eventHandler in _eventHandlers)
                        {
                            await eventHandler.OnEventAsync(ctx);
                        }
                    }, cancellationToken);
                }
            }
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
    public async Task<string> UploadMediaFileAsync(string name, string url)
    {
        string content_uri;
        {
            var res = await _httpClient.PostAsync($"media/v1/create", default);
            res.EnsureSuccessStatusCode();
            var resJson = await res.Content.ReadFromJsonAsync<JsonElement>(Global.JsonSerializerOptions);
            content_uri = resJson.GetProperty("content_uri").GetString()?.Replace("mxc://", string.Empty)!;
        }
        {
            using var httpClient = new HttpClient();
            using var httpStream = await httpClient.GetStreamAsync(url);
            var res = await _httpClient.PutAsync($"media/v3/upload/{content_uri}?fileName={name}", new StreamContent(httpStream));
            res.EnsureSuccessStatusCode();
        }
        return $"mxc://{content_uri}";
    }
}
