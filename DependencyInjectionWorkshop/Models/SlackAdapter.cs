using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public interface INotification
    {
        void PushMessage(string accountId);
    }

    public class SlackAdapter : INotification
    {
        public void PushMessage(string accountId)
        {
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(postMessageResponse => { }, "my channel", $"{accountId} message", "my bot name");
        }
    }
}