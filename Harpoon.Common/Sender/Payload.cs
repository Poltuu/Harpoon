using System;

namespace Harpoon.Sender
{
    /// <summary>
    /// A wrapper class that contains th payload and a unique id
    /// </summary>
    public class Payload
    {
        /// <summary>
        /// Gets or set the unique id of the content
        /// </summary>
        public Guid NotificationId { get; set; }

        /// <summary>
        /// Gets or set the content of the initial payload
        /// </summary>
        public object Content { get; set; }
    }
}