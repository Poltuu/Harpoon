using Microsoft.OpenApi.Models;

namespace Harpoon.Registrations
{
    /// <summary>
    /// Represents an event template that may trigger a webhook
    /// </summary>
    public class WebHookTrigger
    {
        /// <summary>
        /// Gets or sets a unique id for the event. This could typically look like `noun.verb`.
        /// If pattern matching is used, this could look like this `noun.verb.{value}`
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets a short description of the event
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the template for payloads for this trigger
        /// </summary>
        public OpenApiSchema Template { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookTrigger"/> class.
        /// </summary>
        public WebHookTrigger()
        {
            Template = new OpenApiSchema { Type = "object" };
        }
    }
}
