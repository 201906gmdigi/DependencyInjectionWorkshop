using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.DynamicProxy;
using DependencyInjectionWorkshop.Models;

namespace MyConsole
{
    public interface ICacheProvider
    {
        object Get(string key);

        void Put(string key, object value, int duration);

        bool Contains(string key);
    }

    class Program
    {
        private static IContainer _container;

        static void Main(string[] args)
        {
            RegisterContainer();

            var wallet = _container.Resolve<IWallet>();
            //var wallet = new Wallet();
            Console.WriteLine(wallet.CreateGuid("Joey", 91));
            Console.WriteLine(wallet.CreateGuid("Joey", 91));
            Console.WriteLine(wallet.CreateGuid("Tom", 66));
            Console.WriteLine(wallet.CreateGuid("Joey", 91));
            //IAuthentication authentication = _container.Resolve<IAuthentication>();
            //var isValid = authentication.Verify("joey", "pw", "123457");

            //Console.WriteLine(isValid);
        }

        private static void RegisterContainer()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<Wallet>()
                            .As<IWallet>()
                            .EnableInterfaceInterceptors()
                            .InterceptedBy(typeof(CacheInterceptor));

            containerBuilder.RegisterType<CacheInterceptor>();
            containerBuilder.RegisterType<MemoryCacheProvider>()
                            .As<ICacheProvider>();

            containerBuilder.RegisterType<FakeHash>()
                            .As<IHash>();
            containerBuilder.RegisterType<FakeProfile>()
                            .As<IProfile>();
            containerBuilder.RegisterType<FakeOtp>()
                            .As<IOtpService>();
            containerBuilder.RegisterType<FakeFailedCounter>()
                            .As<IFailedCounter>();
            containerBuilder.RegisterType<FakeSlack>()
                            .As<INotification>();
            containerBuilder.RegisterType<ConsoleAdapter>()
                            .As<ILogger>();

            containerBuilder.RegisterType<AuthenticationService>()
                            .As<IAuthentication>();

            containerBuilder.RegisterDecorator<NotificationDecorator, IAuthentication>();
            containerBuilder.RegisterDecorator<FailedCounterDecorator, IAuthentication>();
            containerBuilder.RegisterDecorator<LogFailedCountDecorator, IAuthentication>();
            containerBuilder.RegisterDecorator<LogMethodInfoDecorator, IAuthentication>();

            _container = containerBuilder.Build();
        }
    }

    internal class LogMethodInfoDecorator : BaseAuthenticationDecorator
    {
        private readonly ILogger _logger;

        public LogMethodInfoDecorator(IAuthentication authentication, ILogger logger) : base(authentication)
        {
            _logger = logger;
        }

        public override bool Verify(string accountId, string password, string otp)
        {
            var message = $"{nameof(AuthenticationService)}.{nameof(Verify)}:{accountId} | {password} |{otp}";
            _logger.Info(message);

            var isValid = base.Verify(accountId, password, otp);

            _logger.Info($"{accountId} isValid: {isValid.ToString()}");

            return isValid;
        }
    }

    internal class ConsoleAdapter : ILogger
    {
        public void Info(string message)
        {
            Console.WriteLine(message);
        }
    }

    internal class FakeSlack : INotification
    {
        public void PushMessage(string message)
        {
            Console.WriteLine($"{nameof(FakeSlack)}.{nameof(PushMessage)}({message})");
        }
    }

    internal class FakeFailedCounter : IFailedCounter
    {
        public void ResetFailedCount(string accountId)
        {
            Console.WriteLine($"{nameof(FakeFailedCounter)}.{nameof(ResetFailedCount)}({accountId})");
        }

        public void AddFailedCount(string accountId)
        {
            Console.WriteLine($"{nameof(FakeFailedCounter)}.{nameof(AddFailedCount)}({accountId})");
        }

        public int GetFailedCount(string accountId)
        {
            Console.WriteLine($"{nameof(FakeFailedCounter)}.{nameof(GetFailedCount)}({accountId})");
            return 91;
        }

        public bool IsAccountLocked(string accountId)
        {
            Console.WriteLine($"{nameof(FakeFailedCounter)}.{nameof(IsAccountLocked)}({accountId})");
            return false;
        }
    }

    internal class FakeOtp : IOtpService
    {
        public string GetCurrentOtp(string accountId)
        {
            Console.WriteLine($"{nameof(FakeOtp)}.{nameof(GetCurrentOtp)}({accountId})");
            return "123456";
        }
    }

    internal class FakeHash : IHash
    {
        public string Compute(string plainText)
        {
            Console.WriteLine($"{nameof(FakeHash)}.{nameof(Compute)}({plainText})");
            return "my hashed password";
        }
    }

    internal class FakeProfile : IProfile
    {
        public string GetPassword(string accountId)
        {
            Console.WriteLine($"{nameof(FakeProfile)}.{nameof(GetPassword)}({accountId})");
            return "my hashed password";
        }
    }
}