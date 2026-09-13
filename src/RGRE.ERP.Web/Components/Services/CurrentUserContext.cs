using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using RGRE.ERP.Application.Abstractions;

namespace RGRE.ERP.Web.Components.Services;

/// <summary>
/// The signed-in user + working context, resolved from the auth cookie.
/// Works in both hosting modes: minimal-API requests read the HttpContext
/// directly, Blazor circuits fall back to the AuthenticationStateProvider.
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    public const string PermissionClaim = "erp:permission";
    public const string CompanyClaim = "erp:company";
    public const string BranchClaim = "erp:branch";
    public const string FiscalYearClaim = "erp:fiscalYear";
    public const string DisplayNameClaim = "erp:displayName";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    private ClaimsPrincipal? _circuitUser;
    private bool _circuitUserLoaded;

    public CurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <summary>
    /// Warms the circuit auth state; MainLayout calls this once per page load
    /// so later synchronous reads never see an unfinished task.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = cancellationToken;
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            _circuitUser = state.User;
            _circuitUserLoaded = true;
        }
        catch (Exception)
        {
            _circuitUser = null;
            _circuitUserLoaded = true;
        }
    }

    private ClaimsPrincipal Principal
    {
        get
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;

            if (httpUser?.Identity?.IsAuthenticated == true)
                return httpUser;

            if (!_circuitUserLoaded)
            {
                var task = _authenticationStateProvider.GetAuthenticationStateAsync();

                _circuitUser = task.IsCompletedSuccessfully
                    ? task.Result.User
                    : null;
                _circuitUserLoaded = true;
            }

            return _circuitUser ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public Guid? UserId
        => Guid.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => Principal.Identity?.Name;

    public string? DisplayName
        => Principal.FindFirstValue(DisplayNameClaim) ?? Principal.Identity?.Name;

    public Guid? CompanyId
        => Guid.TryParse(Principal.FindFirstValue(CompanyClaim), out var id) ? id : null;

    public Guid? BranchId
        => Guid.TryParse(Principal.FindFirstValue(BranchClaim), out var id) ? id : null;

    public Guid? FiscalYearId
        => Guid.TryParse(Principal.FindFirstValue(FiscalYearClaim), out var id) ? id : null;

    public bool HasPermission(string permission)
        => Principal.HasClaim(PermissionClaim, permission);
}
