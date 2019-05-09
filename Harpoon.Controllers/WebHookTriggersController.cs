using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harpoon.Controllers
{
    [Authorize]
    [Route("api/webhooks/triggers")]
    [ApiExplorerSettings(GroupName = "WebHooks")]
    public class WebHookTriggersController : ControllerBase
    {
        private readonly IWebHookTriggerProvider _webHookTriggerProvider;

        public WebHookTriggersController(IWebHookTriggerProvider webHookTriggerProvider)
        {
            _webHookTriggerProvider = webHookTriggerProvider ?? throw new ArgumentNullException(nameof(webHookTriggerProvider));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebHookTrigger>>> GetAsync()
        {
            return Ok((await _webHookTriggerProvider.GetAvailableTriggersAsync()).Values);
        }
    }
}