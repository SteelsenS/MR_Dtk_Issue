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
    public class CustomConfigWithAttrFix : MembershipRebootConfiguration<CustomUserAccountWithAttrFix>
    {
        public static readonly CustomConfigWithAttrFix Config;

        static CustomConfigWithAttrFix()
        {
            Config = new CustomConfigWithAttrFix();
            Config.PasswordHashingIterationCount = 10000;
            Config.RequireAccountVerification = false;
        }
    }

    public class CustomUserAccountWithAttrFix : RelationalUserAccount
    {
        [Display(Name = "First Name")]
        public virtual string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public virtual string LastName { get; set; }

        public virtual int? Age { get; set; }

        [DateTimeKind(DateTimeKind.Utc)]
        public override DateTime LastUpdated
        {
            get { return base.LastUpdated; }
            protected set { base.LastUpdated = value; }
        }
    }

    public class CustomDatabaseWithAttrFix : MembershipRebootDbContext<CustomUserAccountWithAttrFix>
    {
        public CustomDatabaseWithAttrFix(string name)
            : base(name)
        {
            ((IObjectContextAdapter)this).ObjectContext.ObjectMaterialized += (sender, e) => DateTimeKindAttribute.Apply(e.Entity);
        }
    }

    public class CustomUserAccountRepositoryWithAttrFix : DbContextUserAccountRepository<CustomDatabaseWithAttrFix, CustomUserAccountWithAttrFix>
    {
        public CustomUserAccountRepositoryWithAttrFix(CustomDatabaseWithAttrFix database)
            : base(database)
        {

        }
    }

    public class CustomUserAccountServiceWithAttrFix : UserAccountService<CustomUserAccountWithAttrFix>
    {
        public CustomUserAccountServiceWithAttrFix(CustomConfigWithAttrFix config, CustomUserAccountRepositoryWithAttrFix repo)
            : base(config, repo)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DateTimeKindAttribute : Attribute
    {
        private readonly DateTimeKind _kind;

        public DateTimeKindAttribute(DateTimeKind kind)
        {
            _kind = kind;
        }

        public DateTimeKind Kind
        {
            get { return _kind; }
        }

        public static void Apply(object entity)
        {
            if (entity == null)
                return;

            var properties = entity.GetType().GetProperties()
                .Where(x => x.PropertyType == typeof(DateTime) || x.PropertyType == typeof(DateTime?));

            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute<DateTimeKindAttribute>();
                if (attr == null)
                    continue;

                var dt = property.PropertyType == typeof(DateTime?)
                    ? (DateTime?)property.GetValue(entity)
                    : (DateTime)property.GetValue(entity);

                if (dt == null)
                    continue;

                property.SetValue(entity, DateTime.SpecifyKind(dt.Value, attr.Kind));
            }
        }
    }
}