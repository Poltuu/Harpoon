using Harpoon.Registrations.EFStorage;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Harpoon.Tests
{
    public class TestContext : DbContext, IRegistrationsContext
    {
        public DbSet<WebHook> WebHooks { get; set; }
        IQueryable<WebHook> IRegistrationsContext.WebHooks => WebHooks;

        public DbSet<WebHookFilter> WebHookFilters { get; set; }

        public TestContext()
        {
        }

        public TestContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=TEST_HARPOON;Trusted_Connection=True;MultipleActiveResultSets=true");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddHarpoonDefaultMappings();
        }
    }
}