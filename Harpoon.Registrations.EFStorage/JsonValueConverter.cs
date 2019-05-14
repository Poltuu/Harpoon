using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace Microsoft.EntityFrameworkCore
{
    internal class JsonValueConverter<T> : ValueConverter<T, string>
    {
        public JsonValueConverter()
            : base(d => JsonConvert.SerializeObject(d), s => JsonConvert.DeserializeObject<T>(s))
        {
        }
    }
}
