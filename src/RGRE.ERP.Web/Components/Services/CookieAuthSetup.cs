using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Organization;
using RGRE.ERP.Domain.Security;

namespace RGRE.ERP.Web.Components.Services;

/// <summary>
/// Configures the cookie scheme and validates the principal on every request:
/// the persisted ticket must exist and be current, the user must still exist
/// and be enabled, and the permission/context claims are refreshed from the
/// database so role or context changes apply without a re-login.
/// </summary>
public sealed class CookieAuthSetup : IConfigureNamedOptions<CookieAuthenticationOptions>
{
    public static readonly TimeSpan TicketLifetime = TimeSpan.FromHours(12);

    private readonly IServiceScopeFactory _scopeFactory;

    public CookieAuthSetup(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Configure(CookieAuthenticationOptions options)
    {
    }

    public void Configure(string? name, CookieAuthenticationOptions options)
    {
        if (name != CookieAuthenticationDefaults.AuthenticationScheme)
            return;

        options.Cookie.Name = ".RGRE.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TicketLifetime;
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Events.OnValidatePrincipal = ValidatePrincipalAsync;
    }

    private async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebAuthDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var companies = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
        var branches = scope.ServiceProvider.GetRequiredService<IBranchRepository>();
        var fiscalYears = scope.ServiceProvider.GetRequiredService<IFiscalYearRepository>();

        var userIdString = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            context.RejectPrincipal();
            return;
        }

        var ticket = await db.AuthTickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId, context.HttpContext.RequestAborted);

        if (ticket is null || ticket.ExpiresUtc < DateTime.UtcNow)
        {
            context.RejectPrincipal();
            return;
        }

        var user = await users.GetAsync(userId, context.HttpContext.RequestAborted);

        if (user is null || !user.Enabled)
        {
            await db.RemoveTicketAsync(userId, context.HttpContext.RequestAborted);
            context.RejectPrincipal();
            return;
        }

        var (companyId, branchId, fiscalYearId) = await ResolveContextAsync(
            context.Principal!, companies, branches, fiscalYears, context.HttpContext.RequestAborted);

        var principal = await ErpClaimsPrincipalFactory.CreateAsync(
            user, roles, companyId, branchId, fiscalYearId, context.HttpContext.RequestAborted);

        context.ReplacePrincipal(principal);
        context.ShouldRenew = true;

        var expires = context.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(TicketLifetime);
        await db.PersistTicketAsync(userId, principal, expires, context.HttpContext.RequestAborted);
    }

    private static async Task<(Guid?, Guid?, Guid?)> ResolveContextAsync(
        ClaimsPrincipal principal,
        ICompanyRepository companies,
        IBranchRepository branches,
        IFiscalYearRepository fiscalYears,
        CancellationToken cancellationToken)
    {
        Guid? companyId = Guid.TryParse(
            principal.FindFirstValue(CurrentUserContext.CompanyClaim), out var c) ? c : null;
        Guid? branchId = Guid.TryParse(
            principal.FindFirstValue(CurrentUserContext.BranchClaim), out var b) ? b : null;
        Guid? fiscalYearId = Guid.TryParse(
            principal.FindFirstValue(CurrentUserContext.FiscalYearClaim), out var f) ? f : null;

        if (companyId.HasValue)
        {
            var company = await companies.GetAsync(companyId.Value, cancellationToken);

            if (company is null || !company.Enabled)
            {
                companyId = null;
                branchId = null;
            }
        }

        if (branchId.HasValue)
        {
            var branch = await branches.GetAsync(branchId.Value, cancellationToken);

            if (branch is null || !branch.Enabled || (companyId.HasValue && branch.CompanyId != companyId))
                branchId = null;
        }

        if (fiscalYearId.HasValue)
        {
            var fy = await fiscalYears.GetAsync(fiscalYearId.Value, cancellationToken);

            if (fy is null || fy.IsClosed)
                fiscalYearId = null;
        }

        return (companyId, branchId, fiscalYearId);
    }
}
