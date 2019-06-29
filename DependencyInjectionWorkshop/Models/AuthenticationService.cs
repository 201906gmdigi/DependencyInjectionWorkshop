namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly ProfileDao _profileDao;
        private readonly SlackAdapter _slackAdapter;
        private readonly Sha256Adapter _sha256Adapter;
        private readonly FailedCounter _failedCounter;
        private readonly OtpService _otpService;
        private readonly NLogAdapter _nLogAdapter;

        public AuthenticationService(ProfileDao profileDao, SlackAdapter slackAdapter, Sha256Adapter sha256Adapter, FailedCounter failedCounter, OtpService otpService, NLogAdapter nLogAdapter)
        {
            _profileDao = profileDao;
            _slackAdapter = slackAdapter;
            _sha256Adapter = sha256Adapter;
            _failedCounter = failedCounter;
            _otpService = otpService;
            _nLogAdapter = nLogAdapter;
        }

        public AuthenticationService()
        {
            _profileDao = new ProfileDao();
            _slackAdapter = new SlackAdapter();
            _sha256Adapter = new Sha256Adapter();
            _failedCounter = new FailedCounter();
            _otpService = new OtpService();
            _nLogAdapter = new NLogAdapter();
        }

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