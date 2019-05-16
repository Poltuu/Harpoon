using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Harpoon.Registrations
{
    /// <inheritdoc />
    public class DefaultWebHookMatcher : IWebHookMatcher
    {
        /// <inheritdoc />
        public bool Matches(IWebHook webHook, IWebHookNotification notification)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (webHook.Filters == null || webHook.Filters.Count == 0)
            {
                return true;
            }

            return webHook.Filters
                .Where(f => IsTriggerMatching(f.Trigger, notification.TriggerId))
                .OrderBy(f => f.Parameters == null ? 0 : f.Parameters.Count)
                .Any(f => f.Parameters == null || f.Parameters.Count == 0 || f.Parameters.All(kvp => IsPayloadMatchingParameter(kvp.Key, kvp.Value, notification.Payload)));
        }

        /// <summary>
        /// This returns a value indicating if the trigger found on a <see cref="IWebHookFilter"/> matches the one on a given <see cref="IWebHookNotification"/>
        /// </summary>
        /// <param name="filterTrigger"></param>
        /// <param name="notificationTrigger"></param>
        /// <returns></returns>
        protected virtual bool IsTriggerMatching(string filterTrigger, string notificationTrigger)
        {
            return filterTrigger == notificationTrigger;
        }

        /// <summary>
        /// Returns a value indicating if a key value pair matches a payload value
        /// Key might references a nested property i.e. object.property.id
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected virtual bool IsPayloadMatchingParameter(string key, object value, object payload)
            => IsMatchingParameter(new Queue<string>((key ?? "").Split(".")), value, payload);

        private bool IsMatchingParameter(Queue<string> properties, object value, object payload)
        {
            if (properties == null || properties.Count == 0)
            {
                return CompareValues(value, payload);
            }

            var propertyName = properties.Dequeue();
            if (string.IsNullOrEmpty(propertyName))
            {
                return CompareValues(value, payload);
            }

            if (payload == null)
            {
                return false;
            }

            var property = payload.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
            if (property == null)
            {
                if (payload is IDictionary dictionary)
                {
                    return CompareValues(value, dictionary[propertyName]);
                }

                return value == null;
            }

            var payloadValue = property.GetValue(payload);
            if (properties.Count > 0)
            {
                return IsMatchingParameter(properties, value, payloadValue);
            }

            return CompareValues(value, payloadValue);
        }

        private bool CompareValues(object expected, object actual)
        {
            if (expected == null && actual == null)
            {
                return true;
            }

            if (expected == null || actual == null)
            {
                return false;
            }

            if (actual is string text)
            {
                return text.Equals(expected.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            if (actual is IList list && list.Contains(expected))
            {
                return true;
            }

            if (actual is IEnumerable enumerable)
            {
                var typedEnumerable = enumerable.Cast<object>();
                if (expected is IEnumerable expectedEnumerable)
                {
                    return typedEnumerable.SequenceEqual(expectedEnumerable.Cast<object>());
                }
                return typedEnumerable.Contains(expected);
            }

            if (expected.Equals(actual))
            {
                return true;
            }

            try
            {
                return Convert.ChangeType(expected, actual.GetType(), CultureInfo.InvariantCulture).Equals(actual);
            }
            catch
            {
                return false;
            }
        }
    }
}