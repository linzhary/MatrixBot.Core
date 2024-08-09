using MatrixBot.Core;


MatrixServiceProvider.Instance.AddServices(typeof(Program).Assembly);

using var client = new MatrixBotClient();
await client.RunAsync(
    serverUrl: Environment.GetEnvironmentVariable("SERVERURL")!,
    userName: Environment.GetEnvironmentVariable("USERNAME")!,
    password: Environment.GetEnvironmentVariable("PASSWORD")!
    );

