using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependencyInjectionWorkshop.Models;

namespace MyConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var notificationDecorator = new NotificationDecorator(new AuthenticationService(
                                                                      new ProfileDao(),
                                                                      new Sha256Adapter(),
                                                                      new OtpService()),new SlackAdapter());
            var failedCounterDecorator = new FailedCounterDecorator(new FailedCounter(), notificationDecorator);
            var logFailedCountDecorator = new LogFailedCountDecorator(failedCounterDecorator, new FailedCounter(),new NLogAdapter());

            var isValid = logFailedCountDecorator.Verify("joey","9487","9527");
            Console.WriteLine(isValid);
        }
    }
}
