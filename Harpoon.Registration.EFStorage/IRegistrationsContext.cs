using System.Linq;

namespace Harpoon.Registration.EFStorage
{
    public interface IRegistrationsContext
    {
        IQueryable<Registration> Registrations { get;}
    }
}
