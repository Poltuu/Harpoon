using Harpoon.Background;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Sender
{
    public class DefaultWebHookSender : IWebHookSender, IQueuedProcessor<IWebHookWorkItem>
    {
        public const string TriggerKey = "Trigger";
        public const string TimestampKey = "Timestamp";
        public const string UniqueIdKey = "NotificationId";
        public const string SignatureHeader = "X-Signature-SHA256";

        protected HttpClient HttpClient { get; private set; }
        protected ISignatureService SignatureService { get; private set; }
        protected ILogger<DefaultWebHookSender> Logger { get; private set; }

        public DefaultWebHookSender(HttpClient httpClient, ISignatureService signatureService, ILogger<DefaultWebHookSender> logger)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            SignatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        Task IQueuedProcessor<IWebHookWorkItem>.ProcessAsync(IWebHookWorkItem workItem, CancellationToken token)
            => SendAsync(workItem, token);

        public async Task SendAsync(IWebHookWorkItem webHookWorkItem, CancellationToken token)
        {
            if (webHookWorkItem == null)
            {
                throw new ArgumentNullException(nameof(webHookWorkItem));
            }

            try
            {
                var request = CreateRequest(webHookWorkItem);
                var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogInformation($"WebHook {webHookWorkItem.WebHook.Id} send. Status: {response.StatusCode}.");
                    await OnSuccessAsync(webHookWorkItem);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
                {
                    Logger.LogInformation($"WebHook {webHookWorkItem.WebHook.Id} send. Status: {response.StatusCode}.");
                    await OnNotFoundAsync(webHookWorkItem);
                }
                else
                {
                    Logger.LogError($"WebHook {webHookWorkItem.WebHook.Id} failed. Status: {response.StatusCode}");
                    await OnFailureAsync(null, webHookWorkItem);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"WebHook {webHookWorkItem.WebHook.Id} failed: {e.Message}.", e);
                await OnFailureAsync(e, webHookWorkItem);
            }
        }

        protected virtual Task OnSuccessAsync(IWebHookWorkItem webHookWorkItem)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnNotFoundAsync(IWebHookWorkItem webHookWorkItem)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnFailureAsync(Exception exception, IWebHookWorkItem webHookWorkItem)
        {
            return Task.CompletedTask;
        }

        protected virtual HttpRequestMessage CreateRequest(IWebHookWorkItem webHookWorkItem)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, webHookWorkItem.WebHook.Callback);

            var serializedBody = JsonConvert.SerializeObject(CreateBody(webHookWorkItem));
            request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

            SignRequest(webHookWorkItem.WebHook, request, serializedBody);

            return request;
        }

        protected virtual IReadOnlyDictionary<string, object> CreateBody(IWebHookWorkItem webHookWorkItem)
        {
            if (webHookWorkItem.Notification.Payload == null || webHookWorkItem.Notification.Payload.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    [TriggerKey] = webHookWorkItem.Notification.TriggerId,
                    [TimestampKey] = webHookWorkItem.Timestamp,
                    [UniqueIdKey] = webHookWorkItem.Id
                };
            }

            //keys may be overwritten
            return new Dictionary<string, object>(webHookWorkItem.Notification.Payload)
            {
                [TriggerKey] = webHookWorkItem.Notification.TriggerId,
                [TimestampKey] = webHookWorkItem.Timestamp,
                [UniqueIdKey] = webHookWorkItem.Id
            };
        }

        protected virtual void SignRequest(IWebHook webHook, HttpRequestMessage request, string serializedBody)
        {
            var signature = SignatureService.GetSignature(webHook.Secret, serializedBody);
            request.Headers.Add(SignatureHeader, signature);
        }
    }
}