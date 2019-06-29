using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public class SlackAdapter
    {
        public void PushMessage(string accountId)
        {
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(postMessageResponse => { }, "my channel", $"{accountId} message", "my bot name");
        }
    }
}