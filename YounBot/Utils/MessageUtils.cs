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

    public static List<string> GetMultiMessageContent(MultiMsgEntity? multiMsgEntity, int layer = 0)
    {
        if (layer >= 4)
        {
            return ["[Too many layers]"];
        }
        List<string> result = [];
        foreach (MessageChain chain in multiMsgEntity.Chains)
        {
            result.Add($"{chain.GroupMemberInfo.MemberName}: {GetPlainText(chain)}");
            MultiMsgEntity? innerMultiMsgEntity = chain.FirstOrDefault(entity => entity is MultiMsgEntity) as MultiMsgEntity;
            if (innerMultiMsgEntity != null)
            {
                List<string> innerChain = GetMultiMessageContent(innerMultiMsgEntity, layer + 1);
                if (innerChain.Count > 0)
                {
                    result.AddRange(innerChain.Select(inner => $"  {inner}"));
                }
            }
        }
        return result;
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
    
    public static List<IMessageEntity> GetMessageEntitiesFromMultiMsg(MultiMsgEntity multiMsgEntity)
    {
        List<IMessageEntity> entities = [];
        foreach (IMessageEntity messageEntity in multiMsgEntity.Chains.SelectMany(messageChain => messageChain))
        {
            switch (messageEntity)
            {
                case TextEntity or ImageEntity:
                    entities.Add(messageEntity);
                    break;
                case MultiMsgEntity entity:
                    entities.AddRange(GetMessageEntitiesFromMultiMsg(entity));
                    break;
            }
        }

        return entities;
    }
}