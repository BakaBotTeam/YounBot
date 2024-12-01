using Lagrange.Core.Event.EventArg;

namespace YounBot.Listener;

public class NewMemberWelcome
{
    private static string[] welcomeMessage = new[]
    {
        "Welcome! We hope you bring a pizza with you!",
    };
    
    public static async Task OnGroupMemberIncrease(GroupMemberIncreaseEvent args)
    {
        
    }
}