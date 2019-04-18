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

        private readonly IWebHookActionProvider _webHookActionProvider;
        private readonly ILogger<DefaultWebHookValidator> _logger;
        private readonly HttpClient _httpClient;

        public DefaultWebHookValidator(IWebHookActionProvider webHookActionProvider, ILogger<DefaultWebHookValidator> logger, HttpClient httpClient)
        {
            _webHookActionProvider = webHookActionProvider ?? throw new ArgumentNullException(nameof(webHookActionProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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
            if (webHook.Filters.Count == 0)
            {
                throw new ArgumentException("WebHooks need to target at least one action");
            }

            var actions = await _webHookActionProvider.GetAvailableActionsAsync();
            var errors = new List<string>();
            foreach (var filter in webHook.Filters)
            {
                if (!actions.ContainsKey(filter.ActionId))
                {
                    errors.Add($" - Action {filter.ActionId} is not valid.");
                }

                foreach (var invalidParam in filter.Parameters.Keys.Where(k => !actions[filter.ActionId].AvailableParameters.Contains(k)))
                {
                    errors.Add($" - {invalidParam} is not a valid parameter to filter the action {filter.ActionId}.");
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
                _logger.LogInformation($"Webhook {webHook.Id} does not allow url verification (noecho query parameter has been found).");
                return;
            }

            try
            {
                var expectedResult = Guid.NewGuid();
                var echoUri = new UriBuilder(webHook.Callback) { Query = "echo=" + expectedResult };
                var response = await _httpClient.GetStringAsync(echoUri.Uri);

                if (Guid.TryParse(response, out var responseResult) && responseResult == expectedResult)
                {
                    return;
                }

                throw new ArgumentException($"WebHook {webHook.Id} callback verification failed. Response is incorrect: {response}");
            }
            catch (Exception e)
            {
                var message = $"WebHook {webHook.Id} callback verification failed: {e.Message}";
                _logger.LogError(message, e);
                throw new ArgumentException(message);
            }
        }
    }
}