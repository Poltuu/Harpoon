using System.Linq;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// Represents a DbContext able to expose WebHooks
    /// </summary>
    public interface IRegistrationsContext
    {
        /// <summary>
        /// Gets <see cref="WebHook"/> queryable entry point
        /// </summary>
        IQueryable<WebHook> WebHooks { get; }

        /// <summary>
        /// Gets <see cref="WebHookNotification"/> queryable entry point
        /// </summary>
        IQueryable<WebHookNotification> WebHookNotifications { get; }

        /// <summary>
        /// Gets <see cref="WebHookLog"/> queryable entry point
        /// </summary>
        IQueryable<WebHookLog> WebHookLogs { get; }
    }
}