using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using IdentityModel;
using IdentityModel.Client;
using IdentityModel.Extensions;
using MR_Config;
using Newtonsoft.Json.Linq;

namespace ConsoleResourceOwnerWithUserInfo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Thread.Sleep(3000); //Give the self hosted service a chance to launch
            
            //Print out LastUpdate datetime value and kid info from all sources
            GetInfoIdSrvr();
            GetInfoDirect("MembershipReboot");
            GetInfoModifiedAll("MembershipReboot");
            GetInfoModifiedAtr("MembershipReboot");

            Console.ReadLine();
        }

        private static void GetInfoIdSrvr()
        {
            var response = RequestToken();
            //ShowResponse(response);
            var claims = GetUserClaims(response.AccessToken);
            var updatedTime = claims.FirstOrDefault(x => x.Type == "updated_at").Value;

            var dt = long.Parse(updatedTime).FromEpochTime();
            //Created as UTC but what's displaying is UTC + Offset
            Console.WriteLine("From Idsrv: {0} - {1}", dt.ToString("s"), dt.Kind);
        }

        public static void GetInfoDirect(string connectionString)
        {
            using (var db = new CustomDatabase(connectionString))
            {
                var svc = GetUserAccountService(db);
                var account = svc.GetByUsername("bob");
                if (account != null)
                {
                    //When fetching the account the first time, the lastUpdated DateTime Kind is reported as "Unspecified"
                    Console.WriteLine("From Local: {0} - {1}", account.LastUpdated.ToString("s"),
                        account.LastUpdated.Kind);

                    /* This can be used to confirm that as soon as Update() is called, the DateTime.Kind property is updated to Utc
                    svc.Update(account); 
                    Console.WriteLine("With Updte: {0} - {1}", account.LastUpdated.ToString("s"),
                        account.LastUpdated.Kind);
                     */
                }
            }
        }

        public static void GetInfoModifiedAll(string connectionString)
        {
            using (var db = new CustomDatabaseWithDtkFix(connectionString))
            {
                //It is expected that the .ToUnixTimeSeconds() will return the same value as IdSrv because it performs a UTC conversion before converting to seconds. 

                var svc = GetUserAccountServiceModifiedAll(db);
                var account = svc.GetByUsername("bob");
                if (account != null)
                {
                    //When fetching the account the first time, the lastUpdated DateTime Kind is reported as "Unspecified"
                    Console.WriteLine("From FixedAll: {0} - {1}", account.LastUpdated.ToString("s"),
                        account.LastUpdated.Kind);
                }
            }
        }

        public static void GetInfoModifiedAtr(string connectionString)
        {
            using (var db = new CustomDatabaseWithAttrFix(connectionString))
            {
                //It is expected that the .ToUnixTimeSeconds() will return the same value as IdSrv because it performs a UTC conversion before converting to seconds. 

                var svc = GetUserAccountServiceModifiedAtr(db);
                var account = svc.GetByUsername("bob");
                if (account != null)
                {
                    //When fetching the account the first time, the lastUpdated DateTime Kind is reported as "Unspecified"
                    Console.WriteLine("From FixedAtr: {0} - {1}", account.LastUpdated.ToString("s"),
                        account.LastUpdated.Kind);
                }
            }
        }

        private static CustomUserAccountService GetUserAccountService(CustomDatabase database)
        {
            var repo = new CustomUserAccountRepository(database);
            var svc = new CustomUserAccountService(CustomConfig.Config, repo);
            return svc;
        }

        private static CustomUserAccountServiceWithDtkFix GetUserAccountServiceModifiedAll(CustomDatabaseWithDtkFix database)
        {
            var repo = new CustomUserAccountRepositoryWithDtkFix(database);
            var svc = new CustomUserAccountServiceWithDtkFix(CustomConfigWithDtkFix.Config, repo);
            return svc;
        }

        private static CustomUserAccountServiceWithAttrFix GetUserAccountServiceModifiedAtr(CustomDatabaseWithAttrFix database)
        {
            var repo = new CustomUserAccountRepositoryWithAttrFix(database);
            var svc = new CustomUserAccountServiceWithAttrFix(CustomConfigWithAttrFix.Config, repo);
            return svc;
        }

        private static TokenResponse RequestToken()
        {
            var client = new TokenClient(
                Constants.TokenEndpoint,
                "roclient",
                "secret");

            return client.RequestResourceOwnerPasswordAsync("bob", "bob", "openid profile email").Result;
        }

        private static IEnumerable<Claim> GetUserClaims(string token)
        {
            var client = new UserInfoClient(
                new Uri(Constants.UserInfoEndpoint),
                token);
            var response = client.GetAsync().Result;
            var identity = response.GetClaimsIdentity();
            //PrintClaims(identity.Claims);
            return identity.Claims;
        }

        private static void ShowResponse(TokenResponse response)
        {
            if (!response.IsError)
            {
                "Token response:".ConsoleGreen();
                Console.WriteLine(response.Json);

                if (response.AccessToken.Contains("."))
                {
                    "\nAccess Token (decoded):".ConsoleGreen();

                    var parts = response.AccessToken.Split('.');
                    var header = parts[0];
                    var claims = parts[1];

                    Console.WriteLine(JObject.Parse(Encoding.UTF8.GetString(Base64Url.Decode(header))));
                    Console.WriteLine(JObject.Parse(Encoding.UTF8.GetString(Base64Url.Decode(claims))));
                }
            }
            else
            {
                if (response.IsHttpError)
                {
                    "HTTP error: ".ConsoleGreen();
                    Console.WriteLine(response.HttpErrorStatusCode);
                    "HTTP error reason: ".ConsoleGreen();
                    Console.WriteLine(response.HttpErrorReason);
                }
                else
                {
                    "Protocol error response:".ConsoleGreen();
                    Console.WriteLine(response.Json);
                }
            }
        }

        private static void PrintClaims(IEnumerable<Claim> claims)
        {
            "\n\nUser claims:".ConsoleGreen();
            foreach (var claim in claims)
            {
                Console.WriteLine("{0}\n {1}", claim.Type, claim.Value);
            }
        }
    }

    public static class Constants
    {
        public const string BaseAddress = "https://localhost:44333/core";
        public const string AuthorizeEndpoint = BaseAddress + "/connect/authorize";
        public const string LogoutEndpoint = BaseAddress + "/connect/endsession";
        public const string TokenEndpoint = BaseAddress + "/connect/token";
        public const string UserInfoEndpoint = BaseAddress + "/connect/userinfo";
        public const string IdentityTokenValidationEndpoint = BaseAddress + "/connect/identitytokenvalidation";
        public const string TokenRevocationEndpoint = BaseAddress + "/connect/revocation";
        public const string AspNetWebApiSampleApi = "http://localhost:2727/";

        public static DateTime FromEpochTime(this long epochTime)
        {
            return new DateTime(1970, 1, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochTime);
        }

    }
}