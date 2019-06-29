namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly FailedCounter _failedCounter = new FailedCounter();
        private readonly NLogAdapter _nLogAdapter = new NLogAdapter();
        private readonly OtpService _otpService = new OtpService();
        private readonly ProfileDao _profileDao = new ProfileDao();
        private readonly Sha256Adapter _sha256Adapter = new Sha256Adapter();
        private readonly SlackAdapter _slackAdapter = new SlackAdapter();

        public bool Verify(string accountId, string password, string otp)
        {
            var isLocked = _failedCounter.IsAccountLocked(accountId);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }

            var currentPassword = _profileDao.GetPassword(accountId);

            var hashPassword = _sha256Adapter.Hash(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId);

            if (hashPassword == currentPassword && otp == currentOtp)
            {
                _failedCounter.ResetFailedCount(accountId);

                return true;
            }
            else
            {
                _failedCounter.AddFailedCount(accountId);

                int failedCount = _failedCounter.GetFailedCount(accountId);
                _nLogAdapter.Info($"accountId:{accountId} failed times:{failedCount}");

                _slackAdapter.PushMessage(accountId);

                return false;
            }
        }
    }
}