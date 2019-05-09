using System;
using System.Collections.Generic;

namespace Harpoon.Registrations.EFStorage
{
    public class WebHookFilter : IWebHookFilter
    {
        public Guid Id { get; set; }

        public string TriggerId { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        IReadOnlyDictionary<string, object> IWebHookFilter.Parameters => Parameters;

        public WebHookFilter()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
}
