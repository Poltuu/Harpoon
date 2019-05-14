using Harpoon.Controllers.Models;
using Harpoon.Registrations;
using Harpoon.Swashbuckle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harpoon.Controllers
{
    /// <summary>
    /// REST interface to manage WebHooks
    /// </summary>
    [Authorize]
    [Route("api/webhooks")]
    [ApiExplorerSettings(GroupName = "WebHooks")]
    public class WebHooksController : ControllerBase
    {
        /// <summary>
        /// Gets the name of the GetByIdAsyncActio
        /// </summary>
        public const string GetByIdAsyncActionName = "WebHooksController_GetByIdAsync";

        private readonly IWebHookRegistrationStore _webHookRegistrationStore;
        private readonly ILogger<WebHooksController> _logger;
        private readonly IWebHookValidator _webHookValidator;

        /// <summary>Initializes a new instance of the <see cref="WebHooksController"/> class.</summary>
        public WebHooksController(IWebHookRegistrationStore webHookRegistrationStore, ILogger<WebHooksController> logger, IWebHookValidator webHookValidator)
        {
            _webHookRegistrationStore = webHookRegistrationStore ?? throw new ArgumentNullException(nameof(webHookRegistrationStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webHookValidator = webHookValidator ?? throw new ArgumentNullException(nameof(webHookValidator));
        }

        /// <summary>
        /// Gets all webHooks belonging to the current user.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<IWebHook>>> GetAsync()
        {
            return Ok(await _webHookRegistrationStore.GetWebHooksAsync(User, HttpContext.RequestAborted));
        }

        /// <summary>
        /// Gets the webHook belonging to the current user with the given <paramref name="id"/>.
        /// </summary>
        [HttpGet("{id}", Name = GetByIdAsyncActionName)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IWebHook>> GetByIdAsync(Guid id)
        {
            var webHook = await _webHookRegistrationStore.GetWebHookAsync(User, id, HttpContext.RequestAborted);
            if (webHook == null)
            {
                return NotFound();
            }
            return Ok(webHook);
        }

        /// <summary>
        /// Registers a new webHook for the current User
        /// </summary>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [WebHookSubscriptionPoint]
        public async Task<ActionResult> PostAsync([FromBody]WebHookDTO webHook)
        {
            if (webHook == null)
            {
                return BadRequest("Body is missing");
            }

            try
            {
                await _webHookValidator.ValidateAsync(webHook, HttpContext.RequestAborted);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("New webhook validation failed: " + ex.Message);
                return BadRequest(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("New webhook validation unexpected failure: " + ex.Message);
                return StatusCode(500);
            }

            try
            {
                var result = await _webHookRegistrationStore.InsertWebHookAsync(User, webHook, HttpContext.RequestAborted);
                if (result == WebHookRegistrationStoreResult.Success)
                {
                    return CreatedAtRoute(GetByIdAsyncActionName, new { id = webHook.Id }, webHook);
                }
                return GetActionFromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Registration insertion unexpected failure: " + ex.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Updates an existing webHook.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutAsync(Guid id, [FromBody]WebHookDTO webHook)
        {
            if (webHook == null)
            {
                return BadRequest("Body is missing");
            }

            if (webHook.Id != id)
            {
                return BadRequest("Id mismatch");
            }

            try
            {
                await _webHookValidator.ValidateAsync(webHook, HttpContext.RequestAborted);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation($"Webhook {id} update validation failed: " + ex.Message);
                return BadRequest(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook {id} update validation unexpected failure: " + ex.Message);
                return StatusCode(500);
            }

            try
            {
                var result = await _webHookRegistrationStore.UpdateWebHookAsync(User, webHook, HttpContext.RequestAborted);
                return GetActionFromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook {id} modification unexpected failure: " + ex.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Deletes an existing webhook.
        /// </summary>
        [Route("{id}")]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _webHookRegistrationStore.DeleteWebHookAsync(User, id, HttpContext.RequestAborted);
                return GetActionFromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook {id} deletion unexpected failure: " + ex.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Deletes all webhooks for current user.
        /// </summary>
        [Route("")]
        public async Task<IActionResult> DeleteAsync()
        {
            try
            {
                await _webHookRegistrationStore.DeleteWebHooksAsync(User, HttpContext.RequestAborted);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhooks deletion unexpected failure: " + ex.Message);
                return StatusCode(500);
            }
        }

        private ActionResult GetActionFromResult(WebHookRegistrationStoreResult result)
        {
            switch (result)
            {
                case WebHookRegistrationStoreResult.Success: return Ok();
                case WebHookRegistrationStoreResult.NotFound: return NotFound();
                default: return StatusCode(500);
            }
        }
    }
}