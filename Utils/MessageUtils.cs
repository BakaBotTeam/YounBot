using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

namespace YounBot.Utils;

public class MessageUtils
{
    public static string GetPlainText(MessageChain chain)
    {
        var plainText = "";
        foreach (var messageEntity in chain)
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
}