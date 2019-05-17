using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Harpoon.Registrations
{
    /// <summary>
    /// Default <see cref="IWebHookValidator"/> implementation
    /// </summary>
    public class DefaultWebHookValidator : IWebHookValidator
    {
        private static readonly Dictionary<(string, bool), HashSet<Type>> _validTypes = new Dictionary<(string, bool), HashSet<Type>>
        {
            [("number", false)] = new HashSet<Type>
                {
                    typeof(float), typeof(double), typeof(decimal), typeof(ulong), typeof(long), typeof(uint), typeof(int), typeof(ushort), typeof(short), typeof(sbyte), typeof(byte)
                },
            [("number", true)] = new HashSet<Type>
                {
                    typeof(float), typeof(double), typeof(decimal), typeof(ulong), typeof(long), typeof(uint), typeof(int), typeof(ushort), typeof(short), typeof(sbyte), typeof(byte),
                    typeof(float?), typeof(double?), typeof(decimal?), typeof(ulong?), typeof(long?), typeof(uint?), typeof(int?), typeof(ushort?), typeof(short?), typeof(sbyte?), typeof(byte?)
                },
            [("integer", false)] = new HashSet<Type>
                {
                    typeof(ulong), typeof(long), typeof(uint), typeof(int), typeof(ushort), typeof(short), typeof(sbyte), typeof(byte)
                },
            [("integer", true)] = new HashSet<Type>
                {
                    typeof(ulong), typeof(long), typeof(uint), typeof(int), typeof(ushort), typeof(short), typeof(sbyte), typeof(byte),
                    typeof(ulong?), typeof(long?), typeof(uint?), typeof(int?), typeof(ushort?), typeof(short?), typeof(sbyte?), typeof(byte?)
                },
            [("boolean", false)] = new HashSet<Type> { typeof(bool) },
            [("boolean", true)] = new HashSet<Type> { typeof(bool), typeof(bool?) }
        };

        private static readonly HashSet<string> ValidSchemes = new HashSet<string> { Uri.UriSchemeHttp.ToString(), Uri.UriSchemeHttps.ToString() };

        /// <summary>
        /// Gets the <see cref="IWebHookTriggerProvider"/> 
        /// </summary>
        protected IWebHookTriggerProvider WebHookTriggerProvider { get; private set; }
        /// <summary>
        /// Gets the <see cref="ILogger{DefaultWebHookValidator}"/> 
        /// </summary>
        protected ILogger<DefaultWebHookValidator> Logger { get; private set; }
        /// <summary>
        /// Gets the <see cref="HttpClient"/> 
        /// </summary>
        protected HttpClient HttpClient { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultWebHookValidator"/> class.
        /// </summary>
        /// <param name="webHookTriggerProvider"></param>
        /// <param name="logger"></param>
        /// <param name="httpClient"></param>
        public DefaultWebHookValidator(IWebHookTriggerProvider webHookTriggerProvider, ILogger<DefaultWebHookValidator> logger, HttpClient httpClient)
        {
            WebHookTriggerProvider = webHookTriggerProvider ?? throw new ArgumentNullException(nameof(webHookTriggerProvider));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc />
        public virtual async Task ValidateAsync(IWebHook webHook, CancellationToken cancellationToken = default)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            await VerifyIdAsync(webHook, cancellationToken);
            await VerifySecretAsync(webHook, cancellationToken);
            await VerifyFiltersAsync(webHook, cancellationToken);
            await VerifyCallbackAsync(webHook, cancellationToken);
        }

        /// <summary>
        /// Id validation
        /// </summary>
        /// <param name="webHook"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task VerifyIdAsync(IWebHook webHook, CancellationToken cancellationToken)
        {
            if (webHook.Id == default)
            {
                webHook.Id = Guid.NewGuid();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Secret validation
        /// </summary>
        /// <param name="webHook"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Secret is not a 64 characters string</exception>
        protected virtual Task VerifySecretAsync(IWebHook webHook, CancellationToken cancellationToken)
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

        /// <summary>
        /// Filters validation
        /// </summary>
        /// <param name="webHook"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">No filter, or incorrect filters</exception>
        protected virtual Task VerifyFiltersAsync(IWebHook webHook, CancellationToken cancellationToken)
        {
            if (webHook.Filters == null || webHook.Filters.Count == 0)
            {
                throw new ArgumentException("WebHooks need to target at least one trigger. Wildcard is not allowed.");
            }

            var triggers = WebHookTriggerProvider.GetAvailableTriggers();
            var errors = new List<string>();
            foreach (var filter in webHook.Filters)
            {
                if (!triggers.ContainsKey(filter.Trigger))
                {
                    errors.Add($" - Trigger {filter.Trigger} is not valid.");
                    continue;
                }

                var trigger = triggers[filter.Trigger];
                if (filter.Parameters != null && trigger.Schema != null)
                {
                    foreach (var invalidParam in filter.Parameters.Where(kvp => !IsValidParameter(kvp.Key, kvp.Value, trigger.Schema)))
                    {
                        errors.Add($" - {invalidParam} is not a valid parameter to filter the trigger {filter.Trigger}.");
                    }
                }
            }

            if (errors.Count != 0)
            {
                throw new ArgumentException("WebHooks filters are incorrect :" + Environment.NewLine + string.Join(Environment.NewLine, errors));
            }
            return Task.CompletedTask;
        }

        private bool IsValidParameter(string key, object value, OpenApiSchema schema)
        {
            if (string.IsNullOrEmpty(key) || schema?.Properties == null)
            {
                return false;
            }

            var parts = key.Split('.');
            OpenApiSchema currentSchema = schema;
            foreach (var part in parts)
            {
                if (!schema.Properties.ContainsKey(part))
                {
                    return false;
                }

                currentSchema = schema.Properties[part];
                if (currentSchema.Type == "array")
                {
                    currentSchema = currentSchema.Items;
                }

                if (currentSchema == null)
                {
                    return false;
                }
            }

            if (value == null)
            {
                return currentSchema.Nullable;
            }

            switch (currentSchema.Type)
            {
                case "string": return true;
                case "number":
                case "integer":
                case "boolean ":
                    return _validTypes[(currentSchema.Type, currentSchema.Nullable)].Contains(value.GetType());
                case "array": return false;
                case "object": return false;
                default:
                    throw new ArgumentException("Given OpenApiSchema Type does not match specification. Type is restricted to: string, number, integer, boolean, array and object.");
            }
        }

        /// <summary>
        /// Callback validation. If noecho is used, the given url should not be called.
        /// The url is tested by sending a GET request containing a echo query parameter that should be echoed.
        /// </summary>
        /// <param name="webHook"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Callback is invalid, not an http(s) url, of echo procedure failed</exception>
        protected virtual async Task VerifyCallbackAsync(IWebHook webHook, CancellationToken cancellationToken)
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