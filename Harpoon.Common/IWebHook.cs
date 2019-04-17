using System;
using System.Collections.Generic;

namespace Harpoon
{
    public interface IWebHook
    {
        Guid Id { get; }

        Uri Callback { get; }

        string Secret { get; }

        IReadOnlyCollection<IWebHookFilter> Filters { get; }
    }
}
