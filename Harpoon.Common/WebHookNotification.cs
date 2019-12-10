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
    }
}
