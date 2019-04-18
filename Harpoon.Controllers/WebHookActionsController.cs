using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harpoon.Controllers
{
    [Authorize]
    [Route("api/webhooks/actions")]
    [ApiExplorerSettings(GroupName = "WebHooks")]
    public class WebHookActionsController : ControllerBase
    {
        IWebHookActionProvider _webHookActionProvider;

        public WebHookActionsController(IWebHookActionProvider webHookActionProvider)
        {
            _webHookActionProvider = webHookActionProvider ?? throw new ArgumentNullException(nameof(webHookActionProvider));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebHookAction>>> GetAsync()
        {
            return Ok((await _webHookActionProvider.GetAvailableActionsAsync()).Values);
        }
    }
}