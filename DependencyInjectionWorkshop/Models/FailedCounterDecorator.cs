namespace DependencyInjectionWorkshop.Models
{
    public class FailedCounterDecorator : IAuthentication
    {
        private readonly IAuthentication _authentication;
        private readonly IFailedCounter _failedCounter;

        public FailedCounterDecorator(IFailedCounter failedCounter, IAuthentication authentication)
        {
            _failedCounter = failedCounter;
            _authentication = authentication;
        }

        public bool Verify(string accountId, string password, string otp)
        {
            CheckAccountIsLocked(accountId);
            return _authentication.Verify(accountId, password, otp);
        }

        private void CheckAccountIsLocked(string accountId)
        {
            var isLocked = _failedCounter.IsAccountLocked(accountId);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }
        }
    }
}