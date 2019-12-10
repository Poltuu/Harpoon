using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Registrations
{
    /// <summary>
    /// Represents a class able to perform CRUD operations on <see cref="IWebHook"/>
    /// </summary>
    public interface IWebHookRegistrationStore
    {
        /// <summary>
        /// Returns asynchronously the <see cref="IWebHook"/> owned by the provided <see cref="IPrincipal"/> with the given <see cref="Guid"/> id
        /// </summary>
        /// <param name="user"></param>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IWebHook> GetWebHookAsync(IPrincipal user, Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns asynchronously a collection of <see cref="IWebHook"/> owned by the provided <see cref="IPrincipal"/>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IReadOnlyList<IWebHook>> GetWebHooksAsync(IPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts asynchronously the provided <see cref="IWebHook"/> owned by the provided <see cref="IPrincipal"/>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="webHook"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<WebHookRegistrationStoreResult> InsertWebHookAsync(IPrincipal user, IWebHook webHook, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates asynchronously the provided <see cref="IWebHook"/> owned by the provided <see cref="IPrincipal"/>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="webHook"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<WebHookRegistrationStoreResult> UpdateWebHookAsync(IPrincipal user, IWebHook webHook, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes asynchronously the <see cref="IWebHook"/> owned by the provided <see cref="IPrincipal"/> with the given <see cref="Guid"/> id
        /// </summary>
        /// <param name="user"></param>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<WebHookRegistrationStoreResult> DeleteWebHookAsync(IPrincipal user, Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes asynchronously the ensemble of <see cref="IWebHook"/> owned by the provided <see cref="IPrincipal"/>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteWebHooksAsync(IPrincipal user, CancellationToken cancellationToken = default);
    }
}