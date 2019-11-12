using Harpoon.Registrations.EFStorage;
using Newtonsoft.Json;
using System;

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
            modelBuilder.AddWebHookLogDefaultMapping();
            modelBuilder.AddWebHookNotificationDefaultMapping();
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

            modelBuilder.Entity<WebHook>().Ignore(w => w.Secret);
            modelBuilder.Entity<WebHook>().Property(r => r.PrincipalId).IsRequired();
            modelBuilder.Entity<WebHook>().Property(w => w.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<WebHook>().Property(w => w.Callback).IsRequired();
            modelBuilder.Entity<WebHook>().Property(w => w.ProtectedSecret).IsRequired();
            modelBuilder.Entity<WebHook>().HasMany(w => w.Filters).WithOne().IsRequired().OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WebHook>().HasMany(w => w.WebHookLogs).WithOne(l => l.WebHook).HasForeignKey(l => l.WebHookId).OnDelete(DeleteBehavior.Cascade);
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

            modelBuilder.Entity<WebHookFilter>().ToTable("WebHookFilters");
            modelBuilder.Entity<WebHookFilter>().Property(f => f.Trigger).IsRequired();
        }

        /// <summary>
        /// Uses default mappings for the <see cref="WebHookLog"/> class to build the webhook part of your model
        /// </summary>
        /// <param name="modelBuilder"></param>
        public static void AddWebHookLogDefaultMapping(this ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<WebHookLog>().HasOne(w => w.WebHook).WithMany(w => w.WebHookLogs).HasForeignKey(l => l.WebHookId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WebHookLog>().HasOne(w => w.WebHookNotification).WithMany(l => l.WebHookLogs).HasForeignKey(l => l.WebHookNotificationId).OnDelete(DeleteBehavior.Cascade);
        }

        /// <summary>
        /// Uses default mappings for the <see cref="WebHookNotification"/> class to build the webhook part of your model
        /// </summary>
        /// <param name="modelBuilder"></param>
        public static void AddWebHookNotificationDefaultMapping(this ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<WebHookNotification>().Property(w => w.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<WebHookNotification>().Property(f => f.TriggerId).IsRequired().HasMaxLength(500);
            modelBuilder.Entity<WebHookNotification>().Property(n => n.Payload).IsRequired().HasConversion(v => JsonConvert.SerializeObject(v), v => JsonConvert.DeserializeObject<object>(v));
        }
    }
}