using Microsoft.AspNetCore.DataProtection;
using System;
using System.Text;

namespace Harpoon.Registrations.EFStorage
{
    public class DefaultSecretProtector : ISecretProtector
    {
        public const string Purpose = "WebHookStorage";

        private readonly IDataProtector _dataProtector;

        public DefaultSecretProtector(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtector = dataProtectionProvider?.CreateProtector(Purpose) ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
        }

        public string Protect(string plaintext)
        {
            return _dataProtector.Protect(plaintext);
        }

        public string Unprotect(string protectedData)
        {
            try
            {
                return _dataProtector.Unprotect(protectedData);
            }
            catch
            {
                if (!(_dataProtector is IPersistedDataProtector persistedProtector))
                {
                    throw;
                }

                return Encoding.UTF8.GetString(persistedProtector.DangerousUnprotect(Encoding.UTF8.GetBytes(protectedData), true, out var _, out var _));
            }
        }
    }
}