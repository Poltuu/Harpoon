using System;

namespace Harpoon.Sender
{
    internal static class EncodingUtilities
    {
        private static readonly char[] HexLookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string ToHex(byte[] data)
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

        public static byte[] FromHex(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return new byte[0];
            }

            try
            {
                var data = new byte[content.Length / 2];
                var input = 0;
                while (input < content.Length)
                {
                    data[input / 2] = Convert.ToByte(new string(new [] { content[input++], content[input++] }), 16);
                }

                return data;
            }
            catch
            {
                throw new InvalidOperationException("Invalid content : " + content);
            }
        }
    }
}
