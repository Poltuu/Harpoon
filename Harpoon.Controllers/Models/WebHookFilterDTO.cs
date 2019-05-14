using System.Collections.Generic;

namespace Harpoon.Controllers.Models
{
    /// <inheritdoc />
    public class WebHookFilterDTO : IWebHookFilter
    {
        /// <inheritdoc />
        public string TriggerId { get; set; }
        /// <inheritdoc />
        public Dictionary<string, object> Parameters { get; set; }

        IReadOnlyDictionary<string, object> IWebHookFilter.Parameters => Parameters;
    }
}