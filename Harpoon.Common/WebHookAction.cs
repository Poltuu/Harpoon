using System.Collections.Generic;

namespace Harpoon
{
    public class WebHookAction
    {
        public string Id { get; set; }
        public string Description { get; set; }

        public HashSet<string> AvailableParameters { get; set; }

        public WebHookAction()
        {
            AvailableParameters = new HashSet<string>();
        }
    }
}
