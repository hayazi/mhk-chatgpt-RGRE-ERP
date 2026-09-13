using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;

namespace RGRE.ERP.Web.Components.Services;

/// <summary>
/// Authentication state provider for Blazor Server with cookie auth.
/// The principal is captured during the initial HTTP request (prerender) and
/// handed to the interactive circuit through <see cref="PersistentComponentState"/>
/// — the same mechanism the .NET template uses — so callbacks after prerender
/// still see the signed-in user (HttpContext is null inside a circuit).
/// </summary>
public sealed class PersistingServerAuthenticationStateProvider
    : AuthenticationStateProvider, IDisposable
{
    private const string PersistKey = "authstate";

    private readonly PersistentComponentState _persistentState;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private ClaimsPrincipal? _circuitUser;
    private PersistingComponentStateSubscription? _subscription;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState persistentState,
        IHttpContextAccessor httpContextAccessor)
    {
        _persistentState = persistentState;
        _httpContextAccessor = httpContextAccessor;

        _subscription = _persistentState.RegisterOnPersisting(OnPersisting, RenderMode.InteractiveServer);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_circuitUser is not null)
            return Task.FromResult(new AuthenticationState(_circuitUser));

        var httpUser = _httpContextAccessor.HttpContext?.User;

        if (httpUser is not null)
            return Task.FromResult(new AuthenticationState(httpUser));

        // Circuit mode after prerender: restore from persisted state.
        if (_persistentState.TryTakeFromJson<AuthTicketRecord>(PersistKey, out var record)
            && record is not null)
        {
            _circuitUser = record.ToPrincipal();
            return Task.FromResult(new AuthenticationState(_circuitUser));
        }

        return Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private Task OnPersisting()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated == true)
            _persistentState.PersistAsJson(PersistKey, AuthTicketRecord.FromPrincipal(user));

        return Task.CompletedTask;
    }

    public void Dispose() => _subscription?.Dispose();
}
