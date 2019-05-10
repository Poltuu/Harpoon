using System;

namespace Harpoon
{
    public class WebHookWorkItem : IWebHookWorkItem
    {
        public Guid Id { get; }
        public DateTime Timestamp { get; }
        public IWebHookNotification Notification { get; }
        public IWebHook WebHook { get; }

        public WebHookWorkItem(IWebHookNotification notification, IWebHook webHook)
        {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
            WebHook = webHook ?? throw new ArgumentNullException(nameof(webHook));
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
}