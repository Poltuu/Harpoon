using System;
using System.Collections.Generic;

namespace Harpoon
{
    public class WebHookTrigger
    {
        /// <summary>
        /// Typically noun.verb
        /// </summary>
        public string Id { get; set; }
        public string Description { get; set; }

        public Dictionary<string, Type> Template { get; set; }

        public WebHookTrigger()
        {
            Template = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
