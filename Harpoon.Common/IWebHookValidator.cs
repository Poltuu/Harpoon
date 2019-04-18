using System.Threading.Tasks;

namespace Harpoon
{
    public interface IWebHookValidator
    {
        Task ValidateAsync(IWebHook webHook);
    }
}