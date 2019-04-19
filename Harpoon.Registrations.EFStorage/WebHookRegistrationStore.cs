using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Harpoon.Registrations.EFStorage
{
    public class WebHookRegistrationStore<TContext> : IWebHookRegistrationStore, IWebHookStore
        where TContext : DbContext, IRegistrationsContext
    {
        private readonly TContext _context;
        private readonly IPrincipalIdGetter _idGetter;
        private readonly IDataProtector _dataProtector;
        private readonly ILogger<WebHookRegistrationStore<TContext>> _logger;

        public WebHookRegistrationStore(TContext context, IPrincipalIdGetter idGetter, IDataProtectionProvider dataProtectionProvider, ILogger<WebHookRegistrationStore<TContext>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _idGetter = idGetter ?? throw new ArgumentNullException(nameof(idGetter));
            _dataProtector = dataProtectionProvider?.CreateProtector(DataProtection.Purpose) ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<IWebHook>> GetAllWebHooksAsync(string action)
        {
            var webHooks = await _context.Registrations
                .Select(r => r.WebHook)
                .Where(w => !w.IsPaused && w.Filters.Any(f => f.ActionId == action))
                .Include(w => w.Filters)
                .AsNoTracking()
                .ToListAsync();

            foreach (var webHook in webHooks)
            {
                Prepare(webHook);
            }

            return webHooks;
        }

        public async Task<IWebHook> GetWebHookAsync(IPrincipal user, Guid id)
        {
            var key = await _idGetter.GetPrincipalIdForWebHookRegistrationAsync(user);
            var webHook = await _context.Registrations
                .Where(r => r.PrincipalId == key && r.WebHookId == id)
                .Select(r => r.WebHook)
                .Include(w => w.Filters)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (webHook == null)
            {
                return null;
            }

            Prepare(webHook);

            return webHook;
        }

        public async Task<IReadOnlyList<IWebHook>> GetWebHooksAsync(IPrincipal user)
        {
            var key = await _idGetter.GetPrincipalIdForWebHookRegistrationAsync(user);
            var webHooks = await _context.Registrations
                .Where(r => r.PrincipalId == key)
                .Select(r => r.WebHook)
                .Include(w => w.Filters)
                .AsNoTracking()
                .ToListAsync();

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

            webHook.Secret = _dataProtector.Unprotect(webHook.ProtectedSecret);
            webHook.Callback = new Uri(_dataProtector.Unprotect(webHook.ProtectedCallback));

            foreach (var filter in webHook.Filters)
            {
                filter.Parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(filter.ParametersJson);
            }
        }

        public async Task<WebHookRegistrationStoreResult> InsertWebHookAsync(IPrincipal user, IWebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            var key = await _idGetter.GetPrincipalIdForWebHookRegistrationAsync(user);
            var registration = new Registration
            {
                PrincipalId = key,
                WebHook = new WebHook
                {
                    Id = webHook.Id == default ? Guid.NewGuid() : webHook.Id,
                    ProtectedCallback = _dataProtector.Protect(webHook.Callback.ToString()),
                    ProtectedSecret = _dataProtector.Protect(webHook.Secret),
                    Filters = webHook.Filters.Select(f => new WebHookFilter
                    {
                        ActionId = f.ActionId,
                        ParametersJson = JsonConvert.SerializeObject(f.Parameters)
                    }).ToList()
                }
            };

            try
            {
                _context.Add(registration);
                await _context.SaveChangesAsync();
                return WebHookRegistrationStoreResult.Success;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"WebHook {registration.WebHook.Id} insertion failed : {e.Message}");
                return WebHookRegistrationStoreResult.InternalError;
            }
        }

        public async Task<WebHookRegistrationStoreResult> UpdateWebHookAsync(IPrincipal user, IWebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            var key = await _idGetter.GetPrincipalIdForWebHookRegistrationAsync(user);
            var dbWebHook = await _context.Registrations
                .Where(r => r.PrincipalId == key && r.WebHookId == webHook.Id)
                .Select(r => r.WebHook)
                .Include(w => w.Filters)
                .FirstOrDefaultAsync();

            if (dbWebHook == null)
            {
                return WebHookRegistrationStoreResult.NotFound;
            }

            dbWebHook.IsPaused = webHook.IsPaused;
            dbWebHook.ProtectedCallback = _dataProtector.Protect(webHook.Callback.ToString());
            dbWebHook.ProtectedSecret = _dataProtector.Protect(webHook.Secret);
            _context.RemoveRange(dbWebHook.Filters);
            dbWebHook.Filters = webHook.Filters.Select(f => new WebHookFilter
            {
                ActionId = f.ActionId,
                ParametersJson = JsonConvert.SerializeObject(f.Parameters)
            }).ToList();

            try
            {
                await _context.SaveChangesAsync();
                return WebHookRegistrationStoreResult.Success;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"WebHook {dbWebHook.Id} update failed : {e.Message}");
                return WebHookRegistrationStoreResult.InternalError;
            }
        }

        public async Task<WebHookRegistrationStoreResult> DeleteWebHookAsync(IPrincipal user, Guid id)
        {
            var key = await _idGetter.GetPrincipalIdForWebHookRegistrationAsync(user);
            var registration = await _context.Registrations.Where(r => r.PrincipalId == key && r.WebHookId == id).FirstOrDefaultAsync();

            if (registration == null)
            {
                return WebHookRegistrationStoreResult.NotFound;
            }

            _context.Remove(registration);
            try
            {
                await _context.SaveChangesAsync();
                return WebHookRegistrationStoreResult.Success;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"WebHook {id} deletion failed : {e.Message}");
                return WebHookRegistrationStoreResult.InternalError;
            }
        }

        public async Task DeleteWebHooksAsync(IPrincipal user)
        {
            var key = await _idGetter.GetPrincipalIdForWebHookRegistrationAsync(user);
            var registrations = await _context.Registrations.Where(r => r.PrincipalId == key).ToListAsync();

            if (registrations == null || registrations.Count == 0)
            {
                return;
            }

            try
            {
                _context.RemoveRange(registrations);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"WebHooks deletion from user {key} failed : {e.Message}");
                throw;
            }
        }
    }
}