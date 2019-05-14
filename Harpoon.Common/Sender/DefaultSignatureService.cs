using System;
using System.Security.Cryptography;
using System.Text;

namespace Harpoon.Sender
{
    /// <inheritdoc />
    public class DefaultSignatureService : ISignatureService
    {
        private static readonly char[] _hexLookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

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
                return ToHex(hasher.ComputeHash(data));
            }
        }

        private string ToHex(byte[] data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            var content = new char[data.Length * 2];
            var output = 0;
            byte d;
            for (var input = 0; input < data.Length; input++)
            {
                d = data[input];
                content[output++] = _hexLookup[d / 0x10];
                content[output++] = _hexLookup[d % 0x10];
            }
            return new string(content);
        }
    }
}