using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Playwright;
using YounBot.Permissions;
using YounBot.Utils;

namespace YounBot.Command.Implements;

public class BrowserCommand
{
    public CooldownUtils cooldown = new(75000);
    
    [Command("http", "HTTP测试")]
    public async Task Http(BotContext context, MessageChain chain, string url)
    {
        if (!cooldown.IsTimePassed(chain.FriendUin) && !Permission.HasPermission(chain))
        {
            if (cooldown.ShouldSendCooldownNotice(chain.FriendUin))
            {
                await MessageUtils.SendMessage(context, chain, $"你可以在 {cooldown.GetLeftTime(chain.FriendUin) / 1000} 秒后继续使用该指令");
            }
            return;
        }
        cooldown.Flag(chain.FriendUin);
        Task<MessageResult> preMessage = context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Forward(chain).Text("等一下喵...").Build());
        IPlaywright playwright = await Playwright.CreateAsync();
        IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        IPage page = await browser.NewPageAsync();
        await page.GotoAsync("https://zhale.me/http/");
        await page.WaitForLoadStateAsync();
        await page.FillAsync("#__nuxt > div > div.actions > div > form > div.search-container > div > div.search-input > input[type=text]", url);
        await page.ClickAsync("#__nuxt > div > div.actions > div > form > div.action-btn.btn-info");
        await page.WaitForLoadStateAsync();
        Thread.Sleep(3);
        // wait #__nuxt > div > div.ping-container > div.media-container > div.right-container > div > div.zha-card-body > div > div.process-container > div.loading disappear
        await page.WaitForSelectorAsync("#__nuxt > div > div.ping-container > div.media-container > div.right-container > div > div.zha-card-body > div > div.process-container");
        Thread.Sleep(1);
        string innerHtml = await page.InnerHTMLAsync("#__nuxt > div > div.ping-container > div.media-container > div.right-container > div > div.zha-card-body > div > div.process-container");
        while (innerHtml.Contains("data-process=\"100.00%\""))
        {
            Thread.Sleep(1);
            innerHtml = await page.InnerHTMLAsync("#__nuxt > div > div.ping-container > div.media-container > div.right-container > div > div.zha-card-body > div > div.process-container");
        }
        while (!innerHtml.Contains("data-process=\"100.00%\""))
        {
            Thread.Sleep(1);
            innerHtml = await page.InnerHTMLAsync("#__nuxt > div > div.ping-container > div.media-container > div.right-container > div > div.zha-card-body > div > div.process-container");
        }
        byte[] image = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true
        });
        preMessage.Wait();
        await context.RecallGroupMessage(chain.GroupUin!.Value, preMessage.Result);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Image(image).Build());
        await browser.CloseAsync();
    }
}