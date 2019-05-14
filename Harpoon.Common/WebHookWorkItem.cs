using System;

namespace Harpoon
{
    /// <inheritdoc />
    public class WebHookWorkItem : IWebHookWorkItem
    {
        /// <inheritdoc />
        public Guid Id { get; }
        /// <inheritdoc />
        public DateTime Timestamp { get; }
        /// <inheritdoc />
        public IWebHookNotification Notification { get; }
        /// <inheritdoc />
        public IWebHook WebHook { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookWorkItem"/> class.
        /// </summary>
        public WebHookWorkItem(IWebHookNotification notification, IWebHook webHook)
        {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
            WebHook = webHook ?? throw new ArgumentNullException(nameof(webHook));
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
}