using System;

namespace Harpoon
{
    public interface IWebHookWorkItem
    {
        Guid Id { get; }
        DateTime Timestamp { get; }
        IWebHookNotification Notification { get; }
        IWebHook WebHook { get; }
    }
}