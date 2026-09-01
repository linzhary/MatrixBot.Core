# MatrixBot.Core

基于 [Matrix](https://matrix.org/) 协议（Client-Server API）的 `.NET` 机器人框架。它封装了 Matrix 服务器的登录、长轮询同步、消息收发、媒体上传/下载等能力，并在此基础上提供一套基于 **特性（Attribute）+ 反射 + 表达式树** 的轻量服务/事件路由机制，让你可以用简洁的声明式方式编写 Bot 逻辑。

## 项目结构

```
MatrixBot.Core
├── src/
│   ├── MatrixBot.Core/              # 框架核心类库
│   │   ├── Attributes/              # 特性与标记接口
│   │   │   ├── ManagedServiceAttribute.cs   # 标记需自动注册的服务类
│   │   │   ├── FromServiceAttribute.cs     # 属性注入（从 DI 容器解析）
│   │   │   ├── IMatrixAction.cs            # 动作标记接口
│   │   │   ├── ITypeMatcher.cs             # 事件类型匹配（EventType）
│   │   │   └── IRuleMatcher.cs             # 规则匹配（IsMatch）
│   │   ├── Base/                    # 框架基础类型
│   │   │   ├── IMessage.cs                 # 消息接口
│   │   │   ├── MatrixEvent.cs              # Matrix 事件模型 + M 常量
│   │   │   ├── MatrixService.cs            # 服务基类（生命周期钩子）
│   │   │   ├── MatrixServiceCollection.cs  # 服务注册集合
│   │   │   ├── MatrixServiceProvider.cs    # 服务容器/终结点扫描
│   │   │   ├── MatrixEndpoint.cs           # 内部终结点封装
│   │   │   └── MatrixSyncResponse.cs       # /sync 响应模型
│   │   ├── Common/                  # 通用工具
│   │   │   ├── HttpClientFactory.cs        # 带日志的 HttpClient + 媒体下载
│   │   │   └── MatrixMedia.cs              # 媒体模型
│   │   ├── Event/                   # 消息类型
│   │   │   ├── Message.cs                  # 消息基类（body/mention/relate_to）
│   │   │   ├── TextMessage.cs              # 文本消息
│   │   │   └── ImageMessage.cs             # 图片消息
│   │   ├── Room/
│   │   │   └── Room.cs                     # 房间 + OnMessage 特性
│   │   ├── Context.cs                      # 消息上下文（Reply/Send）
│   │   ├── Global.cs                       # 全局常量/工具
│   │   ├── MatrixBotClient.cs              # 客户端主体（连接/同步/发送）
│   │   ├── Storage.cs                      # 本地缓存（token / since）
│   │   └── MatrixBot.Core.csproj
│   └── Miya.NET/                    # 可执行示例应用
│       ├── Program.cs                     # 入口
│       ├── Setu.cs                        # 示例服务（涩图机器人）
│       ├── Dockerfile
│       └── Miya.NET.csproj
├── Miya.NET.sln
└── README.md
```

## 技术栈

- **.NET 11**（`net11.0`）
- **Microsoft.Extensions.DependencyInjection** —— 服务容器
- **Serilog** —— 控制台 + 文件日志（按天轮转，保留 7 天，单文件 10MB）
- **dotenv.net** —— 读取 `.env` 环境变量
- **System.Text.Json** —— JSON 序列化（属性名大小写不敏感）
- 表达式树（`System.Linq.Expressions`）—— 编译终结点分发

## 核心机制

### 1. 启动流程（`MatrixBotClient.RunAsync`）

1. `DotEnv.Load()` 加载 `.env`
2. 从 `client.cache` 尝试恢复会话（`Storage.TryLoadFromDiskAsync`）
3. 若缺少 `access_token`，通过密码登录（`m.login.password`）获取并持久化
4. 初始化所有 `MatrixService`（调用 `OnReadyAsync`）
5. 进入 `/sync` 长轮询循环（默认超时 30s），将时间线事件按类型+规则分发到对应终结点

### 2. 服务与事件路由

框架的核心是「特性驱动的服务 + 终结点」。一个服务即一个继承自 `MatrixService` 的类，方法上标注动作特性即可成为事件终结点。

| 特性/接口 | 作用 |
| --- | --- |
| `[ManagedService]` | 标记自动注册到容器的服务类（配合 `RegisterAssembly`） |
| `[Room.OnMessage("正则")]` | 监听 `m.room.message` 事件并用正则匹配消息正文 |
| `[FromService]` | 属性注入，从 DI 容器解析依赖 |
| `ITypeMatcher` | 指定监听的事件类型（`EventType`） |
| `IRuleMatcher` | 额外规则过滤（`IsMatch`） |

终结点方法签名要求：第一个参数必须是 `Context` 或 `Context<T>` 类型。框架会在启动时扫描所有带 `IMatrixAction` 特性的方法，用表达式树编译为 `Func<Context?, Task>`，按 `EventType` 分组存到内部字典。事件到达时先按类型路由，再依次匹配 `IRuleMatcher`，命中即调用。

### 3. 消息上下文（`Context<T>`）

`Context<T>` 封装了当前事件的 `RoomId`、`Sender`、`EventId` 及反序列化后的 `Content`，并直接提供两个发送方法：

- `ReplyAsync(message)` —— 回复原消息（自动带上 `m.relates_to` 与 `m.mentions` 引用发送者）
- `SendAsync(message)` —— 向当前房间发送新消息

### 4. 消息与媒体

- `TextMessage` / `ImageMessage` 继承自 `Message`
- `MatrixBotClient.UploadMediaAsync` —— 上传媒体到 Matrix，返回 `mxc://` URI
- `HttpClientFactory.DownloadAsync` —— 从 URL 下载媒体并封装为 `MatrixMedia`

## 快速开始

### 1. 配置 `.env`

在 `src/Miya.NET/` 目录下创建 `.env` 文件（该文件已被 git 忽略，需自行创建）：

```env
SERVERURL=https://your-matrix-server.example.com
USERNAME=your_bot_username
PASSWORD=your_bot_password
```

### 2. 运行示例

```bash
dotnet run --project src/Miya.NET/Miya.NET.csproj
```

`Miya.NET` 内置了一个「涩图」机器人示例（`Setu.cs`），它在房间里监听形如 `来N张XX色图` 的消息，调用 [Lolicon API](https://api.lolicon.app/) 获取图片并发送到房间。

### 3. 编写自己的服务

```csharp
using MatrixBot.Core;

public class EchoService : MatrixService
{
    [FromService]
    public MatrixBotClient Client { get; set; } = default!;

    // 监听 m.room.message，匹配消息正文为 "ping"
    [Room.OnMessage("^ping$")]
    public Task OnPingAsync(Context<Message> ctx)
    {
        return ctx.ReplyAsync(new TextMessage("pong 🏓"));
    }
}
```

然后在 `Program.cs` 中注册程序集即可：

```csharp
using var client = new MatrixBotClient();
client.Services.RegisterAssembly(typeof(Program).Assembly);
await client.RunAsync(
    serverUrl: Environment.GetEnvironmentVariable("SERVERURL")!,
    userName: Environment.GetEnvironmentVariable("USERNAME")!,
    password: Environment.GetEnvironmentVariable("PASSWORD")!
);
```

## 环境变量

| 变量 | 说明 |
| --- | --- |
| `SERVERURL` | Matrix homeserver 地址（如 `https://matrix.example.com`） |
| `USERNAME` | 机器人账号用户名 |
| `PASSWORD` | 机器人账号密码 |

登录成功后，`access_token`、`device_id`、`user_id` 以及 sync 的 `since` 游标会被持久化到工作目录下的 `client.cache`，后续启动无需重复登录。

## 日志

使用 Serilog 输出到两处：

- **控制台**：`[HH:mm:ss INF] 消息`
- **文件**：`logs/log-YYYYMMDD.txt`，按天轮转，超过 10MB 或保留 7 天自动清理

## Docker

`src/Miya.NET/Dockerfile` 提供了容器化构建模板（基于 official dotnet 镜像）。注意：项目当前目标框架为 `net11.0`，若使用该 Dockerfile 构建需确保基础镜像的 SDK/Runtime 版本与之匹配；当前模板中引用的是 `mcr.microsoft.com/dotnet/sdk:8.0` 与 `runtime:8.0`，请按实际使用的 SDK 版本调整后再进行容器化部署。

## 说明

- 本项目当前仅实现了 `m.room.message` 事件的转换与路由，`Context._converters` 是扩展更多事件类型的入口。
- 框架 API 大量使用内部（`internal`）成员，`MatrixBotClient`、`MatrixService`、`Context<T>`、各消息类型是其对外主要接口。
