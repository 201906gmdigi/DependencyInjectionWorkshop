using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dapper;
using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public class Sha256Adapter
    {
        public string Hash(string plainText)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(plainText));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            return hash.ToString();
        }
    }

    public class FailedCounter
    {
        public void AddFailedCount(string accountId)
        {
            var addFailedCountResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                                         .PostAsJsonAsync("api/failedCounter/Add", accountId).Result;
            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        public int GetFailedCount(string accountId)
        {
            var failedCountResponse =
                new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                    .PostAsJsonAsync("api/failedCounter/GetFailedCount", accountId).Result;

            failedCountResponse.EnsureSuccessStatusCode();

            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        public void ResetFailedCount(string accountId)
        {
            var resetResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                                .PostAsJsonAsync("api/failedCounter/Reset", accountId).Result;
            resetResponse.EnsureSuccessStatusCode();
        }
    }

    public class NLogAdapter
    {
        public void Info(string message)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }
    }

    public class SlackAdapter
    {
        public void PushMessage(string accountId)
        {
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(postMessageResponse => { }, "my channel", $"{accountId} message", "my bot name");
        }
    }

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
            var isLocked = IsAccountLocked(accountId);
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

        private static bool IsAccountLocked(string accountId)
        {
            var isLockedResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                                   .PostAsJsonAsync("api/failedCounter/IsLocked", accountId).Result;

            isLockedResponse.EnsureSuccessStatusCode();
            var isLocked = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isLocked;
        }
    }

    public class OtpService
    {
        public string GetCurrentOtp(string accountId)
        {
            var response = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                           .PostAsJsonAsync("api/otps", accountId).Result;
            string currentOtp;
            if (response.IsSuccessStatusCode)
            {
                currentOtp = response.Content.ReadAsAsync<string>().Result;
            }
            else
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }

            return currentOtp;
        }
    }

    public class ProfileDao
    {
        public string GetPassword(string accountId)
        {
            string currentPassword;
            using (var connection = new SqlConnection("datasource=db,password=abc"))
            {
                currentPassword = connection.Query<string>("spGetUserPassword", new {Id = accountId},
                                                           commandType: CommandType.StoredProcedure).SingleOrDefault();
            }

            return currentPassword;
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}