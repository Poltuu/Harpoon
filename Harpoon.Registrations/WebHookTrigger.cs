using System;

namespace Harpoon.Registrations
{
    /// <summary>
    /// Represents an strongly-typed event template that may trigger a webhook
    /// </summary>
    public class WebHookTrigger<TPayload> : WebHookTrigger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookTrigger{TPayload}"/> class.
        /// </summary>
        public WebHookTrigger(string id, string description)
            : base(id, description, typeof(TPayload))
        {
        }
    }

    /// <summary>
    /// Represents an event template that may trigger a webhook
    /// </summary>
    public class WebHookTrigger
    {
        /// <summary>
        /// Gets or sets a unique id for the event. This could typically look like `noun.verb`.
        /// If pattern matching is used, this could look like this `noun.verb.{value}`
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets or sets a short description of the event
        /// </summary>
        public string Description { get; set; }

        private readonly Type _payloadType;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookTrigger"/> class.
        /// </summary>
        public WebHookTrigger(string id, string description, Type payloadType)
        {
            Id = id;
            Description = description;
            _payloadType = payloadType;
        }

        /// <summary>
        /// Gets the payload type for this trigger
        /// </summary>
        public Type GetPayloadType() => _payloadType;
    }
}