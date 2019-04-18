using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Harpoon.Registration.EFStorage
{
    public class WebHook : IWebHook
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        [NotMapped]
        public Uri Callback { get; set; }
        public string ProtectedCallback { get; set; }

        [NotMapped]
        public string Secret { get; set; }
        public string ProtectedSecret { get; set; }

        public bool IsPaused { get; set; }

        public List<WebHookFilter> Filters { get; set; }

        IReadOnlyCollection<IWebHookFilter> IWebHook.Filters => Filters;
    }
}
