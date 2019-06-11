using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Harpoon
{
    /// <summary>
    /// Default Payload implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Payloadable<T> : IReadOnlyDictionary<string, object> where T : class
    {
        private readonly Dictionary<string, PropertyInfo> _properties;

        /// <summary>
        /// Gets the original object
        /// </summary>
        public T Object { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Payloadable{T}"/> class.
        /// </summary>
        /// <param name="payload"></param>
        public Payloadable(T payload)
        {
            Object = payload;
            _properties = typeof(T).GetProperties().ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public object this[string key] => _properties[key].GetValue(Object);

        /// <inheritdoc />
        public IEnumerable<string> Keys => _properties.Keys;

        /// <inheritdoc />
        public IEnumerable<object> Values => _properties.Values.Select(p => p.GetValue(Object));

        /// <inheritdoc />
        public int Count => _properties.Count;

        /// <inheritdoc />
        public bool ContainsKey(string key) => _properties.ContainsKey(key);

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            => _properties.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value.GetValue(Object))).GetEnumerator();

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value)
        {
            value = null;
            if (_properties.TryGetValue(key, out var prop))
            {
                value = prop.GetValue(Object);
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
