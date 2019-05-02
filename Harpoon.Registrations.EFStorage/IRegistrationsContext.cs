using System.Linq;

namespace Harpoon.Registrations.EFStorage
{
    public interface IRegistrationsContext
    {
        IQueryable<WebHook> WebHooks { get;}
    }
}
