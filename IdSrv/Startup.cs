/*
 * Copyright 2014 Dominick Baier, Brock Allen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using IdentityServer3.Core.Configuration;
using IdSrv.IdSvr;
using MR_Config;
using Owin;

namespace IdSrv
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var connectionString = "MembershipReboot";

            var idSvrFactory = Factory.Configure();
            idSvrFactory.ConfigureCustomUserService(connectionString);

            var options = new IdentityServerOptions
            {
                SiteName = "IdentityServer3 - UserService-MembershipReboot",
                SigningCertificate = Certificate.Get(),
                Factory = idSvrFactory
            };

            app.UseIdentityServer(options);


            NewUsers(connectionString);
        }

        public static void NewUsers(string connectionString)
        {
            var us = new CustomUserAccountService(CustomConfig.Config,
                new CustomUserAccountRepository(new CustomDatabase(connectionString)));
            var userBob = us.GetByUsername("bob");
            if (userBob == null)
            {
                var newUserBob = us.CreateAccount("bob", "bob", "BobSmith@email.com");
                us.AddClaim(newUserBob.ID, "GivenName", "Bob");
                us.AddClaim(newUserBob.ID, "FamilyName", "Smith");
            }
        }
    }
}