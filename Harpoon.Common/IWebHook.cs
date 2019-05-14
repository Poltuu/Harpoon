using System;
using System.Collections.Generic;

namespace Harpoon
{
    /// <summary>
    /// Represents a webhook, i.e. a subscription to certain internal events that need to translate to url callbacks
    /// </summary>
    public interface IWebHook
    {
        /// <summary>
        /// Gets or sets the <see cref="IWebHook"/> unique identifier.
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// Gets the url to be called in case an event matching the <see cref="Filters"/> is triggered
        /// </summary>
        Uri Callback { get; }

        /// <summary>
        /// Gets or sets the <see cref="IWebHook"/> shared secret. The secret must be 64 character in default implementation.
        /// </summary>
        string Secret { get; set; }

        /// <summary>
        /// Gets a value indicating if the given is currently paused.
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Gets a collection of <see cref="IWebHookFilter"/> describing what should trigger the given <see cref="IWebHook"/>
        /// </summary>
        IReadOnlyCollection<IWebHookFilter> Filters { get; }
    }
}