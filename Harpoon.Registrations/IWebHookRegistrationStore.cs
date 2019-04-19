using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Harpoon.Registrations
{
    public interface IWebHookRegistrationStore
    {
        Task<IWebHook> GetWebHookAsync(IPrincipal user, Guid id);
        Task<IReadOnlyList<IWebHook>> GetWebHooksAsync(IPrincipal user);

        Task<WebHookRegistrationStoreResult> InsertWebHookAsync(IPrincipal user, IWebHook webHook);
        Task<WebHookRegistrationStoreResult> UpdateWebHookAsync(IPrincipal user, IWebHook webHook);
        Task<WebHookRegistrationStoreResult> DeleteWebHookAsync(IPrincipal user, Guid id);

        Task DeleteWebHooksAsync(IPrincipal user);
    }
}