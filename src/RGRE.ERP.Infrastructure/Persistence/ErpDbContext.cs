using Microsoft.EntityFrameworkCore;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Common;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Inventory.Issues;
using RGRE.ERP.Domain.Inventory.Receipts;
using RGRE.ERP.Domain.Inventory.Transfers;
using RGRE.ERP.Domain.Organization;
using RGRE.ERP.Domain.Partners;
using RGRE.ERP.Domain.Security;

namespace RGRE.ERP.Infrastructure.Persistence;

public class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options)
        : base(options)
    {
    }

    public DbSet<Goods> Goods => Set<Goods>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockMove> StockMoves => Set<StockMove>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueLine> IssueLines => Set<IssueLine>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherLine> VoucherLines => Set<VoucherLine>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- Inventory master data ----------

        modelBuilder.Entity<Goods>(b =>
        {
            b.ToTable("Goods");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).HasMaxLength(250).IsRequired();
            b.Property(x => x.Name2).HasMaxLength(250);
            b.Property(x => x.BarCode).HasMaxLength(50);
            b.Property(x => x.StandardCost).HasPrecision(18, 4);
            b.Property(x => x.SalePrice).HasPrecision(18, 4);
            b.Property(x => x.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Unit>(b =>
        {
            b.ToTable("Units");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.Property(x => x.ConversionFactor).HasPrecision(18, 6);
        });

        modelBuilder.Entity<Warehouse>(b =>
        {
            b.ToTable("Warehouses");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(10).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.Telephone).HasMaxLength(50);
            b.Property(x => x.Description).HasMaxLength(2000);
        });

        // ---------- Stock ledger ----------

        modelBuilder.Entity<StockMove>(b =>
        {
            b.ToTable("StockMoves");
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.BaseQuantity).HasPrecision(18, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);
            b.HasIndex(x => new { x.GoodsId, x.MoveDate });
            b.HasIndex(x => x.DocumentId);
        });

        modelBuilder.Entity<StockBalance>(b =>
        {
            b.ToTable("StockBalances");
            b.HasKey(x => new { x.WarehouseId, x.GoodsId });
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.TotalValueIn).HasPrecision(18, 4);
            b.Property(x => x.TotalValueOut).HasPrecision(18, 4);
            b.Property(x => x.TotalInQuantity).HasPrecision(18, 4);
        });

        // ---------- Inventory documents ----------

        modelBuilder.Entity<Receipt>(b =>
        {
            b.ToTable("Receipts");
            b.HasKey(x => x.Id);
            b.Property(x => x.DocumentNumber).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.DocumentNumber).IsUnique();
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.ReceiptId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ReceiptLine>(b =>
        {
            b.ToTable("ReceiptLines");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);
            b.Property(x => x.Amount).HasPrecision(18, 4);
            b.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Issue>(b =>
        {
            b.ToTable("Issues");
            b.HasKey(x => x.Id);
            b.Property(x => x.DocumentNumber).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.DocumentNumber).IsUnique();
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.IssueId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<IssueLine>(b =>
        {
            b.ToTable("IssueLines");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);
            b.Property(x => x.Amount).HasPrecision(18, 4);
            b.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<StockTransfer>(b =>
        {
            b.ToTable("StockTransfers");
            b.HasKey(x => x.Id);
            b.Property(x => x.DocumentNumber).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.DocumentNumber).IsUnique();
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.TransferId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<StockTransferLine>(b =>
        {
            b.ToTable("StockTransferLines");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.Description).HasMaxLength(500);
        });

        // ---------- Partners ----------

        modelBuilder.Entity<Partner>(b =>
        {
            b.ToTable("Partners");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).HasMaxLength(250).IsRequired();
            b.Property(x => x.Name2).HasMaxLength(250);
            b.Property(x => x.NationalId).HasMaxLength(30);
            b.Property(x => x.EconomicCode).HasMaxLength(30);
            b.Property(x => x.Telephone).HasMaxLength(50);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.CashCreditLimit).HasPrecision(18, 4);
            b.Property(x => x.ChequeCreditLimit).HasPrecision(18, 4);
            b.Property(x => x.Description).HasMaxLength(2000);
        });

        // ---------- Accounting ----------

        modelBuilder.Entity<LedgerAccount>(b =>
        {
            b.ToTable("LedgerAccounts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        });

        modelBuilder.Entity<Voucher>(b =>
        {
            b.ToTable("Vouchers");
            b.HasKey(x => x.Id);
            b.Property(x => x.Number).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Number).IsUnique();
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.VoucherId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<VoucherLine>(b =>
        {
            b.ToTable("VoucherLines");
            b.HasKey(x => x.Id);
            b.Property(x => x.Debit).HasPrecision(18, 4);
            b.Property(x => x.Credit).HasPrecision(18, 4);
            b.Property(x => x.Description).HasMaxLength(500);
        });

        // ---------- Platform: security ----------

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Username).HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            b.HasMany(x => x.Roles).WithOne()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.ToTable("UserRoles");
            b.HasKey(x => new { x.UserId, x.RoleId });
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Roles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasMany(x => x.Permissions).WithOne()
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("RolePermissions");
            b.HasKey(x => new { x.RoleId, x.Permission });
            b.Property(x => x.Permission).HasMaxLength(100).IsRequired();
        });

        // ---------- Platform: organization ----------

        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("Companies");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(20).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).HasMaxLength(250).IsRequired();
            b.Property(x => x.Name2).HasMaxLength(250);
            b.Property(x => x.EconomicCode).HasMaxLength(30);
            b.Property(x => x.NationalId).HasMaxLength(30);
        });

        modelBuilder.Entity<Branch>(b =>
        {
            b.ToTable("Branches");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(20).IsRequired();
            b.Property(x => x.Name).HasMaxLength(250).IsRequired();
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.Telephone).HasMaxLength(50);
        });

        // ---------- Platform: fiscal years ----------

        modelBuilder.Entity<FiscalYear>(b =>
        {
            b.ToTable("FiscalYears");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.CompanyId, x.IsClosed });
        });

        // ---------- Audit columns on every auditable entity ----------

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity.CreatedAt));

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity.ModifiedAt));

            var createdBy = modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity.CreatedBy));
            createdBy.IsRequired(false);

            var modifiedBy = modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity.ModifiedBy));
            modifiedBy.IsRequired(false);
        }
    }
}
