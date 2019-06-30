namespace DependencyInjectionWorkshop.Models
{
    public class NotificationDecorator : BaseAuthenticationDecorator
    {
        private readonly INotification _notification;

        public NotificationDecorator(IAuthentication authentication, INotification notification) : base(authentication)
        {
            _notification = notification;
        }

        public override bool Verify(string accountId, string password, string otp)
        {
            var isValid = base.Verify(accountId, password, otp);
            if (!isValid)
            {
                _notification.PushMessage(accountId);
            }

            return isValid;
        }
    }
}