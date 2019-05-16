namespace Harpoon
{
    /// <inheritdoc />
    public class WebHookNotification : IWebHookNotification
    {
        /// <inheritdoc />
        public string TriggerId { get; set; }

        /// <inheritdoc />
        public object Payload { get; set; }
    }
}
