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
            Timestamp = DateTime.UtcNow;

            Id = Guid.NewGuid();
            if (notification.Payload != null && notification.Payload.TryGetValue("NotificationId", out var value))
            {
                if (value is Guid id)
                {
                    Id = id;
                }
                else if (value is string text && Guid.TryParse(text, out var realId))
                {
                    Id = realId;
                }
            }
        }
    }
}