using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Harpoon.Sender
{
    public class DefaultWebHookSender : IWebHookSender
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DefaultWebHookSender> _logger;

        public DefaultWebHookSender(HttpClient httpClient, ILogger<DefaultWebHookSender> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (webHooks == null)
            {
                throw new ArgumentNullException(nameof(webHooks));
            }

            return Task.WhenAll(webHooks.Select(w => SendAsync(notification, w)));
        }

        private async Task SendAsync(IWebHookNotification notification, IWebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            try
            {
                var request = CreateRequest(notification, webHook);
                var response = await _httpClient.SendAsync(request);

                _logger.LogInformation($"WebHook {webHook.Id} send. Status: {response.StatusCode}.");

                if (response.IsSuccessStatusCode)
                {
                    await OnSuccessAsync(notification, webHook);
                    return;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
                {
                    await OnNotFoundAsync(notification, webHook);
                    return;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"WebHook {webHook.Id} failed: {e.Message}.", e);
                await OnFailureAsync(e, notification, webHook);
            }
        }

        protected virtual Task OnSuccessAsync(IWebHookNotification notification, IWebHook webHook)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnNotFoundAsync(IWebHookNotification notification, IWebHook webHook)
        {
            webHook.IsPaused = true;
            return Task.CompletedTask;
        }

        protected virtual Task OnFailureAsync(Exception exception, IWebHookNotification notification, IWebHook webHook)
        {
            webHook.IsPaused = true;
            return Task.CompletedTask;
        }

        protected virtual HttpRequestMessage CreateRequest(IWebHookNotification notification, IWebHook webHook)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, webHook.Callback);

            var body = CreateBody(notification);
            var serializedBody = JsonConvert.SerializeObject(body);
            request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

            SignRequest(webHook, request, serializedBody);

            return request;
        }

        protected virtual Dictionary<string, object> CreateBody(IWebHookNotification notification)
        {
            return new Dictionary<string, object>(notification.Payload)
            {
                ["Action"] = notification.ActionId
            };
        }

        protected virtual void SignRequest(IWebHook webHook, HttpRequestMessage request, string serializedBody)
        {
            var secret = Encoding.UTF8.GetBytes(webHook.Secret);
            using (var hasher = new HMACSHA256(secret))
            {
                var data = Encoding.UTF8.GetBytes(serializedBody);
                var sha256 = hasher.ComputeHash(data);
                request.Headers.Add("X-Signature-SHA256", EncodingUtilities.ToHex(sha256));
            }
        }
    }
}