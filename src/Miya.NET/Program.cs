using MatrixBot.Core;


MatrixServiceProvider.Instance.AddServices(typeof(Program).Assembly);

using var client = new MatrixBotClient();
await client.RunAsync(
    serverUrl: "https://chat.pcrbot.com",
    userName: "mia",
    password: "950819Lqh#"
    );

