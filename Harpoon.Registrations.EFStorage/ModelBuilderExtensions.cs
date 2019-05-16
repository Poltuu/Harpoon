using Harpoon.Registrations.EFStorage;
using System;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// A set of extensions methods on <see cref="ModelBuilder"/>
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Uses default mappings the build to webhook part of your model
        /// </summary>
        /// <param name="modelBuilder"></param>
        public static void AddHarpoonDefaultMappings(this ModelBuilder modelBuilder)
        {
            modelBuilder.AddWebHookDefaultMapping();
            modelBuilder.AddWebHookFilterDefaultMapping();
        }

        /// <summary>
        /// Uses default mappings for the <see cref="WebHook"/> class to build the webhook part of your model
        /// </summary>
        /// <param name="modelBuilder"></param>
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

        /// <summary>
        /// Uses default mappings for the <see cref="WebHookFilter"/> class to build the webhook part of your model
        /// </summary>
        /// <param name="modelBuilder"></param>
        public static void AddWebHookFilterDefaultMapping(this ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<WebHookFilter>().Property(f => f.Trigger).IsRequired();
            modelBuilder.Entity<WebHookFilter>().Property(f => f.Parameters).HasConversion(new JsonValueConverter<Dictionary<string, object>>());
        }
    }
}
