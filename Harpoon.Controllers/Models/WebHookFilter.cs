using System;
using System.Collections.Generic;
using System.Linq;

namespace Harpoon.Controllers.Models
{
    /// <inheritdoc />
    public class WebHookFilter : IWebHookFilter
    {
        /// <inheritdoc />
        public Guid Id { get; set; }
        /// <inheritdoc />
        public string Trigger { get; set; }
        /// <inheritdoc />
        public Dictionary<string, object> Parameters { get; set; }

        IReadOnlyDictionary<string, object> IWebHookFilter.Parameters => Parameters;

        /// <summary>Initializes a new instance of the <see cref="WebHookFilter"/> class.</summary>
        public WebHookFilter() { }
        /// <summary>Initializes a new instance of the <see cref="WebHookFilter"/> class.</summary>
        public WebHookFilter(IWebHookFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            Id = filter.Id;
            Trigger = filter.Trigger;
            Parameters = filter.Parameters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}