using System;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// Represents a log for a callback call
    /// </summary>
    public class WebHookLog
    {
        /// <summary>
        /// Gets or sets the <see cref="IWebHook"/> id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the time stamp when the treatment started
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets an error message
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets a value representing if the log is in error
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(Error);

        /// <summary>
        /// Gets or sets the associated <see cref="WebHookNotification"/> id
        /// </summary>
        public Guid WebHookNotificationId { get; set; }
        /// <summary>
        /// Gets or sets the associated <see cref="WebHookNotification"/>
        /// </summary>
        public WebHookNotification WebHookNotification { get; set; }

        /// <summary>
        /// Gets or sets the associated <see cref="WebHook"/> id
        /// </summary>
        public Guid WebHookId { get; set; }
        /// <summary>
        /// Gets or sets the associated <see cref="WebHook"/>
        /// </summary>
        public WebHook WebHook { get; set; }

        /// <summary>Initializes a new instance of the <see cref="WebHookLog"/> class.</summary>
        public WebHookLog()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
}