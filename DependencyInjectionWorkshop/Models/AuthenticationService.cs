using System;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly IFailedCounter _failedCounter;
        private readonly IHash _hash;
        private readonly ILogger _logger;
        private readonly INotification _notification;
        private readonly IOtpService _otpService;
        private readonly IProfile _profile;

        public AuthenticationService(ILogger logger, IProfile profile, INotification notification,
            IHash hash, IFailedCounter failedCounter, IOtpService otpService)
        {
            _logger = logger;
            _profile = profile;
            _notification = notification;
            _hash = hash;
            _failedCounter = failedCounter;
            _otpService = otpService;
        }

        public AuthenticationService()
        {
            _profile = new ProfileDao();
            _logger = new NLogAdapter();
            _notification = new SlackAdapter();
            _hash = new Sha256Adapter();
            _failedCounter = new FailedCounter();
            _otpService = new OtpService();
        }

        public bool Verify(string accountId, string password, string otp)
        {
            var isLocked = _failedCounter.IsAccountLocked(accountId);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }

            var currentPassword = _profile.GetPassword(accountId);

            var hashPassword = _hash.Compute(password);

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
                _logger.Info($"accountId:{accountId} failed times:{failedCount}");

                _notification.PushMessage(accountId);

                return false;
            }
        }
    }
}