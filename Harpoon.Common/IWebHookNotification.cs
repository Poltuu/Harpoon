namespace Harpoon
{
    /// <summary>
    /// Represents the content of an event that triggered
    /// </summary>
    public interface IWebHookNotification
    {
        /// <summary>
        /// Gets the name of the event.
        /// If pattern matching is used, this may contain parameters i.e. `client.23.creation`
        /// </summary>
        string TriggerId { get; }

        /// <summary>
        /// Gets an serializable object representing the payload to be sent to the registered webhooks
        /// This is exactly serialized into the send payload
        /// </summary>
        object Payload { get; }
    }
}
