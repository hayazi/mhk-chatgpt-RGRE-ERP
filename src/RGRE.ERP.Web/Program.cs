using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Radzen;
using Radzen.Blazor;
using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Application.Accounting;
using RGRE.ERP.Application.Inventory;
using RGRE.ERP.Application.Inventory.Issues;
using RGRE.ERP.Application.Inventory.Receipts;
using RGRE.ERP.Application.Inventory.Transfers;
using RGRE.ERP.Application.Partners;
using RGRE.ERP.Application.Platform;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Organization;
using RGRE.ERP.Domain.Security;
using RGRE.ERP.Infrastructure;
using RGRE.ERP.Infrastructure.Persistence;
using RGRE.ERP.Web;
using RGRE.ERP.Web.Components;
using RGRE.ERP.Web.Components.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddErp(builder.Configuration);
builder.Services.AddOpenApi();

// Blazor Server (interactive server-side rendering).
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

// ----- Authentication (cookie) -----

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, CookieAuthSetup>();

builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<ICurrentUserContext>(sp => sp.GetRequiredService<CurrentUserContext>());

builder.Services.AddScoped<UiLookups>();

// Persisted auth-ticket store (separate context, same SQL Server database).
builder.Services.AddDbContext<WebAuthDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ErpDatabase")
        ?? "Data Source=localhost;Initial Catalog=RGREERP;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True"));

var app = builder.Build();

// Auto-migrate + seed on startup (SQL Server demo database).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);

    var authDb = scope.ServiceProvider.GetRequiredService<WebAuthDbContext>();
    authDb.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ===================== Authentication =====================

app.MapGet("/account/login", (string? returnUrl) =>
        Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/login" : $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}"))
    .WithTags("Auth").ExcludeFromDescription();

app.MapPost("/account/login", async (
    HttpContext http,
    [FromForm] LoginForm form,
    IAuthService auth,
    IUserRepository users,
    IRoleRepository roles,
    WebAuthDbContext authDb,
    CancellationToken ct) =>
{
    var result = await auth.LoginAsync(form.Username, form.Password, ct);

    if (!result.Success || result.UserId is null)
        return Results.Redirect($"/login?error={Uri.EscapeDataString(result.Error ?? "Login failed.")}");

    var user = await users.GetAsync(result.UserId.Value, ct);

    if (user is null)
        return Results.Redirect("/login?error=User+not+found.");

    var principal = await ErpClaimsPrincipalFactory.CreateAsync(
        user,
        roles,
        result.LastCompanyId,
        result.LastBranchId,
        result.LastFiscalYearId,
        ct);

    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    var expires = DateTimeOffset.UtcNow.Add(CookieAuthSetup.TicketLifetime);
    await authDb.PersistTicketAsync(user.Id, principal, expires, ct);

    return Results.LocalRedirect(
        string.IsNullOrWhiteSpace(form.ReturnUrl) || !form.ReturnUrl.StartsWith('/') ? "/" : form.ReturnUrl);
}).WithTags("Auth").ExcludeFromDescription();

app.MapPost("/account/logout", async (HttpContext http, WebAuthDbContext authDb, CancellationToken ct) =>
{
    var userId = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

    if (Guid.TryParse(userId?.Value, out var uid))
        await authDb.RemoveTicketAsync(uid, ct);

    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
})
// Posted from plain HTML forms inside interactive circuits, where the
// <AntiforgeryToken> component renders nothing. Logout is low-risk CSRF.
.DisableAntiforgery().WithTags("Auth").ExcludeFromDescription();

app.MapPost("/account/switch-context", async (
    HttpContext http,
    [FromForm] SwitchContextForm form,
    IUserRepository users,
    IRoleRepository roles,
    ICompanyRepository companies,
    IBranchRepository branches,
    IFiscalYearRepository fiscalYears,
    WebAuthDbContext authDb,
    CancellationToken ct) =>
{
    if (http.User.Identity?.IsAuthenticated != true)
        return Results.Redirect("/login");

    var userIdString = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    if (!Guid.TryParse(userIdString, out var userId))
        return Results.Redirect("/login");

    var user = await users.GetAsync(userId, ct);

    if (user is null)
        return Results.Redirect("/login");

    var companyId = Guid.TryParse(form.CompanyId, out var c) ? c : user.LastCompanyId;
    var branchId = Guid.TryParse(form.BranchId, out var b) ? b : user.LastBranchId;
    var fiscalYearId = Guid.TryParse(form.FiscalYearId, out var f) ? f : user.LastFiscalYearId;

    if (companyId.HasValue && (await companies.GetAsync(companyId.Value, ct)) is null)
        companyId = null;

    if (branchId.HasValue && (await branches.GetAsync(branchId.Value, ct)) is null)
        branchId = null;

    if (fiscalYearId.HasValue)
    {
        var fy = await fiscalYears.GetAsync(fiscalYearId.Value, ct);
        if (fy is null || fy.IsClosed)
            fiscalYearId = null;
    }

    var principal = await ErpClaimsPrincipalFactory.CreateAsync(
        user, roles, companyId, branchId, fiscalYearId, ct);

    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    var expires = DateTimeOffset.UtcNow.Add(CookieAuthSetup.TicketLifetime);
    await authDb.PersistTicketAsync(userId, principal, expires, ct);

    return Results.LocalRedirect("/");
})
// Posted from the sidebar form inside interactive circuits (no rendered
// antiforgery token); it only changes the caller's own working context.
.DisableAntiforgery().WithTags("Auth").ExcludeFromDescription();

// ===================== Inventory: master data =====================

var units = app.MapGroup("/api/units").WithTags("Units");

units.MapGet("", async (IUnitAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

units.MapPost("", async (CreateUnitDto input, IUnitAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/units/{id}", new { id });
});

var goods = app.MapGroup("/api/goods").WithTags("Goods");

goods.MapGet("", async (IGoodsAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

goods.MapPost("", async (CreateGoodsDto input, IGoodsAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/goods/{id}", new { id });
});

goods.MapPost("/{id:guid}/disable", async (Guid id, IGoodsAppService service, CancellationToken ct) =>
{
    await service.DisableAsync(id, ct);
    return Results.NoContent();
});

goods.MapPost("/{id:guid}/enable", async (Guid id, IGoodsAppService service, CancellationToken ct) =>
{
    await service.EnableAsync(id, ct);
    return Results.NoContent();
});

var warehouses = app.MapGroup("/api/warehouses").WithTags("Warehouses");

warehouses.MapGet("", async (IWarehouseAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

warehouses.MapPost("", async (CreateWarehouseDto input, IWarehouseAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/warehouses/{id}", new { id });
});

// ===================== Inventory: stock queries =====================

app.MapGet("/api/stock/{warehouseId:guid}", async (
    Guid warehouseId,
    IStockQueryService service,
    CancellationToken ct) =>
    Results.Ok(await service.GetByWarehouseAsync(warehouseId, ct)))
    .WithTags("Stock");

app.MapGet("/api/stock-moves/{goodsId:guid}", async (
    Guid goodsId,
    IStockQueryService service,
    CancellationToken ct) =>
    Results.Ok(await service.GetMovesAsync(goodsId, ct)))
    .WithTags("Stock");

// ===================== Inventory: receipts (Resid) =====================

var receipts = app.MapGroup("/api/receipts").WithTags("Receipts");

receipts.MapGet("", async (IReceiptAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

receipts.MapGet("/{id:guid}", async (
    Guid id, IReceiptAppService service, CancellationToken ct) =>
{
    var receipt = await service.GetAsync(id, ct);
    return receipt is null ? Results.NotFound() : Results.Ok(receipt);
});

receipts.MapPost("", async (CreateReceiptDto input, IReceiptAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/receipts/{id}", new { id });
});

receipts.MapPost("/{id:guid}/lines", async (
    Guid id, AddReceiptLineDto input, IReceiptAppService service, CancellationToken ct) =>
{
    await service.AddLineAsync(id, input, ct);
    return Results.NoContent();
});

receipts.MapDelete("/{id:guid}/lines/{lineId:guid}", async (
    Guid id, Guid lineId, IReceiptAppService service, CancellationToken ct) =>
{
    await service.RemoveLineAsync(id, lineId, ct);
    return Results.NoContent();
});

receipts.MapPost("/{id:guid}/post", async (Guid id, IReceiptAppService service, CancellationToken ct) =>
{
    await service.PostAsync(id, ct);
    return Results.NoContent();
});

receipts.MapPost("/{id:guid}/cancel", async (Guid id, IReceiptAppService service, CancellationToken ct) =>
{
    await service.CancelAsync(id, ct);
    return Results.NoContent();
});

// ===================== Inventory: issues (Khorooj) =====================

var issues = app.MapGroup("/api/issues").WithTags("Issues");

issues.MapGet("", async (IIssueAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

issues.MapGet("/{id:guid}", async (
    Guid id, IIssueAppService service, CancellationToken ct) =>
{
    var issue = await service.GetAsync(id, ct);
    return issue is null ? Results.NotFound() : Results.Ok(issue);
});

issues.MapPost("", async (CreateIssueDto input, IIssueAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/issues/{id}", new { id });
});

issues.MapPost("/{id:guid}/lines", async (
    Guid id, AddIssueLineDto input, IIssueAppService service, CancellationToken ct) =>
{
    await service.AddLineAsync(id, input, ct);
    return Results.NoContent();
});

issues.MapDelete("/{id:guid}/lines/{lineId:guid}", async (
    Guid id, Guid lineId, IIssueAppService service, CancellationToken ct) =>
{
    await service.RemoveLineAsync(id, lineId, ct);
    return Results.NoContent();
});

issues.MapPost("/{id:guid}/post", async (Guid id, IIssueAppService service, CancellationToken ct) =>
{
    await service.PostAsync(id, ct);
    return Results.NoContent();
});

issues.MapPost("/{id:guid}/cancel", async (Guid id, IIssueAppService service, CancellationToken ct) =>
{
    await service.CancelAsync(id, ct);
    return Results.NoContent();
});

// ===================== Inventory: transfers (EnteghalBeinAnbar) =====================

var transfers = app.MapGroup("/api/transfers").WithTags("Transfers");

transfers.MapGet("", async (IStockTransferAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

transfers.MapGet("/{id:guid}", async (
    Guid id, IStockTransferAppService service, CancellationToken ct) =>
{
    var transfer = await service.GetAsync(id, ct);
    return transfer is null ? Results.NotFound() : Results.Ok(transfer);
});

transfers.MapPost("", async (CreateStockTransferDto input, IStockTransferAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/transfers/{id}", new { id });
});

transfers.MapPost("/{id:guid}/lines", async (
    Guid id, AddStockTransferLineDto input, IStockTransferAppService service, CancellationToken ct) =>
{
    await service.AddLineAsync(id, input, ct);
    return Results.NoContent();
});

transfers.MapPost("/{id:guid}/post", async (Guid id, IStockTransferAppService service, CancellationToken ct) =>
{
    await service.PostAsync(id, ct);
    return Results.NoContent();
});

transfers.MapPost("/{id:guid}/cancel", async (Guid id, IStockTransferAppService service, CancellationToken ct) =>
{
    await service.CancelAsync(id, ct);
    return Results.NoContent();
});

// ===================== Partners =====================

var partners = app.MapGroup("/api/partners").WithTags("Partners");

partners.MapGet("", async (IPartnerAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

partners.MapPost("", async (CreatePartnerDto input, IPartnerAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/partners/{id}", new { id });
});

partners.MapPost("/{id:guid}/block", async (Guid id, IPartnerAppService service, CancellationToken ct) =>
{
    await service.BlockAsync(id, ct);
    return Results.NoContent();
});

partners.MapPost("/{id:guid}/unblock", async (Guid id, IPartnerAppService service, CancellationToken ct) =>
{
    await service.UnblockAsync(id, ct);
    return Results.NoContent();
});

// ===================== Accounting =====================

var accounts = app.MapGroup("/api/accounts").WithTags("Accounting");

accounts.MapGet("", async (ILedgerAccountAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

accounts.MapPost("", async (CreateLedgerAccountDto input, ILedgerAccountAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/accounts/{id}", new { id });
});

var vouchers = app.MapGroup("/api/vouchers").WithTags("Accounting");

vouchers.MapGet("", async (IVoucherAppService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

vouchers.MapPost("", async (CreateVoucherDto input, IVoucherAppService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/vouchers/{id}", new { id });
});

vouchers.MapPost("/{id:guid}/lines", async (
    Guid id, AddVoucherLineDto input, IVoucherAppService service, CancellationToken ct) =>
{
    await service.AddLineAsync(id, input, ct);
    return Results.NoContent();
});

vouchers.MapPost("/{id:guid}/post", async (Guid id, IVoucherAppService service, CancellationToken ct) =>
{
    await service.PostAsync(id, ct);
    return Results.NoContent();
});

vouchers.MapPost("/{id:guid}/cancel", async (Guid id, IVoucherAppService service, CancellationToken ct) =>
{
    await service.CancelAsync(id, ct);
    return Results.NoContent();
});

// ===================== Platform: users =====================

var usersGroup = app.MapGroup("/api/users").WithTags("Platform");

usersGroup.MapGet("", async (IUserAdminService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

usersGroup.MapPost("", async (CreateUserDto input, IUserAdminService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/users/{id}", new { id });
});

usersGroup.MapPost("/{id:guid}/password", async (
    Guid id, SetPasswordDto input, IUserAdminService service, CancellationToken ct) =>
{
    await service.SetPasswordAsync(id, input.Password, ct);
    return Results.NoContent();
});

usersGroup.MapPost("/{id:guid}/roles", async (
    Guid id, SetRolesDto input, IUserAdminService service, CancellationToken ct) =>
{
    await service.SetRolesAsync(id, input.RoleIds, ct);
    return Results.NoContent();
});

usersGroup.MapPost("/{id:guid}/enable", async (Guid id, IUserAdminService service, CancellationToken ct) =>
{
    await service.SetEnabledAsync(id, true, ct);
    return Results.NoContent();
});

usersGroup.MapPost("/{id:guid}/disable", async (Guid id, IUserAdminService service, CancellationToken ct) =>
{
    await service.SetEnabledAsync(id, false, ct);
    return Results.NoContent();
});

usersGroup.MapPost("/{id:guid}/unlock", async (Guid id, IUserAdminService service, CancellationToken ct) =>
{
    await service.UnlockAsync(id, ct);
    return Results.NoContent();
});

// ===================== Platform: roles =====================

var rolesGroup = app.MapGroup("/api/roles").WithTags("Platform");

rolesGroup.MapGet("", async (IRoleAdminService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

rolesGroup.MapPost("", async (CreateRoleDto input, IRoleAdminService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/roles/{id}", new { id });
});

rolesGroup.MapPost("/{id:guid}/permissions", async (
    Guid id, SetPermissionsDto input, IRoleAdminService service, CancellationToken ct) =>
{
    await service.SetPermissionsAsync(id, input.Permissions, ct);
    return Results.NoContent();
});

// ===================== Platform: companies & branches =====================

var companiesGroup = app.MapGroup("/api/companies").WithTags("Platform");

companiesGroup.MapGet("", async (IOrganizationService service, CancellationToken ct) =>
    Results.Ok(await service.ListCompaniesAsync(ct)));

companiesGroup.MapPost("", async (CreateCompanyDto input, IOrganizationService service, CancellationToken ct) =>
{
    var id = await service.CreateCompanyAsync(input, ct);
    return Results.Created($"/api/companies/{id}", new { id });
});

companiesGroup.MapPost("/{id:guid}/enable", async (Guid id, IOrganizationService service, CancellationToken ct) =>
{
    await service.SetCompanyEnabledAsync(id, true, ct);
    return Results.NoContent();
});

companiesGroup.MapPost("/{id:guid}/disable", async (Guid id, IOrganizationService service, CancellationToken ct) =>
{
    await service.SetCompanyEnabledAsync(id, false, ct);
    return Results.NoContent();
});

var branchesGroup = app.MapGroup("/api/branches").WithTags("Platform");

branchesGroup.MapGet("", async (IOrganizationService service, CancellationToken ct) =>
    Results.Ok(await service.ListBranchesAsync(ct)));

branchesGroup.MapPost("", async (CreateBranchDto input, IOrganizationService service, CancellationToken ct) =>
{
    var id = await service.CreateBranchAsync(input, ct);
    return Results.Created($"/api/branches/{id}", new { id });
});

// ===================== Platform: fiscal years =====================

var fiscalYearsGroup = app.MapGroup("/api/fiscal-years").WithTags("Platform");

fiscalYearsGroup.MapGet("", async (IFiscalYearService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(ct)));

fiscalYearsGroup.MapGet("/open", async (IFiscalYearService service, CancellationToken ct) =>
{
    var fy = await service.GetOpenAsync(ct);
    return fy is null ? Results.NotFound() : Results.Ok(fy);
});

fiscalYearsGroup.MapPost("", async (CreateFiscalYearDto input, IFiscalYearService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(input, ct);
    return Results.Created($"/api/fiscal-years/{id}", new { id });
});

fiscalYearsGroup.MapPost("/{id:guid}/close", async (Guid id, IFiscalYearService service, CancellationToken ct) =>
{
    await service.CloseAsync(id, ct);
    return Results.NoContent();
});

fiscalYearsGroup.MapPost("/{id:guid}/reopen", async (Guid id, IFiscalYearService service, CancellationToken ct) =>
{
    await service.ReopenAsync(id, ct);
    return Results.NoContent();
});

// ===================== Blazor UI =====================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ===================== Health =====================

app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "RGRE-ERP" }))
    .WithTags("System");

app.Run();

// Form-binding records for the antiforgery-enforced auth endpoints; type
// declarations must come after all top-level statements.
public sealed record LoginForm(string Username, string Password, string? ReturnUrl);

public sealed record SwitchContextForm(string? CompanyId, string? BranchId, string? FiscalYearId);
