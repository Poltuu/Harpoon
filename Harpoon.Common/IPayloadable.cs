using System;

namespace Harpoon
{
    /// <summary>
    /// Represents a class that can be used as a payload.
    /// This class will be serialized as-is using the default json serialization settings
    /// </summary>
    public interface IPayloadable
    {
        /// <summary>
        /// Gets or sets an id of the notification owning this payload. This is important for security reasons.
        /// </summary>
        Guid NotificationId { get; set; }
    }
}
