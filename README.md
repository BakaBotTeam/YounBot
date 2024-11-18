# YounBot

YounBot is a bot developed based on Lagrange.Core, designed to manage group chats and provide practical functions.

## How 2 Create a Cloudflare Worker AI for YounBot?
* Create a new Cloudflare Worker
* Paste the code below
* Replace `username:password` with your own username and password
```javascript
export default {
  async fetch(request, env) {
    const auth = request.headers.get('Authorization');
    const expectedAuth = 'Basic ' + btoa('username:password'); // Replace 'username:password' with your credentials

    if (auth !== expectedAuth) {
      return new Response('Unauthorized', { status: 401, headers: { 'WWW-Authenticate': 'Basic realm="User Visible Realm"' } });
    }

    const tasks = [];
    const url = new URL(request.url);
    const content = url.searchParams.get('content');
    // messages - chat style input
    let chat = {
      messages: [
        { role: 'system', content: '你是一个文本内容检测专家 你的任务是分析给定的消息并判断其是否包含以下类型的违规内容\n1 广告\n2 政治敏感内容\n\n你的输出必须严格遵循以下要求\n1 如果消息包含违规内容 请输出 true|违规类型|判断理由\n2 如果消息不包含违规内容 请输出 false|无\n\n注意\n违规类型只能是广告或政治敏感内容 判断理由必须清晰简洁 不得包含消息中的任何原句文本 对于政治敏感内容的判断 不得使用任何可能的政治敏感关键词 仅需描述判断的逻辑依据或模式 你的回复必须使用中文\n如果只是一些游戏平台的网址则不应该被判断为广告\n你的输出必须严格遵循以下要求\n1 如果消息包含违规内容 请输出 true|违规类型|判断理由\n2 如果消息不包含违规内容 请输出 false|无\n\n下面将会给出一段消息记录, 会强调你要检查的是谁的消息, 消息记录格式是 用户名称(用户ID) -> 聊天文本(可能为空)' },
        { role: 'user', content: content || "Apotheosis 免费客户端 可绕过Hypixel Hyt 加群934454805获取" }
      ]
    };
    let response = await env.AI.run('@cf/qwen/qwen1.5-7b-chat-awq', chat);
    tasks.push({ inputs: chat, response });

    return Response.json(tasks);
  }
};
```

## Conbrtibuting

### Prerequisites

- .NET 6.0

### Configuration

1. Clone the repository:
    ```sh
    git clone https://github.com/BakaBotTeam/YounBot.git
    ```
2. Navigate to the project directory:
    ```sh
    cd YounBot
    ```
3. Restore the dependencies:
    ```sh
    dotnet restore
    ```

### Build&Run

1. Build the project:
    ```sh
    dotnet build
    ```
2. Run the project:
    ```sh
    dotnet run
    ```