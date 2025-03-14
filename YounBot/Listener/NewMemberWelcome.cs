﻿using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;

namespace YounBot.Listener;

public class NewMemberWelcome
{
    private static string[] _welcomeMessage = new[]
    {
        "{0} just joined the server - glhf!",
        "{0} just joined. Everyone, look busy!",
        "{0} just joined. Can I get a heal?",
        "{0} joined your party.",
        "{0} joined. You must construct additional pylons.",
        "Ermagherd. {0} is here.",
        "Welcome, {0}. Stay awhile and listen.",
        "Welcome, {0}. We were expecting you ( ͡° ͜ʖ ͡°)",
        "Welcome, {0}. We hope you brought pizza.",
        "Welcome {0}. Leave your weapons by the door.",
        "A wild {0} appeared.",
        "Swoooosh. {0} just landed.",
        "Brace yourselves. {0} just joined the server.",
        "{0} just joined. Hide your bananas.",
        "{0} just arrived. Seems OP - please nerf.",
        "{0} just slid into the server.",
        "A {0} has spawned in the server.",
        "Big {0} showed up!",
        "Where’s {0}? In the server!",
        "{0} hopped into the server. Kangaroo!!",
        "{0} just showed up. Hold my beer.",
        "Challenger approaching - {0} has appeared!",
        "It's a bird! It's a plane! Nevermind, it's just {0}.",
        "It's {0}! Praise the sun! [T]/",
        "Never gonna give {0} up. Never gonna let {0} down.",
        "Ha! {0} has joined! You activated my trap card!",
        "Cheers, love! {0}'s here!",
        "Hey! Listen! {0} has joined!",
        "We've been expecting you {0}",
        "It's dangerous to go alone, take {0}!",
        "{0} has joined the server! It's super effective!",
        "Cheers, love! {0} is here!",
        "{0} is here, as the prophecy foretold.",
        "{0} has arrived. Party's over.",
        "Ready player {0}",
        "{0} is here to kick butt and chew bubblegum. And {0} is all out of gum.",
        "Hello. Is it {0} you're looking for?",
        "{0} has joined. Stay a while and listen!",
        "Roses are red, violets are blue, {0} joined this server with you",
    };
    
    public static async Task OnGroupMemberIncrease(GroupMemberIncreaseEvent args)
    {
        MessageBuilder mb = MessageBuilder.Group(args.GroupUin);
        string messageTemplate = _welcomeMessage[new Random().Next(_welcomeMessage.Length)];
        string[] messageParts = messageTemplate.Split("{0}");

        if (messageParts.Length == 1)
        {
            // {0} is at the beginning or end
            if (messageTemplate.StartsWith("{0}"))
            {
                mb.Mention(args.MemberUin);
                mb.Text(messageParts[0]);
            }
            else
            {
                mb.Text(messageParts[0]);
                mb.Mention(args.MemberUin);
            }
        }
        else
        {
            // {0} is in the middle
            mb.Text(messageParts[0]);
            mb.Mention(args.MemberUin);
            mb.Text(messageParts[1]);
        }

        await YounBotApp.Client!.SendMessage(mb.Build());
    }
}