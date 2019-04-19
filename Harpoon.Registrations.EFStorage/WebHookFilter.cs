using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Harpoon.Registrations.EFStorage
{
    public class WebHookFilter : IWebHookFilter
    {
        public Guid Id { get; set; }

        [Required]
        public string ActionId { get; set; }

        public string ParametersJson { get; set; }
        [NotMapped]
        public Dictionary<string, object> Parameters { get; set; }

        IReadOnlyDictionary<string, object> IWebHookFilter.Parameters => Parameters;
    }
}
