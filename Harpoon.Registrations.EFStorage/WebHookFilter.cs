using System;
using System.Collections.Generic;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// Default implementation of <see cref="IWebHookFilter"/>
    /// </summary>
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
        public WebHookFilter()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
}
