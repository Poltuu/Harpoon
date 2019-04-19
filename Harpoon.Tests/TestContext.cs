using Harpoon.Registrations.EFStorage;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Harpoon.Tests
{
    public class TestContext : DbContext, IRegistrationsContext
    {
        public DbSet<Registration> Registrations { get; set; }
        IQueryable<Registration> IRegistrationsContext.Registrations => Registrations;

        public DbSet<WebHook> WebHooks { get; set; }
        public DbSet<WebHookFilter> WebHookFilters { get; set; }
    }
}