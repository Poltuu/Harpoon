using System;
using System.ComponentModel.DataAnnotations;

namespace Harpoon.Registrations.EFStorage
{
    public class Registration
    {
        public Guid Id { get; set; }

        [Required]
        public string PrincipalId { get; set; }

        public Guid WebHookId { get; set; }
        public WebHook WebHook { get; set; }
    }
}
