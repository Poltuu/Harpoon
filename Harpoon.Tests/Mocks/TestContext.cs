using Harpoon.Registrations.EFStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Linq;

namespace Harpoon.Tests.Mocks
{
    public class InMemoryContext : TestContext
    {
        public InMemoryContext()
        {
        }

        public InMemoryContext(DbContextOptions options) : base(options)
        {
        }

        protected override Guid DbName => Guid.NewGuid();//not used

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        }
    }
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
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));

            var connectionString = Environment.GetEnvironmentVariable("Harpoon_Connection_String");
            if (connectionString == null)
            {
                optionsBuilder.UseSqlServer($@"Server=(localdb)\mssqllocaldb;Database=TEST_HARPOON_{DbName};Trusted_Connection=True;MultipleActiveResultSets=true");
            }
            else
            {
                optionsBuilder.UseSqlServer(string.Format(connectionString, DbName));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddHarpoonDefaultMappings();
        }
    }
}