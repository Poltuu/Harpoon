using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Harpoon
{
    public class DefaultWebHookValidator : IWebHookValidator
    {
        private static readonly HashSet<string> ValidSchemes = new HashSet<string> { Uri.UriSchemeHttp.ToString(), Uri.UriSchemeHttps.ToString() };

        protected IWebHookTriggerProvider WebHookTriggerProvider { get; private set; }
        protected ILogger<DefaultWebHookValidator> Logger { get; private set; }
        protected HttpClient HttpClient { get; private set; }

        public DefaultWebHookValidator(IWebHookTriggerProvider webHookTriggerProvider, ILogger<DefaultWebHookValidator> logger, HttpClient httpClient)
        {
            WebHookTriggerProvider = webHookTriggerProvider ?? throw new ArgumentNullException(nameof(webHookTriggerProvider));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public virtual async Task ValidateAsync(IWebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            await VerifyIdAsync(webHook);
            await VerifySecretAsync(webHook);
            await VerifyFiltersAsync(webHook);
            await VerifyCallbackAsync(webHook);
        }

        protected virtual Task VerifyIdAsync(IWebHook webHook)
        {
            if (webHook.Id == default)
            {
                webHook.Id = Guid.NewGuid();
            }
            return Task.CompletedTask;
        }

        protected virtual Task VerifySecretAsync(IWebHook webHook)
        {
            if (string.IsNullOrEmpty(webHook.Secret))
            {
                webHook.Secret = GetUniqueKey(64);
                return Task.CompletedTask;
            }

            if (webHook.Secret.Length != 64)
            {
                throw new ArgumentException("WebHooks secret needs to be set to a 64 characters string.");
            }

            return Task.CompletedTask;
        }

        private static string GetUniqueKey(int size)
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_".ToCharArray();
            var data = new byte[size];
            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            var result = new StringBuilder(size);
            foreach (var b in data)
            {
                result.Append(chars[b % chars.Length]);
            }
            return result.ToString();
        }

        protected virtual async Task VerifyFiltersAsync(IWebHook webHook)
        {
            if (webHook.Filters == null || webHook.Filters.Count == 0)
            {
                throw new ArgumentException("WebHooks need to target at least one trigger. Wildcard is not allowed.");
            }

            var triggers = await WebHookTriggerProvider.GetAvailableTriggersAsync();
            var errors = new List<string>();
            foreach (var filter in webHook.Filters)
            {
                if (!triggers.ContainsKey(filter.TriggerId))
                {
                    errors.Add($" - Trigger {filter.TriggerId} is not valid.");
                    continue;
                }

                if (filter.Parameters != null)
                {
                    foreach (var invalidParam in filter.Parameters.Keys.Where(k => !triggers[filter.TriggerId].Template.ContainsKey(k)))
                    {
                        errors.Add($" - {invalidParam} is not a valid parameter to filter the trigger {filter.TriggerId}.");
                    }
                }
            }

            if (errors.Count != 0)
            {
                throw new ArgumentException("WebHooks filters are incorrect :" + Environment.NewLine + string.Join(Environment.NewLine, errors));
            }
        }

        protected virtual async Task VerifyCallbackAsync(IWebHook webHook)
        {
            if (webHook.Callback == null)
            {
                throw new ArgumentException("WebHooks callback needs to be set.");
            }

            if (!(webHook.Callback.IsAbsoluteUri && ValidSchemes.Contains(webHook.Callback.Scheme)))
            {
                throw new ArgumentException("WebHooks callback needs to be a valid http(s) absolute Uri.");
            }

            var query = HttpUtility.ParseQueryString(webHook.Callback.Query);
            if (query["noecho"] != null)
            {
                Logger.LogInformation($"Webhook {webHook.Id} does not allow url verification (noecho query parameter has been found).");
                return;
            }

            try
            {
                var expectedResult = Guid.NewGuid();
                var echoUri = new UriBuilder(webHook.Callback) { Query = "echo=" + expectedResult };
                var response = await HttpClient.GetStringAsync(echoUri.Uri);

                if (Guid.TryParse(response, out var responseResult) && responseResult == expectedResult)
                {
                    return;
                }

                throw new ArgumentException($"WebHook {webHook.Id} callback verification failed. Response is incorrect: {response}.{Environment.NewLine}To cancel callback verification, add `noecho` as a query parameter.");
            }
            catch (Exception e)
            {
                var message = $"WebHook {webHook.Id} callback verification failed: {e.Message}.{Environment.NewLine}To cancel callback verification, add `noecho` as a query parameter.";
                Logger.LogError(message, e);
                throw new ArgumentException(message);
            }
        }
    }
}