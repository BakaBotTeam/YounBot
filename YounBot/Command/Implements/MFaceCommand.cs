using System.IO.Compression;
using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using YounBot.Utils;

namespace YounBot.Command.Implements;

public class MFaceCommand
{
    [Command("mface", "我要这个表情包的所有信息！")]
    public async Task MFaceGet(BotContext context, MessageChain chain, int id = -1, string faceId = "")
    {
        if (id == -1)
        {
            ForwardEntity? forwardEntity = chain.FirstOrDefault(entity => entity is ForwardEntity) as ForwardEntity;
            if (forwardEntity == null)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("你要找的啥？").Build());
                return;
            }
            MessageChain? forwardMessage = (await context.GetGroupMessage(chain.GroupUin.Value, forwardEntity.Sequence, forwardEntity.Sequence + 1)).FirstOrDefault(_e => _e.Sequence == forwardEntity.Sequence);
            if (forwardMessage == null)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("你回复的啥消息...?").Build());
                return;
            }
            MarketfaceEntity? marketfaceEntity = forwardMessage.FirstOrDefault(entity => entity is MarketfaceEntity) as MarketfaceEntity;
            if (marketfaceEntity == null)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("这不是mface来着...").Build());
                return;
            }
            id = marketfaceEntity.EmojiPackageId;
            faceId = marketfaceEntity.EmojiId;
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text($"表情包ID: {id}").Build());
        }
        Task<MessageResult> preMessage = context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text("让我找找你要的东西...").Build());
        string url = $"https://gxh.vip.qq.com/qqshow/admindata/comdata/vipEmoji_item_{id}/xydata.json";
        JsonObject response = await HttpUtils.GetJsonObject(url);
        if (!response.TryGetPropertyValue("data", out JsonNode? _))
        {
            preMessage.Wait();
            await context.RecallGroupMessage(chain.GroupUin.Value, preMessage.Result);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("好奇怪...我找不到这啥表情包").Build());
        }
        LoggingUtils.Logger.LogInformation($"Found mface item {id}, name: {response["data"]["baseInfo"][0]["name"].GetValue<string>()}");
        url = $"https://gxh.vip.qq.com/club/item/parcel/{id % 10}/{id}_android.json";
        JsonObject detail = await HttpUtils.GetJsonObject(url);
        if (faceId != "" && detail["imgs"].AsArray().FirstOrDefault(_e => _e["id"].GetValue<string>() == faceId) == null)
        {
            preMessage.Wait();
            await context.RecallGroupMessage(chain.GroupUin.Value, preMessage.Result);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("好奇怪...我没法在表情包里找到这个表情").Build());
            return;
        }
        int width = detail["supportSize"][0]["Width"].GetValue<int>();
        int height = detail["supportSize"][0]["Height"].GetValue<int>();

        if (faceId != "")
        {
            url = $"https://gxh.vip.qq.com/club/item/parcel/item/{faceId[..2]}/{faceId}/raw{width}.gif";
            byte[] rawImage = await HttpUtils.GetBytes(url);
            using (Image image = Image.Load(rawImage))
            {
                if (image.Frames.Count == 1)
                {
                    url = $"https://gxh.vip.qq.com/club/item/parcel/item/{faceId[..2]}/{faceId}/{height}x{width}.png";
                    rawImage = await HttpUtils.GetBytes(url);
                }
            }
            preMessage.Wait();
            await context.RecallGroupMessage(chain.GroupUin.Value, preMessage.Result);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Forward(chain).Image(rawImage).Build());
        }
        else
        {
            List<string> ids = new();
            foreach (JsonNode img in detail["imgs"].AsArray())
            {
                ids.Add(img["id"].GetValue<string>());
            }
            using (MemoryStream zipStream = new())
            {
                using (ZipArchive zip = new(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (string _id in ids)
                    {
                        bool isDyamticPackage = true;
                        url = $"https://gxh.vip.qq.com/club/item/parcel/item/{_id[..2]}/{_id}/raw{width}.gif";
                        byte[] imageBytes = await HttpUtils.GetBytes(url);
                        using (Image image = Image.Load(imageBytes))
                        {
                            if (image.Frames.Count == 1)
                            {
                                url = $"https://gxh.vip.qq.com/club/item/parcel/item/{_id[..2]}/{_id}/{height}x{width}.png";
                                imageBytes = await HttpUtils.GetBytes(url);
                                isDyamticPackage = false;
                            }
                        }
                        ZipArchiveEntry entry = zip.CreateEntry(isDyamticPackage ? $"{_id}.gif" : $"{_id}.png");
                        await using Stream entryStream = entry.Open();
                        await entryStream.WriteAsync(imageBytes);
                    }
                }

                preMessage.Wait();
                await context.RecallGroupMessage(chain.GroupUin.Value, preMessage.Result);
                preMessage = context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("找到了！正在上传...").Build());
                zipStream.Seek(0, SeekOrigin.Begin);
                OperationResult<object> fsUploadResult = await context.GroupFSUploadWithResult(chain.GroupUin.Value, new FileEntity(zipStream.GetBuffer(), $"{id}.zip"));
                preMessage.Wait();
                await context.RecallGroupMessage(chain.GroupUin.Value, preMessage.Result);
                if (!fsUploadResult.IsSuccess)
                {
                    await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("上传失败... " + fsUploadResult.Message).Build());
                }
            }
        }
    }
    
    [Command("imgfile", "我想要这张图的文件！")]
    public async Task ImageFile(BotContext context, MessageChain chain)
    {
        ForwardEntity? forwardEntity = chain.FirstOrDefault(entity => entity is ForwardEntity) as ForwardEntity;
        if (forwardEntity == null)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("你要找的啥？").Build());
            return;
        }
        MessageChain? forwardMessage = (await context.GetGroupMessage(chain.GroupUin.Value, forwardEntity.Sequence, forwardEntity.Sequence + 1)).FirstOrDefault(_e => _e.Sequence == forwardEntity.Sequence);
        if (forwardMessage == null)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("你回复的啥消息...?").Build());
            return;
        }
        ImageEntity? imageEntity = forwardMessage.FirstOrDefault(entity => entity is ImageEntity) as ImageEntity;
        if (imageEntity == null)
        {
            string tips = "这不是图片来着...";
            MarketfaceEntity? marketfaceEntity = forwardMessage.FirstOrDefault(entity => entity is MarketfaceEntity) as MarketfaceEntity;
            if (marketfaceEntity != null)
            {
                tips += $"\nTips: 这是个MFace! 你应当先使用 {YounBotApp.Configuration["CommandPrefix"] ?? "/"}mface 命令";
            }
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text(tips).Build());
            return;
        }
        byte[] imageBytes = await HttpUtils.GetBytes(imageEntity.ImageUrl);
        string fileName = $"{imageEntity.ImageUrl.Split('/').Last().Split("?").First().Split(".").First().Split("#").First()}";
        if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
        {
            fileName += ".gif";
        }
        else
        {
            using (Image image = Image.Load(imageBytes))
            using (MemoryStream pngStream = new())
            {
                image.SaveAsPng(pngStream);
                imageBytes = pngStream.GetBuffer();
                fileName += ".png";
            }
        }
        OperationResult<object> fsUploadResult = await context.GroupFSUploadWithResult(chain.GroupUin.Value, new FileEntity(imageBytes, fileName));
        if (!fsUploadResult.IsSuccess)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("上传失败... " + fsUploadResult.Message).Build());
        }
    }
    
    [Command("urlimg", "Test")]
    public async Task UrlImage(BotContext context, MessageChain chain, string url)
    {
        if (!Permissions.Permission.HasPermission(chain))
        {
            return;
        }
        byte[] imageBytes = await HttpUtils.GetBytes(url);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Image(imageBytes).Build());
    }
    
    [Command("testupload", "test")]
    public async Task TestUpload(BotContext context, MessageChain chain, string url)
    {
        byte[] imageBytes = await HttpUtils.GetBytes(url);
        await context.UploadImage(new ImageEntity(imageBytes));
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("Succees").Build());
    }

    [Command("imgbed", "上传图片到神秘图床 (回复图片/Url参数)")]
    public async Task ImgBed(BotContext context, MessageChain chain, string url = "")
    {
        if (url == "")
        {
            ForwardEntity? forwardEntity = chain.FirstOrDefault(entity => entity is ForwardEntity) as ForwardEntity;
            if (forwardEntity == null)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("你要传的啥...?").Build());
                return;
            }
            MessageChain? forwardMessage = (await context.GetGroupMessage(chain.GroupUin.Value, forwardEntity.Sequence, forwardEntity.Sequence + 1)).FirstOrDefault(_e => _e.Sequence == forwardEntity.Sequence);
            if (forwardMessage == null)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("你回复的啥...?").Build());
                return;
            }
            ImageEntity? imageEntity = forwardMessage.FirstOrDefault(entity => entity is ImageEntity) as ImageEntity;
            if (imageEntity == null)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("这有图片吗...?").Build());
                return;
            }
            url = imageEntity.ImageUrl;
        }
        Task<MessageResult> preMessage = context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text("正在上传...").Build());
        byte[] imageBytes = await HttpUtils.GetBytes(url);
        string fileName = "/setting/" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
        if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
        {
            fileName += ".gif";
        }
        else
        {
            using (Image image = Image.Load(imageBytes))
            using (MemoryStream pngStream = new())
            {
                image.SaveAsPng(pngStream);
                imageBytes = pngStream.GetBuffer();
                fileName += ".png";
            }
        }
        string gitCodeToken = YounBotApp.Configuration["GitCodeToken"] ?? throw new Exception("GitCodeToken not configured");
        string gitCodeUrl = "https://web-api.gitcode.com/api/v1/obs/image";
        Dictionary<string, string> headers = new()
        {
            ["Authorization"] = $"Bearer {gitCodeToken}",
            ["user-agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.0.0",
            ["origin"] = "https://gitcode.com",
            ["referer"] = "https://gitcode.com/",
            ["x-app-channel"] = "gitcode-fe",
            ["x-app-version"] = "0",
            ["x-device-id"] = "unknown",
            ["x-device-type"] = "Windows",
            ["x-network-type"] = "4g",
            ["x-os-version"] = "10",
            ["x-platform"] = "web"
        };
        JsonObject data = new()
        {
            ["object_key"] = fileName,
            ["is_avatar"] = false,
            ["content_type"] = "image/" + fileName.Split(".").Last(),
            ["file_type"] = fileName.Split(".").Last()
        };
        JsonObject response = await HttpUtils.PostJsonObject(gitCodeUrl, headers: headers, data: data);
        string uploadUrl = ((IDictionary<string, JsonNode?>)response).Keys.First();
        if (!uploadUrl.StartsWith("https://") && !uploadUrl.StartsWith("http://"))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("上传失败...服务器返回了一个奇怪的上传Url").Build());
        }
        string downloadUrl = response[uploadUrl]["cdn-img-addr"].GetValue<string>();
        if (!downloadUrl.StartsWith("https://") && !downloadUrl.StartsWith("http://"))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("上传失败...服务器返回了一个奇怪的下载Url").Build());
        }
        IDictionary<string, JsonNode?> uploadInfo = (IDictionary<string, JsonNode?>)response[uploadUrl];
        Dictionary<string, string> uploadHeader = new();
        foreach (KeyValuePair<string, JsonNode?> pair in uploadInfo)
        {
            if (!pair.Key.ToLower().StartsWith("x-")) continue;
            uploadHeader.Add(pair.Key, pair.Value!.GetValue<string>());
        }
        HttpClient client = new();
        HttpRequestMessage request = new(HttpMethod.Put, uploadUrl);
        request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.0.0");
        request.Headers.Add("origin", "https://gitcode.com");
        request.Headers.Add("referer", "https://gitcode.com/");
        foreach (KeyValuePair<string, string> pair in uploadHeader)
        {
            request.Headers.Add(pair.Key, pair.Value);
        }
        request.Content = new ByteArrayContent(imageBytes);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/" + fileName.Split(".").Last());
        HttpResponseMessage uploadResponse = await client.SendAsync(request);
        uploadResponse.EnsureSuccessStatusCode();
        preMessage.Wait();
        await context.RecallGroupMessage(chain.GroupUin.Value, preMessage.Result);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text($"上\u2606传\u2606大\u2606成\u2606功\n{downloadUrl}").Build());
    }
}