using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harpoon.Registration
{
    public class WebHookRegistration
    {
        public Guid Id { get; set; }

        public object PrincipalId { get; set; }//what ?

        public Guid WebHookId { get; set; }
        public WebHook WebHook { get; set; }
    }

    public class WebHook : IWebHook
    {
        public Guid Id { get; set; }
        public Uri Callback { get; set; }

        public string Secret { get; set; }
        public bool IsPaused { get; set; }

        public List<WebHookFilter> Filters { get; set; }

        IReadOnlyCollection<IWebHookFilter> IWebHook.Filters => Filters;
    }

    public class WebHookAction
    {
        public string Id { get; set; }
        public string Description { get; set; }

        public HashSet<string> AvailableParameters { get; set; }
    }

    public class WebHookNotification : IWebHookNotification
    {
        public string ActionId { get; set; }
        public WebHookAction Action { get; set; }

        public Dictionary<string, object> Payload { get; set; }
        IReadOnlyDictionary<string, object> IWebHookNotification.Payload => Payload;
    }

    public class WebHookFilter : IWebHookFilter
    {
        public string ActionId { get; set; }
        public WebHookAction Action { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        IReadOnlyDictionary<string, object> IWebHookFilter.Parameters => Parameters;
    }

    public interface IWebHookRegistrationStore
    {
        Task<WebHook> GetCurrentUserWebHookAsync(Guid id);
        Task<List<WebHook>> GetCurrentUserWebHooksAsync();

        Task<WebHookStoreResult> InsertWebHookAsync(WebHook webHook);
        Task<WebHookStoreResult> UpdateWebHookAsync(WebHook webHook);
        Task<WebHookStoreResult> DeleteWebHookAsync(Guid id);
        Task DeleteCurrentUserWebHooksAsync();
    }

    public enum WebHookStoreResult
    {
        Success,
        NotFound,
        InternalError
    }
}
