using System;
using DependencyInjectionWorkshop.Models;
using NSubstitute;
using NUnit.Framework;

namespace DependencyInjectionWorkshopTests
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        private const string DefaultAccount = "joey";
        private const string DefaultInputPassword = "9487";
        private const string DefaultOtp = "9527";
        private const string DefaultHashedPassword = "abc";
        private const int DefaultFailedCount = 91;
        private IAuthentication _authentication;
        private IFailedCounter _failedCounter;
        private IHash _hash;
        private ILogger _logger;
        private INotification _notification;
        private IOtpService _otpService;
        private IProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _notification = Substitute.For<INotification>();
            _failedCounter = Substitute.For<IFailedCounter>();
            _otpService = Substitute.For<IOtpService>();
            _hash = Substitute.For<IHash>();
            _profile = Substitute.For<IProfile>();

            var authenticationService =
                new AuthenticationService(_logger, _profile, _hash, _failedCounter, _otpService);

            var notificationDecorator = new NotificationDecorator(authenticationService, _notification);

            _authentication = new FailedCounterDecorator(_failedCounter, notificationDecorator);
        }

        [Test]
        public void is_valid()
        {
            GivenPasswordFromDb(DefaultAccount, DefaultHashedPassword);
            GivenHashPassword(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccount, DefaultOtp);

            var isValid = WhenVerify(DefaultAccount, DefaultInputPassword, DefaultOtp);
            ShouldBeValid(isValid);
        }

        [Test]
        public void is_invalid_when_otp_is_wrong()
        {
            GivenPasswordFromDb(DefaultAccount, DefaultHashedPassword);
            GivenHashPassword(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccount, DefaultOtp);

            var isValid = WhenVerify(DefaultAccount, DefaultInputPassword, "wrong otp");
            ShouldBeInvalid(isValid);
        }

        [Test]
        public void should_notify_when_invalid()
        {
            WhenInvalid();
            ShouldNotify(DefaultAccount);
        }

        [Test]
        public void should_add_failed_count_when_invalid()
        {
            WhenInvalid();
            ShouldAddFailedCount(DefaultAccount);
        }

        [Test]
        public void should_reset_failed_count_when_valid()
        {
            WhenValid();
            ShouldResetFailedCount(DefaultAccount);
        }

        [Test]
        public void should_log_failed_count_when_invalid()
        {
            GivenFailedCount(DefaultFailedCount);
            WhenInvalid();

            ShouldLog(DefaultAccount, DefaultFailedCount.ToString());
        }

        [Test]
        public void account_is_locked()
        {
            GivenAccountIsLocked();
            ShouldThrow<FailedTooManyTimesException>();
        }

        private static void ShouldBeInvalid(bool isValid)
        {
            Assert.IsFalse(isValid);
        }

        private static void ShouldBeValid(bool isValid)
        {
            Assert.IsTrue(isValid);
        }

        private void ShouldThrow<TException>() where TException : Exception
        {
            TestDelegate action = () => WhenValid();

            Assert.Throws<TException>(action);
        }

        private void GivenAccountIsLocked()
        {
            _failedCounter.IsAccountLocked(DefaultAccount).ReturnsForAnyArgs(true);
        }

        private void ShouldLog(string account, string failedCount)
        {
            _logger.Received().Info(Arg.Is<string>(m => m.Contains(account) && m.Contains(failedCount)));
        }

        private void GivenFailedCount(int failedCount)
        {
            _failedCounter.GetFailedCount(DefaultAccount).ReturnsForAnyArgs(failedCount);
        }

        private void ShouldResetFailedCount(string accountId)
        {
            _failedCounter.Received(1).ResetFailedCount(accountId);
        }

        private bool WhenValid()
        {
            GivenPasswordFromDb(DefaultAccount, DefaultHashedPassword);
            GivenHashPassword(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccount, DefaultOtp);

            var isValid = WhenVerify(DefaultAccount, DefaultInputPassword, DefaultOtp);
            return isValid;
        }

        private void ShouldAddFailedCount(string accountId)
        {
            _failedCounter.Received().AddFailedCount(accountId);
        }

        private void ShouldNotify(string account)
        {
            _notification.Received().PushMessage(Arg.Is<string>(m => m.Contains(account)));
        }

        private bool WhenInvalid()
        {
            GivenPasswordFromDb(DefaultAccount, DefaultHashedPassword);
            GivenHashPassword(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccount, DefaultOtp);

            var isValid = WhenVerify(DefaultAccount, DefaultInputPassword, "wrong otp");
            return isValid;
        }

        private bool WhenVerify(string accountId, string password, string otp)
        {
            var isValid = _authentication.Verify(accountId, password, otp);
            return isValid;
        }

        private void GivenOtp(string accountId, string otp)
        {
            _otpService.GetCurrentOtp(accountId).ReturnsForAnyArgs(otp);
        }

        private void GivenHashPassword(string password, string hashedPassword)
        {
            _hash.Compute(password).ReturnsForAnyArgs(hashedPassword);
        }

        private void GivenPasswordFromDb(string accountId, string passwordFromDb)
        {
            _profile.GetPassword(accountId).ReturnsForAnyArgs(passwordFromDb);
        }
    }
}