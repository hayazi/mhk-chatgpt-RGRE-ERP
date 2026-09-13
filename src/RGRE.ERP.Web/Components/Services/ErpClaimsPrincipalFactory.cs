using System.Security.Claims;
using RGRE.ERP.Domain.Security;

namespace RGRE.ERP.Web.Components.Services;

/// <summary>
/// Builds the cookie principal: identity claims, one claim per permission
/// from the user's roles, and the working-context claims (company / branch /
/// fiscal year) that <see cref="CurrentUserContext"/> reads.
/// </summary>
public static class ErpClaimsPrincipalFactory
{
    /// <summary>
    /// Resolves the user's roles (permission data lives on <see cref="Role"/>,
    /// not on the <see cref="UserRole"/> link) and builds the principal.
    /// </summary>
    public static async Task<ClaimsPrincipal> CreateAsync(
        User user,
        IRoleRepository roles,
        Guid? companyId,
        Guid? branchId,
        Guid? fiscalYearId,
        CancellationToken cancellationToken = default)
    {
        var roleEntities = new List<Role>();

        foreach (var link in user.Roles)
        {
            var role = await roles.GetAsync(link.RoleId, cancellationToken);

            if (role is not null)
                roleEntities.Add(role);
        }

        return Create(user, roleEntities, companyId, branchId, fiscalYearId);
    }

    public static ClaimsPrincipal Create(
        User user,
        IReadOnlyCollection<Role> roles,
        Guid? companyId,
        Guid? branchId,
        Guid? fiscalYearId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(CurrentUserContext.DisplayNameClaim, user.DisplayName),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role.Name));

        var permissions = roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Permission)
            .Distinct();

        foreach (var permission in permissions)
            claims.Add(new Claim(CurrentUserContext.PermissionClaim, permission));

        if (companyId.HasValue)
            claims.Add(new Claim(CurrentUserContext.CompanyClaim, companyId.Value.ToString()));

        if (branchId.HasValue)
            claims.Add(new Claim(CurrentUserContext.BranchClaim, branchId.Value.ToString()));

        if (fiscalYearId.HasValue)
            claims.Add(new Claim(CurrentUserContext.FiscalYearClaim, fiscalYearId.Value.ToString()));

        var identity = new ClaimsIdentity(
            claims,
            "RGRE.Cookie",
            ClaimTypes.Name,
            ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }
}
