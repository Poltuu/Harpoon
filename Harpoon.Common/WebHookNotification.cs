namespace Harpoon
{
    /// <summary>
    /// Represents the content of an event that triggered
    /// </summary>
    public class WebHookNotification : IWebHookNotification
    {
        /// <inheritdoc />
        public string TriggerId { get; set; }

        /// <inheritdoc />
        public object Payload { get; set; }

        /// <summary>
        /// <summary>Initializes a new instance of the <see cref="WebHookNotification"/> class.</summary>
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="payload"></param>
        public WebHookNotification(string trigger, object payload)
        {
            TriggerId = trigger;
            Payload = payload;
        }
    }
}