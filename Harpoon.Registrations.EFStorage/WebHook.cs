using System;
using System.Collections.Generic;

namespace Harpoon.Registrations.EFStorage
{
    public class WebHook : IWebHook
    {
        public Guid Id { get; set; }

        public Uri Callback { get; set; }
        public string ProtectedCallback { get; set; }

        public string Secret { get; set; }
        public string ProtectedSecret { get; set; }

        public string PrincipalId { get; set; }

        public bool IsPaused { get; set; }

        public List<WebHookFilter> Filters { get; set; }

        IReadOnlyCollection<IWebHookFilter> IWebHook.Filters => Filters;

        public WebHook()
        {
            Filters = new List<WebHookFilter>();
        }
    }
}
