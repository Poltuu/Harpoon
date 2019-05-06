using Harpoon.Controllers.Models;
using Harpoon.Tests.Fixtures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class ControllersTests : IClassFixture<HostFixture>
    {
        private readonly HostFixture _fixture;

        public ControllersTests(HostFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetAllTestsAsync()
        {
            var response = await _fixture.Client.GetAsync("/api/webhooks");
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task CreateAndGetByIdTestsAsync()
        {
            var response = await _fixture.Client.GetAsync($"/api/webhooks/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var badCreationResponse = await _fixture.Client.PostAsync("/api/webhooks", new StringContent("bug", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, badCreationResponse.StatusCode);

            //invalid as no url
            var newWebHook = new WebHookDTO
            {
                Id = Guid.NewGuid(),
                Secret = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_",
                Filters = new List<WebHookFilterDTO>
                {
                    new WebHookFilterDTO
                    {
                        ActionId = "action1"
                    }
                }
            };

            badCreationResponse = await _fixture.Client.PostAsync("/api/webhooks", new StringContent(JsonConvert.SerializeObject(newWebHook), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, badCreationResponse.StatusCode);

            newWebHook.Callback = new Uri("http://www.example.org?noecho=");
            var creationResponse = await _fixture.Client.PostAsync("/api/webhooks", new StringContent(JsonConvert.SerializeObject(newWebHook), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Created, creationResponse.StatusCode);

            response = await _fixture.Client.GetAsync($"/api/webhooks/{newWebHook.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Fact]
        public async Task PutTestsAsync()
        {
            var id = Guid.NewGuid();
            var newWebHook = new WebHookDTO
            {
                Id = id,
                Callback = new Uri("http://www.example2.org?noecho="),
                Secret = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_",
                Filters = new List<WebHookFilterDTO>
                {
                    new WebHookFilterDTO
                    {
                        ActionId = "action1"
                    }
                }
            };

            await _fixture.Client.PostAsync("/api/webhooks", new StringContent(JsonConvert.SerializeObject(newWebHook), Encoding.UTF8, "application/json"));
            

            var badResponse = await _fixture.Client.PutAsync($"/api/webhooks/{id}", new StringContent("incorrect input", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);

            newWebHook.Id = Guid.NewGuid();
            badResponse = await _fixture.Client.PutAsync($"/api/webhooks/{newWebHook.Id}", new StringContent(JsonConvert.SerializeObject(newWebHook), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.NotFound, badResponse.StatusCode);

            badResponse = await _fixture.Client.PutAsync($"/api/webhooks/{id}", new StringContent(JsonConvert.SerializeObject(newWebHook), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);
            newWebHook.Id = id;

            newWebHook.Secret = "wrong secret";
            badResponse = await _fixture.Client.PutAsync($"/api/webhooks/{id}", new StringContent(JsonConvert.SerializeObject(newWebHook), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);
            newWebHook.Secret = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_";

            var response = await _fixture.Client.PutAsync($"/api/webhooks/{id}", new StringContent(JsonConvert.SerializeObject(newWebHook), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Fact]
        public async Task DeleteTestsAsync()
        {
            var id = Guid.NewGuid();
            var newWebHook = new WebHookDTO
            {
                Id = id,
                Callback = new Uri("http://www.example2.org?noecho="),
                Secret = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_",
                Filters = new List<WebHookFilterDTO>
                {
                    new WebHookFilterDTO
                    {
                        ActionId = "action1"
                    }
                }
            };

            await _fixture.Client.PostAsync("/api/webhooks", new StringContent(JsonConvert.SerializeObject(newWebHook), Encoding.UTF8, "application/json"));

            var badResponse = await _fixture.Client.DeleteAsync($"/api/webhooks/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, badResponse.StatusCode);

            var response = await _fixture.Client.DeleteAsync($"/api/webhooks/{id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await _fixture.Client.DeleteAsync($"/api/webhooks");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}