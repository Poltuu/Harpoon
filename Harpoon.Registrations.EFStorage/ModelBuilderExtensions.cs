using Harpoon.Registrations.EFStorage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore
{
    public static class ModelBuilderExtensions
    {
        public static void AddHarpoonDefaultMappings(this ModelBuilder modelBuilder)
        {
            modelBuilder.AddWebHookDefaultMapping();
            modelBuilder.AddWebHookFilterDefaultMapping();
        }

        public static void AddWebHookDefaultMapping(this ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<WebHook>().Ignore(w => w.Callback).Ignore(w => w.Secret);
            modelBuilder.Entity<WebHook>().Property(r => r.PrincipalId).IsRequired();
            modelBuilder.Entity<WebHook>().Property(w => w.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<WebHook>().Property(w => w.ProtectedCallback).IsRequired();
            modelBuilder.Entity<WebHook>().Property(w => w.ProtectedSecret).IsRequired();
            modelBuilder.Entity<WebHook>().HasMany(w => w.Filters).WithOne().IsRequired().OnDelete(DeleteBehavior.Cascade);
        }

        public static void AddWebHookFilterDefaultMapping(this ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<WebHookFilter>().Property(f => f.ActionId).IsRequired();
            modelBuilder.Entity<WebHookFilter>().Property(f => f.Parameters).HasConversion(new JsonValueConverter<Dictionary<string, object>>());
        }

        public class JsonValueConverter<T> : ValueConverter<T, string>
        {
            public JsonValueConverter()
                : base(d => JsonConvert.SerializeObject(d), s => JsonConvert.DeserializeObject<T>(s))
            {
            }
        }
    }
}
