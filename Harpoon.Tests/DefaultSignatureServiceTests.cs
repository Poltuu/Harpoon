using Harpoon.Sender;
using System;
using Xunit;

namespace Harpoon.Tests
{
    public class DefaultSignatureServiceTests
    {
        const string ValidSecret = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_";

        [Fact]
        public void BadSecret()
        {
            var service = new DefaultSignatureService();
            Assert.Throws<ArgumentException>(() => service.GetSignature(null, "content"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("content")]
        public void NormalScenario(string content)
        {
            var service = new DefaultSignatureService();
            service.GetSignature(ValidSecret, content);
            Assert.True(true);
        }
    }
}