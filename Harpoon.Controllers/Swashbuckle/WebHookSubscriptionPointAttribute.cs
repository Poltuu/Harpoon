using System;

namespace Harpoon.Controllers.Swashbuckle
{
    /// <summary>
    /// This attributes informs Swashbuckle to generate a callback node on this api, matching the harpoon configuration
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class WebHookSubscriptionPointAttribute : Attribute { }
}
