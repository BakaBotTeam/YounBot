using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lagrange.Core.Common;
using Lagrange.Core.Utility.Sign;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YounBot.Utils;

namespace YounBot.Signer;

public class OneBotSigner : SignProvider
{
    private readonly IConfiguration _configuration;

    private readonly ILogger _logger;

    private const string Url = "https://sign.lagrangecore.org/api/sign/25765";

    private readonly string? _signServer;

    private readonly HttpClient _client;

    private readonly BotAppInfo? _info;

    private readonly string platform;

    private readonly string version;

    public OneBotSigner(IConfiguration config, ILogger logger)
    {
        _configuration = config;
        _logger = logger;

        _signServer = string.IsNullOrEmpty(config["SignServerUrl"]) ? Url : config["SignServerUrl"];
        logger.LogInformation("Using signature service: " + _signServer);
        string? signProxyUrl = config["SignProxyUrl"]; // Only support HTTP proxy

        _client = new HttpClient(handler: new HttpClientHandler
        {
            Proxy = string.IsNullOrEmpty(signProxyUrl) ? null : new WebProxy()
            {
                Address = new Uri(signProxyUrl),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
            },
        }, disposeHandler: true);

        if (string.IsNullOrEmpty(_signServer)) logger.LogWarning("Signature Service is not available, login may be failed");

        _info ??= GetAppInfo();
        platform = _info.Os switch
        {
            "Windows" => "Windows",
            "Mac" => "MacOs",
            "Linux" => "Linux",
            _ => "Unknown"
        };
        version = _info.CurrentVersion;
        logger.LogInformation("Using version: " + version);
    }

    public override byte[]? Sign(string cmd, uint seq, byte[] body, [UnscopedRef] out byte[]? e, [UnscopedRef] out string? t)
    {
        e = null;
        t = null;

        if (!WhiteListCommand.Contains(cmd)) return null;
        if (_signServer == null) throw new Exception("Sign server is not configured");

        using HttpRequestMessage request = new();
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri(_signServer);
        request.Content = JsonContent.Create(new JsonObject
        {
            { "cmd", cmd },
            { "seq", seq },
            { "src", Convert.ToHexString(body) }
        });

        using HttpResponseMessage message = _client.Send(request);
        if (message.StatusCode != HttpStatusCode.OK) throw new Exception($"Signer server returned a {message.StatusCode}");
        JsonElement json = JsonDocument.Parse(message.Content.ReadAsStream()).RootElement;

        if (json.TryGetProperty("platform", out JsonElement platformJson))
        {
            if (platformJson.GetString() != platform) throw new Exception("Signer platform mismatch");
        }
        else
        {
            _logger.LogWarning("Signer platform miss");
        }

        if (json.TryGetProperty("version", out JsonElement versionJson))
        {
            if (versionJson.GetString() != version) throw new Exception("Signer version mismatch");
        }
        else
        {
            _logger.LogWarning("Signer version miss");
        }

        JsonElement valueJson = json.GetProperty("value");
        JsonElement extraJson = valueJson.GetProperty("extra");
        JsonElement tokenJson = valueJson.GetProperty("token");
        JsonElement signJson = valueJson.GetProperty("sign");

        string? token = tokenJson.GetString();
        string? extra = extraJson.GetString();
        e = extra != null ? Convert.FromHexString(extra) : [];
        t = token != null ? Encoding.UTF8.GetString(Convert.FromHexString(token)) : "";
        string sign = signJson.GetString() ?? throw new Exception("Signer server returned an empty sign");
        return Convert.FromHexString(sign);
    }

    public BotAppInfo GetAppInfo()
    {
        if (_info != null) return _info;

        return FallbackAsync<BotAppInfo>.Create()
            .Add(async token =>
            {
                try { return await _client.GetFromJsonAsync<BotAppInfo>($"{_signServer}/appinfo", token); }
                catch { return null; }
            })
            .Add(token =>
            {
                string path = _configuration["ConfigPath:AppInfo"] ?? "appinfo.json";

                if (!File.Exists(path)) return Task.FromResult(null as BotAppInfo);

                try { return Task.FromResult(JsonSerializer.Deserialize<BotAppInfo>(File.ReadAllText(path))); }
                catch { return Task.FromResult(null as BotAppInfo); }
            })
            .ExecuteAsync(token => Task.FromResult(
                BotAppInfo.ProtocolToAppInfo[_configuration["Account:Protocol"] switch
                {
                    "Windows" => Protocols.Windows,
                    "MacOs" => Protocols.MacOs,
                    _ => Protocols.Linux,
                }]
            ))
            .Result;
    }
}