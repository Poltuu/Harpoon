using System;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// Default implementation of <see cref="IWebHookFilter"/>
    /// </summary>
    public class WebHookFilter : IWebHookFilter
    {
        /// <inheritdoc />
        public Guid Id { get; set; }

        /// <inheritdoc />
        public string Trigger { get; set; }
    }
}
