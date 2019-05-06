using Harpoon.Registrations.EFStorage;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Harpoon.Tests.Mocks
{
    public class TestContext1 : TestContext
    {
        public TestContext1()
        {
        }

        public TestContext1(DbContextOptions options) : base(options)
        {
        }

        private static Guid _guid = Guid.NewGuid();
        protected override Guid DbName => _guid;
    }

    public class TestContext2 : TestContext
    {
        public TestContext2()
        {
        }

        public TestContext2(DbContextOptions options) : base(options)
        {
        }

        private static Guid _guid = Guid.NewGuid();
        protected override Guid DbName => _guid;
    }

    public abstract class TestContext : DbContext, IRegistrationsContext
    {
        public DbSet<WebHook> WebHooks { get; set; }
        IQueryable<WebHook> IRegistrationsContext.WebHooks => WebHooks;

        public DbSet<WebHookFilter> WebHookFilters { get; set; }

        protected abstract Guid DbName { get; }

        public TestContext()
        {
        }

        public TestContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer($@"Server=(localdb)\mssqllocaldb;Database=TEST_HARPOON_{DbName};Trusted_Connection=True;MultipleActiveResultSets=true");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddHarpoonDefaultMappings();
        }
    }
}