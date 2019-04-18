using System;
using System.Collections.Generic;

namespace Harpoon.Controllers.Models
{
    public class WebHookDTO : IWebHook
    {
        public Guid Id { get; set; }
        public Uri Callback { get; set; }
        public string Secret { get; set; }
        public bool IsPaused { get; set; }

        public List<WebHookFilterDTO> Filters { get; set; }

        IReadOnlyCollection<IWebHookFilter> IWebHook.Filters => Filters;
    }
}