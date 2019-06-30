using System;
using System.Threading;

namespace MyConsole
{
    
    public class Wallet : IWallet
    {
        [CacheResult(Duration = 1000)]
        public string CreateGuid(string account, int token)
        {
            Console.WriteLine($"sleep 1.5 seconds, account:{account}, token:{token}");
            Thread.Sleep(1500);
            return Guid.NewGuid().ToString("N");
        }
    }
}