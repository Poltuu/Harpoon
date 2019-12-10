using Harpoon.Controllers.Models;
using Harpoon.Controllers.Swashbuckle;
using Harpoon.Registrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Harpoon.Controllers
{
    /// <summary>
    /// REST interface to manage WebHooks
    /// </summary>
    [Authorize, ApiController, Route("api/webhooks"), ApiExplorerSettings(GroupName = OpenApi.GroupName), Produces("application/json")]
    public class WebHooksController : ControllerBase
    {
        /// <summary>
        /// Gets the name of the GetByIdAsyncAction
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
        /// Gets all WebHooks belonging to the current user.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebHook>>> GetAsync()
            => Ok((await _webHookRegistrationStore.GetWebHooksAsync(User, HttpContext.RequestAborted)).Select(w => new WebHook(w)));

        /// <summary>
        /// Gets the WebHook belonging to the current user with the given <paramref name="id"/>.
        /// </summary>
        [HttpGet("{id}", Name = GetByIdAsyncActionName)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<WebHook>> GetByIdAsync(Guid id)
        {
            var webHook = await _webHookRegistrationStore.GetWebHookAsync(User, id, HttpContext.RequestAborted);
            if (webHook == null)
            {
                return NotFound();
            }
            return Ok(new WebHook(webHook));
        }

        /// <summary>
        /// Registers a new WebHook for the current user
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [WebHookSubscriptionPoint]
        public async Task<ActionResult> PostAsync([FromBody]WebHook webHook)
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
                var message = $"New webhook validation failed: {ex.Message}";
                _logger.LogInformation(message);
                return BadRequest(new { Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"New webhook validation unexpected failure: {ex.Message}");
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
                _logger.LogError($"Registration insertion unexpected failure: {ex.Message}");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Updates the WebHook with the given <paramref name="id"/>.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutAsync(Guid id, [FromBody]WebHook webHook)
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
                var message = $"Webhook {id} update validation failed: {ex.Message}";
                _logger.LogInformation(message);
                return BadRequest(new { Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook {id} update validation unexpected failure: {ex.Message}");
                return StatusCode(500);
            }

            try
            {
                var result = await _webHookRegistrationStore.UpdateWebHookAsync(User, webHook, HttpContext.RequestAborted);
                return GetActionFromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook {id} modification unexpected failure: {ex.Message}");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Deletes the WebHook with the given <paramref name="id"/>.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteByIdAsync(Guid id)
        {
            try
            {
                var result = await _webHookRegistrationStore.DeleteWebHookAsync(User, id, HttpContext.RequestAborted);
                return GetActionFromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook {id} deletion unexpected failure: {ex.Message}");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Deletes all WebHooks of the current user.
        /// </summary>
        [HttpDelete()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAsync()
        {
            try
            {
                await _webHookRegistrationStore.DeleteWebHooksAsync(User, HttpContext.RequestAborted);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhooks deletion unexpected failure: {ex.Message}");
                return StatusCode(500);
            }
        }

        private ActionResult GetActionFromResult(WebHookRegistrationStoreResult result) => result switch
        {
            WebHookRegistrationStoreResult.Success => Ok(),
            WebHookRegistrationStoreResult.NotFound => NotFound(),
            _ => StatusCode(500),
        };
    }
}