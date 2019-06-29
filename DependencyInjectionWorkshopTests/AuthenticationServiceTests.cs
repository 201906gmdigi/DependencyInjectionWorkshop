using DependencyInjectionWorkshop.Models;
using NSubstitute;
using NUnit.Framework;

namespace DependencyInjectionWorkshopTests
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        [Test]
        public void is_valid()
        {
            var profile = Substitute.For<IProfile>();
            var hash = Substitute.For<IHash>();
            var otpService = Substitute.For<IOtpService>();
            var failedCounter = Substitute.For<IFailedCounter>();
            var notification = Substitute.For<INotification>();
            var logger = Substitute.For<ILogger>();

            var authenticationService = new AuthenticationService(
                logger,
                profile,
                notification,
                hash,
                failedCounter,
                otpService);

            profile.GetPassword("joey").ReturnsForAnyArgs("abc");
            hash.Compute("9487").ReturnsForAnyArgs("abc");

            otpService.GetCurrentOtp("joey").ReturnsForAnyArgs("9527");

            var isValid = authenticationService.Verify("joey", "9487", "9527");

            Assert.IsTrue(isValid);
        }
    }
}