using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
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
        /// Http header key used to pass the trigger id.
        /// </summary>
        public const string TriggerKey = "X-WebHook-Trigger";
        /// <summary>
        /// Http header key used to pass the time stamp.
        /// </summary>
        public const string TimestampKey = "X-WebHook-Timestamp";
        /// <summary>
        /// Http header key used to pass a unique id.
        /// </summary>
        public const string UniqueIdKey = "X-WebHook-NotificationId";
        /// <summary>
        /// Http header key used to pass the current payload signature
        /// </summary>
        public const string SignatureHeader = "X-WebHook-Signature";

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

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
                var response = await HttpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogInformation($"WebHook {webHookWorkItem.WebHook.Id} send. Status: {response.StatusCode}.");
                    await OnSuccessAsync(response, webHookWorkItem, cancellationToken);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
                {
                    Logger.LogInformation($"WebHook {webHookWorkItem.WebHook.Id} send. Status: {response.StatusCode}.");
                    await OnNotFoundAsync(response, webHookWorkItem, cancellationToken);
                }
                else
                {
                    Logger.LogError($"WebHook {webHookWorkItem.WebHook.Id} failed. Status: {response.StatusCode}");
                    await OnFailureAsync(response, null, webHookWorkItem, cancellationToken);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"WebHook {webHookWorkItem.WebHook.Id} failed: {e.Message}.");
                await OnFailureAsync(null, e, webHookWorkItem, cancellationToken);
            }
        }

        /// <summary>
        /// Callback on request success
        /// </summary>
        /// <param name="response"></param>
        /// <param name="webHookWorkItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task OnSuccessAsync(HttpResponseMessage response, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Callback on request 404 or 410 (Gone)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="webHookWorkItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task OnNotFoundAsync(HttpResponseMessage response, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Callback on request failure.
        /// </summary>
        /// <param name="response">May be null</param>
        /// <param name="exception">May be null</param>
        /// <param name="webHookWorkItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task OnFailureAsync(HttpResponseMessage response, Exception exception, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Generates the request to be send
        /// </summary>
        /// <param name="webHookWorkItem"></param>
        /// <returns></returns>
        protected virtual HttpRequestMessage CreateRequest(IWebHookWorkItem webHookWorkItem)
        {
            var serializedBody = JsonConvert.SerializeObject(webHookWorkItem.Notification.Payload, _settings);

            var request = new HttpRequestMessage(HttpMethod.Post, webHookWorkItem.WebHook.Callback);
            AddHeaders(webHookWorkItem, request, SignatureService.GetSignature(webHookWorkItem.WebHook.Secret, serializedBody));
            request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

            return request;
        }

        private void AddHeaders(IWebHookWorkItem webHookWorkItem, HttpRequestMessage request, string secret)
        {
            request.Headers.Add(SignatureHeader, secret);
            request.Headers.Add(TriggerKey, webHookWorkItem.Notification.TriggerId);
            request.Headers.Add(TimestampKey, webHookWorkItem.Timestamp.ToUniversalTime().ToString("r"));
            request.Headers.Add(UniqueIdKey, webHookWorkItem.Id.ToString());
        }
    }
}