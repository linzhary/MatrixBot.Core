using MatrixBot.Core;


using var client = new MatrixBotClient();
client.Services.RegisterAssembly(typeof(Program).Assembly);
await client.RunAsync(
    serverUrl: Environment.GetEnvironmentVariable("SERVERURL")!,
    userName: Environment.GetEnvironmentVariable("USERNAME")!,
    password: Environment.GetEnvironmentVariable("PASSWORD")!
    );

