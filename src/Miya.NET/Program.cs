using MatrixBot.Core;
using Serilog;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

using var client = new MatrixBotClient();

client.ScanApplication(typeof(Program).Assembly);

await client.RunAsync(
    serverUrl: "https://chat.pcrbot.com",
    userName: "mia",
    password: "950819Lqh#"
    );

