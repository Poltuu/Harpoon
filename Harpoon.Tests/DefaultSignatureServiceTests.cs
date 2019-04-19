using Harpoon.Sender;
using Harpoon.Sender.Background;
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
            Assert.Throws<ArgumentException>(() => service.GetSignature(null, "dzdazd"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("edzezed")]
        public void NormalScenario(string content)
        {
            var service = new DefaultSignatureService();
            service.GetSignature(ValidSecret, content);
            Assert.True(true);
        }
    }
}