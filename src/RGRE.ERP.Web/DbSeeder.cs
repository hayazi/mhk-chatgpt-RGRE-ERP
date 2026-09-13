using Microsoft.EntityFrameworkCore;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Organization;
using RGRE.ERP.Domain.Partners;
using RGRE.ERP.Domain.Security;
using RGRE.ERP.Infrastructure.Persistence;
using RGRE.ERP.Infrastructure.Security;

namespace RGRE.ERP.Web;

/// <summary>
/// Seeds a small demo dataset (mirrors the legacy MHK master data shape)
/// the first time the SQLite database is created: master data, chart of
/// accounts, users/roles, company/branches and the open fiscal year.
/// </summary>
public static class DbSeeder
{
    private static bool _checked;

    public static void Seed(ErpDbContext db)
    {
        if (_checked || db.Units.Any())
        {
            _checked = true;
            return;
        }

        // ---------- Inventory + accounting demo data ----------

        var each = new Unit("عدد (Each)");
        var box = new Unit("کارتن (Box)", 12m, each.Id);
        db.Units.AddRange(each, box);

        var main = new Warehouse("01", "انبار مرکزی (Main)");
        var raw = new Warehouse("02", "انبار مواد اولیه (Raw)");
        db.Warehouses.AddRange(main, raw);

        db.Goods.AddRange(
            new Goods("G001", "لپ تاپ (Laptop)", each.Id, salePrice: 850_000_000m, standardCost: 720_000_000m),
            new Goods("G002", "ماوس (Mouse)", each.Id, salePrice: 8_500_000m, standardCost: 5_200_000m),
            new Goods("R001", "کابل USB", each.Id, goodsType: GoodsType.RawMaterial, standardCost: 900_000m));

        db.Partners.AddRange(
            new Partner("C0001", "شرکت داده‌پردازی", PartnerKind.Customer,
                telephone: "021-88776655", cashCreditLimit: 1_000_000_000m),
            new Partner("S0001", "بازرگانی فناوران", PartnerKind.Supplier,
                telephone: "021-44556677"),
            new Partner("B0001", "پیمانکار نصب", PartnerKind.Customer | PartnerKind.Supplier));

        var cash = new LedgerAccount("1101", "صندوق (Cash)", 1, isDebitNature: true);
        var bank = new LedgerAccount("1102", "بانک (Bank)", 1, isDebitNature: true);
        var ar = new LedgerAccount("1201", "حساب‌های دریافتنی", 1, isDebitNature: true);
        var inventory = new LedgerAccount("1301", "موجودی کالا", 1, isDebitNature: true);
        var ap = new LedgerAccount("2101", "حساب‌های پرداختنی", 1, isDebitNature: false);
        var sales = new LedgerAccount("4101", "فروش", 1, isProfitAndLoss: true, isDebitNature: false);
        var cogs = new LedgerAccount("5101", "بهای تمام شده فروش", 1, isProfitAndLoss: true);

        db.LedgerAccounts.AddRange(cash, bank, ar, inventory, ap, sales, cogs);

        // ---------- Platform: company, branches, fiscal year ----------

        var company = new Company("RGRE", "شرکت RGRE (RGRE Co.)", economicCode: "411xxxxxxx", nationalId: "14xxxxxxxxx");
        db.Companies.Add(company);

        var hq = new Branch(company.Id, "HQ", "دفتر مرکزی (Headquarters)");
        var north = new Branch(company.Id, "N1", "شعبه شمال (North Branch)");
        db.Branches.AddRange(hq, north);

        // The Jalali year 1405 runs 21 Mar 2026 .. 20 Mar 2027.
        var fy = new FiscalYear(
            company.Id,
            "سال مالی ۱۴۰۵ (FY 1405)",
            JalaliSeedDate(1405, 1, 1),
            JalaliSeedDate(1405, 12, 29));
        db.FiscalYears.Add(fy);

        // ---------- Platform: roles + users ----------

        var adminRole = new Role("Admin", "Full access to all modules", isSystemRole: true);
        adminRole.SetPermissions(AppPermissions.All);

        var accountantRole = new Role("Accountant", "Accounting documents and masters");
        accountantRole.SetPermissions(
        [
            AppPermissions.Partners, AppPermissions.Accounts, AppPermissions.Vouchers, AppPermissions.VouchersPost,
        ]);

        var warehouseRole = new Role("Warehouse", "Inventory documents and masters");
        warehouseRole.SetPermissions(
        [
            AppPermissions.Goods, AppPermissions.Warehouses, AppPermissions.Units,
            AppPermissions.Receipts, AppPermissions.ReceiptsPost,
            AppPermissions.Issues, AppPermissions.IssuesPost,
            AppPermissions.Transfers, AppPermissions.TransfersPost,
        ]);

        db.Roles.AddRange(adminRole, accountantRole, warehouseRole);

        var hasher = new Pbkdf2PasswordHasher();

        var admin = new User("admin", "مدیر سیستم (Administrator)", hasher.Hash("admin123"));
        admin.AddRole(adminRole.Id);
        admin.SetLastContext(company.Id, hq.Id, fy.Id);

        var accountant = new User("accountant", "حسابدار (Accountant)", hasher.Hash("accountant123"));
        accountant.AddRole(accountantRole.Id);
        accountant.SetLastContext(company.Id, hq.Id, fy.Id);

        var warehouse = new User("warehouse", "انباردار (Warehouse keeper)", hasher.Hash("warehouse123"));
        warehouse.AddRole(warehouseRole.Id);
        warehouse.SetLastContext(company.Id, hq.Id, fy.Id);

        db.Users.AddRange(admin, accountant, warehouse);

        db.SaveChanges();
        _checked = true;
    }

    /// <summary>Converter used only by the seeder (Jalali → Gregorian).</summary>
    private static DateTime JalaliSeedDate(int year, int month, int day)
        => Domain.Common.JalaliDate.ToGregorian(year, month, day);
}
