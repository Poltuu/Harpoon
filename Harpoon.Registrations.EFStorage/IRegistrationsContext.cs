using System.Linq;

namespace Harpoon.Registrations.EFStorage
{
    public interface IRegistrationsContext
    {
        IQueryable<Registration> Registrations { get;}
    }
}
