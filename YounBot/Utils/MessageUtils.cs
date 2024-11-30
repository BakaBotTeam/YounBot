using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

namespace YounBot.Utils;

public static class MessageUtils
{
    public static string GetPlainText(MessageChain chain)
    {
        string plainText = "";
        foreach (IMessageEntity messageEntity in chain)
        {
            if (messageEntity is MentionEntity)
            {
                plainText += "@" + (messageEntity as MentionEntity)!.Uin;
            }
            else
            {
                plainText += messageEntity.ToPreviewText();
            }
        }

        return plainText;
    }
    
    public static string GetPlainTextForCheck(MessageChain chain)
    {
        string plainText = "";
        foreach (IMessageEntity messageEntity in chain)
        {
            if (messageEntity is TextEntity)
            {
                plainText += messageEntity.ToPreviewText();
            } else if (messageEntity is MultiMsgEntity)
            {
                foreach (MessageChain messageChain in (messageEntity as MultiMsgEntity)!.Chains)
                {
                    plainText += "\n" + GetPlainTextForCheck(messageChain);
                }
            }
        }

        return plainText;
    }
    
    public static async Task SendMessage(BotContext context, MessageChain chain, string message, bool mention = false)
    {
        if (mention)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Mention(chain.FriendUin).Text($" {message}").Build());
        }
        else
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text(message).Build());
        }
    }
    
    public static async Task SendImage(BotContext context, MessageChain chain, byte[] imageBytes, bool mention = false)
    {
        if (mention)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Mention(chain.FriendUin).Image(imageBytes).Build());
        }
        else
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Image(imageBytes).Build());
        }
    }
}