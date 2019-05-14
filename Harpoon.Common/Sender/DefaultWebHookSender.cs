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
    /// <summary>
    /// Default <see cref="IQueuedProcessor{IWebHookWorkItem}"/> implementation
    /// </summary>
    public class DefaultWebHookSender : IWebHookSender, IQueuedProcessor<IWebHookWorkItem>
    {
        /// <summary>
        /// Payload key used to pass the trigger id.
        /// </summary>
        public const string TriggerKey = "Trigger";
        /// <summary>
        /// Payload key used to pass the time stamp.
        /// </summary>
        public const string TimestampKey = "Timestamp";
        /// <summary>
        /// Payload key used to pass a unique id.
        /// </summary>
        public const string UniqueIdKey = "NotificationId";

        /// <summary>
        /// Http header key used to pass current payload signature
        /// </summary>
        public const string SignatureHeader = "X-Signature-SHA256";

        /// <summary>
        /// Gets the <see cref="System.Net.Http.HttpClient"/> used for sending request
        /// </summary>
        protected HttpClient HttpClient { get; private set; }
        /// <summary>
        /// Gets the <see cref="ISignatureService"/> used to sign payloads
        /// </summary>
        protected ISignatureService SignatureService { get; private set; }
        /// <summary>
        /// Gets the <see cref="ILogger{DefaultWebHookSender}"/>
        /// </summary>
        protected ILogger<DefaultWebHookSender> Logger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultWebHookSender"/> class.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="signatureService"></param>
        /// <param name="logger"></param>
        public DefaultWebHookSender(HttpClient httpClient, ISignatureService signatureService, ILogger<DefaultWebHookSender> logger)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            SignatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        Task IQueuedProcessor<IWebHookWorkItem>.ProcessAsync(IWebHookWorkItem workItem, CancellationToken cancellationToken)
            => SendAsync(workItem, cancellationToken);

        /// <inheritdoc />
        public async Task SendAsync(IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken = default)
        {
            if (webHookWorkItem == null)
            {
                throw new ArgumentNullException(nameof(webHookWorkItem));
            }

            try
            {
                var request = CreateRequest(webHookWorkItem);
                var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogInformation($"WebHook {webHookWorkItem.WebHook.Id} send. Status: {response.StatusCode}.");
                    await OnSuccessAsync(webHookWorkItem, cancellationToken);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
                {
                    Logger.LogInformation($"WebHook {webHookWorkItem.WebHook.Id} send. Status: {response.StatusCode}.");
                    await OnNotFoundAsync(webHookWorkItem, cancellationToken);
                }
                else
                {
                    Logger.LogError($"WebHook {webHookWorkItem.WebHook.Id} failed. Status: {response.StatusCode}");
                    await OnFailureAsync(null, webHookWorkItem, cancellationToken);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"WebHook {webHookWorkItem.WebHook.Id} failed: {e.Message}.", e);
                await OnFailureAsync(e, webHookWorkItem, cancellationToken);
            }
        }

        /// <summary>
        /// Callback on request success
        /// </summary>
        /// <param name="webHookWorkItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task OnSuccessAsync(IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Callback on request 404 or 410 (Gone)
        /// </summary>
        /// <param name="webHookWorkItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task OnNotFoundAsync(IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Callback on request failure.
        /// </summary>
        /// <param name="exception">May be null</param>
        /// <param name="webHookWorkItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task OnFailureAsync(Exception exception, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Generates the request to be send
        /// </summary>
        /// <param name="webHookWorkItem"></param>
        /// <returns></returns>
        protected virtual HttpRequestMessage CreateRequest(IWebHookWorkItem webHookWorkItem)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, webHookWorkItem.WebHook.Callback);

            var serializedBody = JsonConvert.SerializeObject(CreateBody(webHookWorkItem));
            request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

            SignRequest(webHookWorkItem.WebHook, request, serializedBody);

            return request;
        }

        /// <summary>
        /// Generates the request body
        /// </summary>
        /// <param name="webHookWorkItem"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Apply signature to the HttpRequestMessage
        /// </summary>
        /// <param name="webHook"></param>
        /// <param name="request"></param>
        /// <param name="serializedBody"></param>
        protected virtual void SignRequest(IWebHook webHook, HttpRequestMessage request, string serializedBody)
        {
            var signature = SignatureService.GetSignature(webHook.Secret, serializedBody);
            request.Headers.Add(SignatureHeader, signature);
        }
    }
}