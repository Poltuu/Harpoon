using System;
using System.Collections.Generic;

namespace Harpoon
{
    /// <summary>
    /// Represents a filter on triggered events, i.e. an event type to listen to
    /// </summary>
    public interface IWebHookFilter
    {
        /// <summary>
        /// Gets or sets the <see cref="IWebHookFilter"/> unique identifier.
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// Gets a unique name for listened event. Depending on the implementation, pattern matching can be used.
        /// For instance, '*.created' could refer to any event similar to the pattern. 
        /// </summary>
        string Trigger { get; }

        /// <summary>
        /// Gets a collection of filters on the payload.
        /// This offers a second layer of filter
        /// </summary>
        IReadOnlyDictionary<string, object> Parameters { get; }
    }
}
