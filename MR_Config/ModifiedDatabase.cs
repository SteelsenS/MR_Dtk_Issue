using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using BrockAllen.MembershipReboot;
using BrockAllen.MembershipReboot.Ef;
using BrockAllen.MembershipReboot.Relational;


namespace MR_Config
{
    public class CustomConfigWithDtkFix : MembershipRebootConfiguration<CustomUserAccountWithDtkFix>
    {
        public static readonly CustomConfigWithDtkFix Config;

        static CustomConfigWithDtkFix()
        {
            Config = new CustomConfigWithDtkFix();
            Config.PasswordHashingIterationCount = 10000;
            Config.RequireAccountVerification = false;
        }
    }

    public class CustomUserAccountWithDtkFix : RelationalUserAccount
    {
        [Display(Name = "First Name")]
        public virtual string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public virtual string LastName { get; set; }

        public virtual int? Age { get; set; }

    }

    public class CustomDatabaseWithDtkFix : MembershipRebootDbContext<CustomUserAccountWithDtkFix>
    {
        public CustomDatabaseWithDtkFix(string name)
            : base(name)
        {
            ((IObjectContextAdapter)this).ObjectContext.ObjectMaterialized += ReadAllDateTimeValuesAsUtc;
        }

        private static void ReadAllDateTimeValuesAsUtc(object sender, ObjectMaterializedEventArgs e)
        {
            //Extract all DateTime properties of the object type
            var properties = e.Entity.GetType().GetProperties()
                .Where(property => property.PropertyType == typeof(DateTime) ||
                                   property.PropertyType == typeof(DateTime?)).ToList();
            //Set all DaetTimeKinds to Utc
            properties.ForEach(property => SpecifyUtcKind(property, e.Entity));
        }

        private static void SpecifyUtcKind(PropertyInfo property, object value)
        {
            //Get the datetime value
            var datetime = property.GetValue(value, null);

            //set DateTimeKind to Utc
            if (property.PropertyType == typeof(DateTime))
            {
                datetime = DateTime.SpecifyKind((DateTime)datetime, DateTimeKind.Utc);
            }
            else if (property.PropertyType == typeof(DateTime?))
            {
                var nullable = (DateTime?)datetime;
                if (!nullable.HasValue) return;
                datetime = (DateTime?)DateTime.SpecifyKind(nullable.Value, DateTimeKind.Utc);
            }
            else
            {
                return;
            }

            //And set the Utc DateTime value
            property.SetValue(value, datetime, null);
        }
    }

    public class CustomUserAccountRepositoryWithDtkFix : DbContextUserAccountRepository<CustomDatabaseWithDtkFix, CustomUserAccountWithDtkFix>
    {
        public CustomUserAccountRepositoryWithDtkFix(CustomDatabaseWithDtkFix database)
            : base(database)
        {

        }
    }

    public class CustomUserAccountServiceWithDtkFix : UserAccountService<CustomUserAccountWithDtkFix>
    {
        public CustomUserAccountServiceWithDtkFix(CustomConfigWithDtkFix config, CustomUserAccountRepositoryWithDtkFix repo)
            : base(config, repo)
        {
        }
    }
}