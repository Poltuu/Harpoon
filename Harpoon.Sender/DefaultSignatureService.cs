using System;
using System.Security.Cryptography;
using System.Text;

namespace Harpoon.Sender
{
    public class DefaultSignatureService : ISignatureService
    {
        protected readonly char[] HexLookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

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

        protected string ToHex(byte[] data)
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
                content[output++] = HexLookup[d / 0x10];
                content[output++] = HexLookup[d % 0x10];
            }
            return new string(content);
        }
    }
}