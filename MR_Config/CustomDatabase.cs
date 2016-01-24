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

using System.ComponentModel.DataAnnotations;
using BrockAllen.MembershipReboot;
using BrockAllen.MembershipReboot.Ef;
using BrockAllen.MembershipReboot.Relational;

namespace MR_Config
{
    public class CustomConfig : MembershipRebootConfiguration<CustomUserAccount>
    {
        public static readonly CustomConfig Config;

        static CustomConfig()
        {
            Config = new CustomConfig();
            Config.PasswordHashingIterationCount = 10000;
            Config.RequireAccountVerification = false;
        }
    }

    public class CustomUserAccount : RelationalUserAccount
    {
        [Display(Name = "First Name")]
        public virtual string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public virtual string LastName { get; set; }

        public virtual int? Age { get; set; }
    }

    public class CustomDatabase : MembershipRebootDbContext<CustomUserAccount>
    {
        public CustomDatabase(string name)
            : base(name)
        {
        }
    }

    public class CustomUserAccountRepository : DbContextUserAccountRepository<CustomDatabase, CustomUserAccount>
    {
        public CustomUserAccountRepository(CustomDatabase database)
            : base(database)
        {
        }
    }

    public class CustomUserAccountService : UserAccountService<CustomUserAccount>
    {
        public CustomUserAccountService(CustomConfig config, CustomUserAccountRepository repo)
            : base(config, repo)
        {
        }
    }
}