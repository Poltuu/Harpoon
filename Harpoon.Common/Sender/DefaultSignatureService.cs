using System;
using System.Security.Cryptography;
using System.Text;

namespace Harpoon.Sender
{
    /// <summary>
    /// A class able to assign a signature to content using a secret
    /// </summary>
    public class DefaultSignatureService : ISignatureService
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentException">The provider secret is not a 64 characters string</exception>
        public string GetSignature(string secret, string content)
        {
            if (secret?.Length != 64)
            {
                throw new ArgumentException("Secret needs to be a 64 characters string.");
            }

            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var data = Encoding.UTF8.GetBytes(content ?? "");

            using (var hasher = new HMACSHA256(secretBytes))
            {
                return Convert.ToBase64String(hasher.ComputeHash(data));
            }
        }
    }
}