using System;
using System.Collections.Generic;

namespace Harpoon.Controllers.Models
{
    /// <inheritdoc />
    public class WebHookDTO : IWebHook
    {
        /// <inheritdoc />
        public Guid Id { get; set; }
        /// <inheritdoc />
        public Uri Callback { get; set; }
        /// <inheritdoc />
        public string Secret { get; set; }
        /// <inheritdoc />
        public bool IsPaused { get; set; }

        /// <inheritdoc />
        public List<WebHookFilterDTO> Filters { get; set; }

        IReadOnlyCollection<IWebHookFilter> IWebHook.Filters => Filters;
    }
}