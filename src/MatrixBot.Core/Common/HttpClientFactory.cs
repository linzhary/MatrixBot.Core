using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace MatrixBot.Core;

public static class HttpClientFactory
{
    public static readonly HttpClient Default = CreateClient();
    internal class LoggingHandler(HttpClientHandler handler) : DelegatingHandler(handler)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Log.Debug("Request: {Method} {Uri}", request.Method, request.RequestUri);
            if (request.Content?.Headers.ContentType?.MediaType != null)
            {
                if (request.Content.Headers.ContentType.MediaType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase))
                {
                    var requestJson = await request.Content.ReadAsStringAsync(cancellationToken);
                    Log.Debug("Request Content: {RequestJson}", requestJson);
                }
            }
            var response = await base.SendAsync(request, cancellationToken);

            Log.Debug("Response: {StatusCode}", response.StatusCode);
            if (response.Content?.Headers.ContentType?.MediaType != null)
            {
                if (response.Content.Headers.ContentType.MediaType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase))
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    Log.Debug("Response Content: {ResponseJson}", responseJson);
                }
            }

            return response;
        }
    }

    public static HttpClient CreateClient()
    {
        //var webProxyUrl = Global.IsRunningInContainer() ? "host.docker.internal:7890" : "http://127.0.0.1:7890";
        //var handler = new LoggingHandler(new HttpClientHandler
        //{
        //    UseProxy = true,
        //    Proxy = new WebProxy(webProxyUrl)
        //});
        return new HttpClient(new LoggingHandler(new HttpClientHandler()));
    }

    private static string? GetFileNameFromContentDisposition(HttpContentHeaders headers)
    {
        if (headers.ContentDisposition != null && !string.IsNullOrEmpty(headers.ContentDisposition.FileNameStar))
        {
            return headers.ContentDisposition.FileNameStar.Trim('"');
        }

        if (headers.ContentDisposition != null && !string.IsNullOrEmpty(headers.ContentDisposition.FileName))
        {
            return headers.ContentDisposition.FileName.Trim('"');
        }

        return null;
    }

    public static async Task<MatrixMediaInfo?> DownloadAsync(string fileUrl)
    {
        try
        {
            // 发送 GET 请求
            using var response = await Default.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // 获取文件名
            var fileName = GetFileNameFromContentDisposition(response.Content.Headers);

            // 如果未能从 Content-Disposition 头获取文件名，使用 URL 中的文件名
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            }
            var ret = new MatrixMediaInfo()
            {
                FileName = fileName,
                MediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
                FileSize = response.Content.Headers.ContentLength ?? 0,
            };
            using var httpStream = await response.Content.ReadAsStreamAsync();
            await httpStream.CopyToAsync(ret.FileStream);
            ret.FileStream.Seek(0, SeekOrigin.Begin);
            return ret;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "下载文件[{fileUrl}]失败", fileUrl);
        }
        return null;
    }
}
