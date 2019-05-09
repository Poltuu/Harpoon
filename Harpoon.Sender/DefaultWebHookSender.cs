using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Sender
{
    public class DefaultWebHookSender : IWebHookSender
    {
        public const string TriggerKey = "Trigger";
        public const string SignatureHeader = "X-Signature-SHA256";

        private readonly HttpClient _httpClient;
        private readonly ISignatureService _signatureService;
        private readonly ILogger<DefaultWebHookSender> _logger;

        public DefaultWebHookSender(HttpClient httpClient, ISignatureService signatureService, ILogger<DefaultWebHookSender> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks, CancellationToken token)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (webHooks == null)
            {
                throw new ArgumentNullException(nameof(webHooks));
            }

            if (webHooks.Count == 0)
            {
                return Task.CompletedTask;
            }

            return Task.WhenAll(webHooks.Select(w => SendAsync(notification, w, token)));
        }

        private async Task SendAsync(IWebHookNotification notification, IWebHook webHook, CancellationToken token)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            try
            {
                var request = CreateRequest(notification, webHook);
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"WebHook {webHook.Id} send. Status: {response.StatusCode}.");
                    await OnSuccessAsync(notification, webHook);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
                {
                    _logger.LogInformation($"WebHook {webHook.Id} send. Status: {response.StatusCode}.");
                    await OnNotFoundAsync(notification, webHook);
                }
                else
                {
                    _logger.LogError($"WebHook {webHook.Id} failed. Status: {response.StatusCode}");
                    await OnFailureAsync(null, notification, webHook);
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
            return Task.CompletedTask;
        }

        protected virtual Task OnFailureAsync(Exception exception, IWebHookNotification notification, IWebHook webHook)
        {
            return Task.CompletedTask;
        }

        protected virtual HttpRequestMessage CreateRequest(IWebHookNotification notification, IWebHook webHook)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, webHook.Callback);

            var serializedBody = JsonConvert.SerializeObject(CreateBody(notification));
            request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

            SignRequest(webHook, request, serializedBody);

            return request;
        }

        protected virtual IReadOnlyDictionary<string, object> CreateBody(IWebHookNotification notification)
        {
            if (notification.Payload == null || notification.Payload.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    [TriggerKey] = notification.TriggerId
                };
            }

            if (notification.Payload.ContainsKey(TriggerKey))
            {
                return notification.Payload;
            }

            return new Dictionary<string, object>(notification.Payload)
            {
                [TriggerKey] = notification.TriggerId
            };
        }

        protected virtual void SignRequest(IWebHook webHook, HttpRequestMessage request, string serializedBody)
        {
            var signature = _signatureService.GetSignature(webHook.Secret, serializedBody);
            request.Headers.Add(SignatureHeader, signature);
        }
    }
}