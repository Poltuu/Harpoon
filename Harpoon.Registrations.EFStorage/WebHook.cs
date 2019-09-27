using System;
using System.Collections.Generic;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// Default <see cref="IWebHook"/> implementation
    /// </summary>
    public class WebHook : IWebHook
    {
        /// <inheritdoc />
        public Guid Id { get; set; }

        /// <inheritdoc />
        Uri IWebHook.Callback => new Uri(Callback);

        /// <inheritdoc />
        public string Callback { get; set; }

        /// <inheritdoc />
        public string Secret { get; set; }
        /// <summary>
        /// Gets or sets protected secret
        /// </summary>
        public string ProtectedSecret { get; set; }

        /// <inheritdoc />
        public string PrincipalId { get; set; }

        /// <inheritdoc />
        public bool IsPaused { get; set; }

        /// <summary>
        /// Gets or sets the associated collection of <see cref="WebHookFilter"/>
        /// </summary>
        public List<WebHookFilter> Filters { get; set; }

        IReadOnlyCollection<IWebHookFilter> IWebHook.Filters => Filters;

        /// <summary>Initializes a new instance of the <see cref="WebHook"/> class.</summary>
        public WebHook()
        {
            Filters = new List<WebHookFilter>();
        }
    }
}
