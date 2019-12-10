using System;
using System.Collections.Generic;
using System.Linq;

namespace Harpoon.Controllers.Models
{
    /// <summary>
    /// Represents a webhook, i.e. a subscription to certain internal events that need to translate to url callbacks
    /// </summary>
    public class WebHook : IWebHook
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
        public List<WebHookFilter> Filters { get; set; }

        IReadOnlyCollection<IWebHookFilter> IWebHook.Filters => Filters;

        /// <summary>Initializes a new instance of the <see cref="WebHook"/> class.</summary>
        public WebHook() { }
        /// <summary>Initializes a new instance of the <see cref="WebHook"/> class.</summary>
        public WebHook(IWebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            Id = webHook.Id;
            Callback = webHook.Callback;
            Secret = webHook.Secret;
            IsPaused = webHook.IsPaused;

            Filters = webHook.Filters?.Select(f => new WebHookFilter(f)).ToList();
        }
    }
}