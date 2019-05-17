using Harpoon.Registrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Harpoon.Controllers
{
    /// <summary>
    /// <see cref="WebHookTriggersController"/> allows the caller to get the list of triggers available for <see cref="IWebHook"/> registration.
    /// </summary>
    [Authorize, ApiController, Route("api/webhooks/triggers"), ApiExplorerSettings(GroupName = OpenApi.GroupName), Produces("application/json")]
    public class WebHookTriggersController : ControllerBase
    {
        private readonly IWebHookTriggerProvider _webHookTriggerProvider;

        /// <summary>Initializes a new instance of the <see cref="WebHookTriggersController"/> class.</summary>
        public WebHookTriggersController(IWebHookTriggerProvider webHookTriggerProvider)
        {
            _webHookTriggerProvider = webHookTriggerProvider ?? throw new ArgumentNullException(nameof(webHookTriggerProvider));
        }

        /// <summary>
        /// Returns available <see cref="WebHookTrigger"/> for <see cref="IWebHook"/> registration.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<IEnumerable<WebHookTrigger>> Get()
        {
            return Ok(_webHookTriggerProvider.GetAvailableTriggers().Values);
        }
    }
}