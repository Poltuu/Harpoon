using System;
using System.Collections.Generic;

namespace Harpoon
{
    public interface IWebHook
    {
        Guid Id { get; set; }

        Uri Callback { get; set; }

        string Secret { get; set; }

        bool IsPaused { get; set; }

        IReadOnlyCollection<IWebHookFilter> Filters { get; }
    }
}
