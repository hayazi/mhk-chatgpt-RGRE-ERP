using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using RGRE.ERP.Domain.Inventory.Issues;
using RGRE.ERP.Domain.Inventory.Receipts;
using RGRE.ERP.Domain.Inventory.Transfers;
using RGRE.ERP.Domain.Organization;
using RGRE.ERP.Domain.Partners;
using RGRE.ERP.Domain.Security;
using RGRE.ERP.Infrastructure.Persistence;
using RGRE.ERP.Infrastructure.Repositories;
using RGRE.ERP.Infrastructure.Security;

namespace RGRE.ERP.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the EF Core DbContext (SQLite by default), all repositories,
    /// the unit of work, the audit interceptor and the application services.
    /// </summary>
    public static IServiceCollection AddErp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("ErpDatabase")
            ?? "Data Source=rgre-erp.db";

        services.AddDbContext<ErpDbContext>((sp, options) =>
            options.UseSqlite(connectionString)
                .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        // Cross-cutting infrastructure
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // ICurrentUserContext is registered by the host (Web) because its
        // implementation depends on the HTTP/Blazor request pipeline.

        // Unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        // Repositories
        services.AddScoped<IGoodsRepository, GoodsRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IStockMoveRepository, StockMoveRepository>();
        services.AddScoped<IStockBalanceRepository, StockBalanceRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IIssueRepository, IssueRepository>();
        services.AddScoped<IStockTransferRepository, StockTransferRepository>();
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        services.AddScoped<ILedgerAccountRepository, LedgerAccountRepository>();
        services.AddScoped<IVoucherRepository, VoucherRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IFiscalYearRepository, FiscalYearRepository>();

        // Application services
        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<IReceiptAppService, ReceiptAppService>();
        services.AddScoped<IIssueAppService, IssueAppService>();
        services.AddScoped<IStockTransferAppService, StockTransferAppService>();
        services.AddScoped<IGoodsAppService, GoodsAppService>();
        services.AddScoped<IUnitAppService, UnitAppService>();
        services.AddScoped<IWarehouseAppService, WarehouseAppService>();
        services.AddScoped<IStockQueryService, StockQueryService>();
        services.AddScoped<IPartnerAppService, PartnerAppService>();
        services.AddScoped<ILedgerAccountAppService, LedgerAccountAppService>();
        services.AddScoped<IVoucherAppService, VoucherAppService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IFiscalYearService, FiscalYearService>();

        return services;
    }
}
