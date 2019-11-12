using System;
using System.Collections.Generic;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// Represents the content of an event that triggered
    /// </summary>
    public class WebHookNotification : IWebHookNotification
    {
        /// <summary>
        /// Gets or sets the <see cref="IWebHook"/> id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the time stamp when the notification was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <inheritdoc />
        public string TriggerId { get; set; }

        /// <inheritdoc />
        public object Payload { get; set; }

        /// <summary>
        /// Gets or sets the number of applicable webhooks
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the associated collection of <see cref="WebHookLog"/>
        /// </summary>
        public List<WebHookLog> WebHookLogs { get; set; }

        /// <summary>Initializes a new instance of the <see cref="WebHookNotification"/> class.</summary>
        public WebHookNotification()
        {
            Id = Guid.NewGuid();
            WebHookLogs = new List<WebHookLog>();
            CreatedAt = DateTime.UtcNow;
        }
    }
}