using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Matrix.Bot.Core.Common;

public class LoggingHandler() : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Log.Information("Request: {Method} {Uri}", request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
            Log.Debug("Request Content: {RequestContent}", requestContent);
        }

        var response = await base.SendAsync(request, cancellationToken);

        Log.Information("Response: {StatusCode}", response.StatusCode);

        if (response.Content != null)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Debug("Response Content: {ResponseJson}", responseContent);
        }

        return response;
    }
}
