# RGRE-ERP Feature Analysis — Legacy MHK → New System

Source of truth: `mhk delphi source for chatgpt.zip` (2,194 Delphi units across 11
module folders) and `MHKOLDScripts.sql` (548 tables, 186k lines). Companion to the
legacy mapping table in the root [README](../README.md).

## 1. Legacy module inventory (by Delphi source size)

| # | Legacy module (folder) | Units | Domain | Tables (approx) | Status in RGRE-ERP |
|---|------------------------|------:|--------|----------------:|--------------------|
| 1 | `Anbar` | 772 | Inventory, purchasing, sales, production, CRM | ~200 | 🟡 ~15% (core ledger only) |
| 2 | `PayRoll` | 250 | Payroll engine | ~90 (PR*) | 🔴 0% |
| 3 | `Account` | 188 | General ledger / accounting | ~40 | 🟡 ~10% |
| 4 | `KhazanehDaryVaCheck` | 149 | Treasury & cheques | ~25 | 🔴 0% |
| 5 | `Amval` + `AmvalH` | 270 | Fixed assets (two variants) | ~35 | 🔴 0% |
| 6 | `Salary` | 622 | Salary list handling / personnel | (shares PR*) | 🔴 0% |
| 7 | `MHK` | 91 | Shared shell: login, permissions, branches, fiscal year, registry, reports engine | ~15 | 🔴 0% |
| 8 | `Lib` | 36 | Shared UI/report libraries | — | n/a (replaced by .NET stack) |
| 9 | `Anbar2`, `Abstract`, `Desgin` | 16 | Variants/prototypes | — | n/a |

Key insight: **Anbar is not just inventory** — it contains the full
procure-to-stock, order-to-cash and make-to-stock cycles, which is where most of
the missing features live.

## 2. Feature gap analysis, per business cycle

### 2.1 Inventory (Anbar core) — 🟡 started

| Legacy feature | Legacy tables/forms | New equivalent | Gap |
|---|---|---|---|
| Goods receipt (Resid) | `Resid`, `ResidDetail` | `Receipt` aggregate + API | ✅ Done (qty, moving avg cost) |
| Goods issue (Khorooj) | `Khorooj`, `KhoroojDetail` | `Issue` aggregate + API | ✅ Done (validates against stock) |
| Inter-warehouse transfer | `EnteghalBeinAnbar(+Detail)` | `StockTransfer` aggregate | ✅ Done |
| Stock ledger & balances | `Analyze`, `AnalyzeDeatil` | `StockMove` + `StockBalance` | ✅ Done (immutable moves, avg cost) |
| Multi-unit goods (3 units, ratios) | `Goods` Unit1..3, `Unit`, `UnitRatio` | `Goods` has one `BaseUnitId` | 🔴 Units 2/3 + conversion ratios |
| Multiple price levels (10!) | `Goods` SalePrice1..10 | `Goods.UnitPrice` only | 🔴 Price list per level + customer class |
| Batch/lot & serial tracking | `GoodsSerialNo`, `ResidDetailSerialNo`, `KhoroojSerialNo`, `ControlSerialNo` | — | 🔴 Serial/batch dimension on StockMove |
| Consignment (Amani) receipts/issues | `ResidAmani`, `KhoroojAmani`, `AmaniDT/MS` | — | 🔴 Separate consignment ledger |
| Stock counting (AnbarGardani) | `AnbarGardani(+Detail)`, `ShomareshAnbarGardani` | — | 🔴 Count sheets + variance posting |
| Waste/scrap & adjustment | `ElamZaieat(+Detail)`, `TaadilTedadi`, `TaadilMablaghi` | — | 🔴 Adjustment/waste doc types |
| Reorder points, min/max | `Goods` TolKharid/ArzKharid, RepMKUnderOrderLimit | — | 🔴 Reorder rules + alert report |
| Barcode printing | `RepPrintBarCode` | — | 🔴 |
| Physically-separated sub-stocks | `Anbar2` concept, `ResidDtDirect`/`ResidMsDirect` | — | 🟡 Later |
| Direct (non-doc) entry | `ResidDirect`, `GoodsDirect` | — | 🟡 Later |

### 2.2 Purchasing (BrgKharid chain) — 🔴 missing

Legacy chain: `DarkhastKharid` (purchase request) → `ElamKharid` (purchase order,
independent+linked variants, multi-currency) → `DaryaftMavad` (goods receipt note) →
`BrgKharid`/`Factor` vendor invoice → `HazineVaKosorat*` (cost variances) → AP voucher.
Plus commissions to buyers (`ComiKharid`).

**To build:** `PurchaseRequest`, `PurchaseOrder` (with currency + FX rate),
`GoodsReceiptNote` (feeds existing Receipt), `VendorInvoice` (3-way match),
buyer commissions.

### 2.3 Sales (BrgForoosh chain) — 🔴 missing

Legacy chain: `DarkhastKala` (customer order) → `PishFactor` (proforma) →
`BrgKhorooj`/`MKhorooj` (delivery note) → `Khorooj` (issue) → `Factor`/`FactorMostaghel`
(invoice, discounts, VAT, salesperson+commission, settlement days) → `BrgForoosh` →
AR voucher. CRM side: `CRMBazaryabMoshtari` (sales-rep ↔ customer), activities.

**To build:** `SalesOrder`, `Proforma`, `DeliveryNote` (feeds existing Issue),
`SalesInvoice` (line discounts, invoice-level discount, VAT, salesperson commission),
customer credit limit, CRM-lite (customers, activities).

### 2.4 Production (BOM/Montaj) — 🔴 missing

Legacy: `BOMMs/Dt`, `FormulMontaj` (recipe), `SefareshAzKarkhane` (production order),
`ElamTolid` (production declaration), `Montaj*` (assembly from components),
`BG*` tables (production lines: materials/parti/tabdil), `Karkard` (machine
work-hours), `DarkhastKalaAzAnbar` (material request).

**To build:** `BillOfMaterials` (multi-level), `ProductionOrder`
(reserve components → consume via Issue → produce via Receipt with rolled-up cost),
work centers.

### 2.5 Accounting (Account) — 🟡 started

| Legacy | Tables/forms | New | Gap |
|---|---|---|---|
| 4-level account tree | `GKol`, `GMoin`, `GTafzili`, `Account` | `LedgerAccount` (Kol/Moin/Tafzili levels) | 🟡 4th level + coding rules |
| Vouchers (double-entry) | `Voucher`, `Voucherms`, `VoucherAppendix`, `VoucherCurrency` | `Voucher` + `VoucherLine`, balanced check | ✅ Core done |
| Fiscal year + opening entry | `FYear`, `SanadEftetahie`, `DelVouchersAndCreateEftetahie` | — | 🔴 Fiscal-year entity + closing/opening |
| Currency ledgers | `CurrencyLeger`, `VoucherCurrency` | — | 🔴 Multi-currency amounts per line |
| Cost centers (2-level) | `CenterG`, `CenterGAccount`, `CostCenterProject` | — | 🔴 Cost-center dimension on VoucherLine |
| Reports | Daily journal, ledgers, trial balance, P&L, balance sheet (`BussProLoss`) | — | 🔴 Reporting layer |
| Confirmation workflow | `ConfirmedVoucherGroups` | — | 🟡 Later |

### 2.6 Treasury & cheques (KhazanehDaryVaCheck) — 🔴 missing

Legacy: cash boxes (`Sandogh`), bank accounts (`Banks`, `BankBranches`, `BankSpec`),
receipt/payment vouchers (`DaryaftData`, `PardakhtCashData`, `DastorPardakht`),
cheque lifecycle — **received cheques** (`ReceiveChecks`: in hand → deposited →
cleared/bounced) and **issued cheques** (`PaidChecks`, groups, print), bank
reconciliation (`MoghayeratBanki`, `AbsReadBankTransFromFile`), blacklist.

**To build:** `CashBox`/`BankAccount`, `TreasuryDocument` (receipt/payment, auto
voucher), `Cheque` aggregate with state machine (InHand → Deposited → Cleared |
Bounced; Issued → HandedOver → Cleared), reconciliation.

### 2.7 Fixed assets (Amval/AmvalH) — 🔴 missing

Legacy: asset cards (`FixedAsset`, groups), acquisition (`FixedKharid`,
`KharidAmval`), revaluation up/down (`AfzaeshArzeshAmval`, `KaheshArzeshAmval`),
depreciation tables + methods (`JadvalEstehlak`, `FixedAssetCalMethod`), scrap
(`EsghatAmval`), loss (`MafghodiAmval`), sale (`ForoshAmval`), location tracking
(`MojodiAmvalDarMahal`, `MahalEsteghrarFixedAsset`), conversion of inventory item
to asset (`FixedTabdilKalaBeAmval`), custody/insurance (`Vasigheh`).

**To build:** `FixedAsset` aggregate, depreciation schedules (straight-line +
declining balance), asset transactions (acquire/revalue/depreciate/scrap/sell),
auto-journal to GL.

### 2.8 Payroll & HR (PayRoll + Salary) — 🔴 missing (largest surface, ~90 PR* tables)

Legacy: personnel master (`PersonalInfo`, contracts, education, family), work
contracts (Hokm) with formula-driven items (`PRAhkams`, `AhkamItemsFormula`), time
clocks (`PRClock`, `ClockMatch*`, `Karkard` attendance), monthly payroll run
(`ComputingSalary`, plus/bonus items `PRPlusItems`, deductions `PRMinusItems`),
insurance (`SazmanBimeh`, `GharardadBimeh`, `BimehSpec`), tax tables (`PRTaxTables`,
`PersonnelTaxInfo`), loans (`Loan`, `PRPersonalLoans`), leaves, severance (`CalcSanavat`),
Eidi/bonus, bank disk export (`PRBankDisk*`), salary vouchers (`SalaryVouchers`).

**Recommended order:** personnel + contracts → attendance import → payroll run
engine (items + formulas) → tax/insurance → payslip + bank disk → GL posting.

### 2.9 Platform services (MHK module) — 🔴 missing (prerequisite for multi-user reality)

Legacy: users/permissions matrix (`Permissions`, `PermissionsAcc/Action/Branch/Co`),
multi-branch (`Branchs`), multi-company (`Corporations`), fiscal year gating
(`FYear` on every table), Persian calendar dates (`StrDate` type, `Calender`),
audit (`CreatedBy/ModifiedBy/ModificationDate` everywhere), report designer
(`rb*` tables), messaging (`tbMessages`).

**To build:** authentication + roles, company/branch dimension, fiscal year +
period control, Jalali date handling, audit trail columns via EF interceptors.

## 3. Current RGRE-ERP baseline (verified)

- 9 endpoint groups, 35 endpoints: units, goods, warehouses, receipts, issues,
  transfers, partners, accounts, vouchers (+ `/api/stock/{warehouseId}`,
  `/api/stock-moves/{goodsId}`, `/health`, OpenAPI)
- 11/11 tests green; SQLite dev DB with Persian seed data
- Moving-average costing bug already fixed (cumulative inbound basis)

## 4. Recommended completion roadmap

| Phase | Scope | Why first |
|---|---|---|
| **P1 — Platform** | Auth/roles, fiscal year + Jalali dates, audit columns, company/branch | Every other phase depends on it |
| **P2 — Purchasing** | PurchaseRequest → PurchaseOrder → GoodsReceiptNote → VendorInvoice (+AP voucher) | Highest daily-use value; reuses Receipt/ledger |
| **P3 — Sales** | SalesOrder → Proforma → DeliveryNote → SalesInvoice (+AR voucher), price levels, VAT, commissions | Completes order-to-cash |
| **P4 — Treasury** | Cash/bank accounts, payment/receipt docs, cheque state machine, reconciliation | Invoices need settlement |
| **P5 — Production** | BOM, production orders, material requests | Builds on existing Issue/Receipt |
| **P6 — Fixed assets** | Asset cards, depreciation, transactions → GL | Independent, mid-size |
| **P7 — Inventory depth** | Serial/batch, consignment, counting, adjustments, multi-unit, reorder, reports | Enriches the ledger built in M1 |
| **P8 — GL depth** | Cost centers, multi-currency, fiscal closing, financial reports | After transactional load exists |
| **P9 — Payroll/HR** | Personnel, contracts, attendance, payroll run, tax/insurance, payslips | Largest, most isolated — last |

Suggested P2 slice (vertical, testable): domain `PurchaseRequest`/`PurchaseOrder`
aggregates with status workflow → app services → EF configs + migration → 6–8
endpoints under `/api/purchase/*` → xUnit coverage mirroring the receipt tests.
