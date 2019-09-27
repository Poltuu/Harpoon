using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// Default <see cref="IWebHookRegistrationStore"/> implementation using EF
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public class WebHookRegistrationStore<TContext> : IWebHookRegistrationStore
        where TContext : DbContext, IRegistrationsContext
    {
        private readonly TContext _context;
        private readonly IPrincipalIdGetter _idGetter;
        private readonly ISecretProtector _secretProtector;
        private readonly ILogger<WebHookRegistrationStore<TContext>> _logger;

        /// <summary>Initializes a new instance of the <see cref="WebHookRegistrationStore{TContext}"/> class.</summary>
        public WebHookRegistrationStore(TContext context, IPrincipalIdGetter idGetter, ISecretProtector secretProtector, ILogger<WebHookRegistrationStore<TContext>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _idGetter = idGetter ?? throw new ArgumentNullException(nameof(idGetter));
            _secretProtector = secretProtector ?? throw new ArgumentNullException(nameof(secretProtector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IWebHook> GetWebHookAsync(IPrincipal user, Guid id, CancellationToken cancellationToken = default)
        {
            var key = await _idGetter.GetPrincipalIdAsync(user, cancellationToken);
            var webHook = await _context.WebHooks
                .Where(w => w.PrincipalId == key && w.Id == id)
                .Include(w => w.Filters)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (webHook == null)
            {
                return null;
            }

            Prepare(webHook);

            return webHook;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IWebHook>> GetWebHooksAsync(IPrincipal user, CancellationToken cancellationToken = default)
        {
            var key = await _idGetter.GetPrincipalIdAsync(user, cancellationToken);
            var webHooks = await _context.WebHooks
                .Where(w => w.PrincipalId == key)
                .Include(w => w.Filters)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var webHook in webHooks)
            {
                Prepare(webHook);
            }

            return webHooks;
        }

        private void Prepare(WebHook webHook)
        {
            if (webHook == null)
            {
                return;
            }

            webHook.Secret = _secretProtector.Unprotect(webHook.ProtectedSecret);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Id, callback, secret or filters is <see langword="null" /></exception>
        public async Task<WebHookRegistrationStoreResult> InsertWebHookAsync(IPrincipal user, IWebHook webHook, CancellationToken cancellationToken = default)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            if (webHook.Id == default)
            {
                throw new ArgumentException("WebHook id needs to be set by client.");
            }

            if (webHook.Callback == null)
            {
                throw new ArgumentException("WebHook callback needs to be set.");
            }

            if (webHook.Secret == null)
            {
                throw new ArgumentException("WebHook secret needs to be set.");
            }

            if (webHook.Filters == null)
            {
                throw new ArgumentException("WebHook filters needs to be set.");
            }

            var key = await _idGetter.GetPrincipalIdAsync(user, cancellationToken);
            var dbWebHook = new WebHook
            {
                Id = webHook.Id,
                PrincipalId = key,
                Callback = webHook.Callback,
                ProtectedSecret = _secretProtector.Protect(webHook.Secret),
                Filters = webHook.Filters.Select(f => new WebHookFilter { Trigger = f.Trigger }).ToList()
            };

            try
            {
                _context.Add(dbWebHook);
                await _context.SaveChangesAsync(cancellationToken);
                return WebHookRegistrationStoreResult.Success;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"WebHook {dbWebHook.Id} insertion failed : {e.Message}");
                return WebHookRegistrationStoreResult.InternalError;
            }
        }

        /// <inheritdoc />
        public async Task<WebHookRegistrationStoreResult> UpdateWebHookAsync(IPrincipal user, IWebHook webHook, CancellationToken cancellationToken = default)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            var key = await _idGetter.GetPrincipalIdAsync(user, cancellationToken);
            var dbWebHook = await _context.WebHooks
                .Where(w => w.PrincipalId == key && w.Id == webHook.Id)
                .Include(w => w.Filters)
                .FirstOrDefaultAsync(cancellationToken);

            if (dbWebHook == null)
            {
                return WebHookRegistrationStoreResult.NotFound;
            }

            dbWebHook.IsPaused = webHook.IsPaused;

            if (webHook.Callback != null)
            {
                dbWebHook.Callback = webHook.Callback;
            }

            if (!string.IsNullOrEmpty(webHook.Secret))
            {
                dbWebHook.ProtectedSecret = _secretProtector.Protect(webHook.Secret);
            }

            if (webHook.Filters != null)
            {
                _context.RemoveRange(dbWebHook.Filters);
                dbWebHook.Filters = webHook.Filters.Select(f => new WebHookFilter { Trigger = f.Trigger }).ToList();
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return WebHookRegistrationStoreResult.Success;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"WebHook {dbWebHook.Id} update failed : {e.Message}");
                return WebHookRegistrationStoreResult.InternalError;
            }
        }

        /// <inheritdoc />
        public async Task<WebHookRegistrationStoreResult> DeleteWebHookAsync(IPrincipal user, Guid id, CancellationToken cancellationToken = default)
        {
            var key = await _idGetter.GetPrincipalIdAsync(user, cancellationToken);
            var webHook = await _context.WebHooks.Where(w => w.PrincipalId == key && w.Id == id).FirstOrDefaultAsync(cancellationToken);

            if (webHook == null)
            {
                return WebHookRegistrationStoreResult.NotFound;
            }

            _context.Remove(webHook);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return WebHookRegistrationStoreResult.Success;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"WebHook {id} deletion failed : {e.Message}");
                return WebHookRegistrationStoreResult.InternalError;
            }
        }

        /// <inheritdoc />
        public async Task DeleteWebHooksAsync(IPrincipal user, CancellationToken cancellationToken = default)
        {
            var key = await _idGetter.GetPrincipalIdAsync(user, cancellationToken);
            var webHooks = await _context.WebHooks.Where(r => r.PrincipalId == key).ToListAsync(cancellationToken);

            if (webHooks == null || webHooks.Count == 0)
            {
                return;
            }

            try
            {
                _context.RemoveRange(webHooks);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"WebHooks deletion from user {key} failed : {e.Message}");
                throw;
            }
        }
    }
}