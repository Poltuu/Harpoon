using Harpoon.Registrations;
using Harpoon.Registrations.EFStorage;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Harpoon.Tests
{
    public class WebHookMatcherTests
    {
        [Fact]
        public void ArgNull()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookMatcher().Matches(null, new WebHookNotification()));
            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookMatcher().Matches(new WebHook(), null));
        }

        private class Payload : IPayloadable
        {
            public int Param { get; set; }
            public IEnumerable<int> Params { get; set; }
            public DateTime Date { get; set; }
            public float Float { get; set; }
            public Payload Sub { get; set; }
            public Guid NotificationId { get; set; }
        }

        public static IEnumerable<object[]> MatchesScenario => new List<object[]>
        {
            new object[] { null, new WebHookNotification(), true },//no filters
            new object[] { new List<WebHookFilter>(), new WebHookNotification(), true },//no filters
            new object[] { new List<WebHookFilter> { new WebHookFilter { Trigger = "wrong" } }, new WebHookNotification { TriggerId = "ok" }, false }, //Wrong trigger
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = null } }, new WebHookNotification { }, true }, //no params
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object>() } }, new WebHookNotification { }, true }, //no params
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { [""] = null } } }, new WebHookNotification { Payload = null }, true }, //null expected
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { [""] = null } } }, new WebHookNotification { Payload = new Payload() }, false }, //null expected
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["param"] = 1 } } }, new WebHookNotification { Payload = null }, false }, //null payload
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["param2"] = 1 } } }, new WebHookNotification { Payload = new Payload { Param = 1 } }, false }, //not a property
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["sub.param"] = 1 } } }, new WebHookNotification { Payload = new Payload { Sub = new Payload { Param = 1 } } }, true }, //good sub params
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["params"] = 2 } } }, new WebHookNotification { Payload = new Payload { Params = new List<int> { 1, 2, 3 } } }, true }, //value found list
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["params"] = 2 } } }, new WebHookNotification { Payload = new Payload { Params = new[] { 1, 2, 3 } } }, true }, //value found array
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["params"] = 20 } } }, new WebHookNotification { Payload = new Payload { Params = new List<int> { 1, 3 } } }, false }, //value not found
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["params"] = new List<int> { 1, 2, 3 } } } }, new WebHookNotification { Payload = new Payload { Params = new List<int> { 1, 2, 3 } } }, true }, //sequence found
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["param"] = 1 } } }, new WebHookNotification { Payload = new Payload { Param = 1 } }, true }, //good params
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["date"] = DateTime.Today.ToString("o", CultureInfo.InvariantCulture) } } }, new WebHookNotification { Payload = new Payload { Date = DateTime.Today } }, true }, //date parsing
            new object[] { new List<WebHookFilter> { new WebHookFilter { Parameters = new Dictionary<string, object> { ["float"] = "2.3" } } }, new WebHookNotification { Payload = new Payload { Float = 2.3F } }, true }, //numeric parsing
        };

        [Theory]
        [MemberData(nameof(MatchesScenario))]
        public void MatchesTests(List<WebHookFilter> filters, WebHookNotification notif, bool result)
        {
            Assert.Equal(result, new DefaultWebHookMatcher().Matches(new WebHook { Filters = filters }, notif));
        }

    }
}