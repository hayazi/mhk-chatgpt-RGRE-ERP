# RGRE-ERP

A clean-architecture rewrite of the legacy MHK Delphi ERP (see
`mhk delphi source for chatgpt.zip` and `MHKOLDScripts.sql`, 548 tables)
for the RGRE organization. This first milestone covers the **Inventory**
(Anbar), **Partners** (Account/Customer) and **Accounting** (Voucher)
core with a full stock ledger.

## Stack

- .NET 9, C# 13
- **Blazor Server** interactive UI (server-side rendering over SignalR) + minimal APIs
- EF Core 9 (SQLite for dev; the provider is swappable to SQL Server)
- xUnit for tests

## Projects

| Project | Role |
|---|---|
| `RGRE.ERP.Domain` | Entities, domain events, repository interfaces. No dependencies. |
| `RGRE.ERP.Application` | App services, DTOs, stock-ledger logic. Depends on Domain only. |
| `RGRE.ERP.Infrastructure` | EF Core `ErpDbContext`, repositories, DI, migrations. |
| `RGRE.ERP.Web` | **Blazor Server UI** (pages under `Components/Pages`), minimal-API endpoints, OpenAPI, seeding. |
| `RGRE.ERP.Application.Tests` | Domain rule + service tests with in-memory fakes. |

## Mapping from the legacy system

| Legacy (Delphi / SQL) | RGRE-ERP |
|---|---|
| `Resid` + `ResidDetail` (رسید) | `Receipt` aggregate → `StockMove` (In) on post |
| `Khorooj` + `KhoroojDetail` (خروج) | `Issue` aggregate → `StockMove` (Out) on post |
| `EnteghalBeinAnbar` (+Detail) | `StockTransfer` → Out move + In move on post |
| `Analyze` quantity tables | `StockBalances` with moving-average cost |
| `Goods`, `Unit`, `Anbar` | `Goods`, `Unit`, `Warehouse` |
| `Customer` / supplier side of `Account` | `Partner` (Customer, Supplier or both) |
| `Account` (Kol/Moin/Tafzili levels) | `LedgerAccount` (Level 1..5 hierarchy) |
| `Voucher`/`Voucherms` + `VoucherCurrency` | `Voucher` + `VoucherLine` |
| Fixed SQL `ResidDetailSerialNo` etc. | handled in application services |

Key invariants enforced in the domain:

- Documents are only editable in `Draft`; `Post` requires ≥ 1 line.
- Posting writes immutable `StockMove` rows and updates `StockBalance`.
- `Voucher.Post` requires ≥ 2 lines and balanced debit/credit totals.
- Transfers reject same-source-and-destination warehouses.

## Running

```bash
cd RGRE-ERP
dotnet run --project src/RGRE.ERP.Web
# then open http://localhost:5000 (or the URL shown)
```

- `/` — the Blazor Server web app (dashboard, master data, documents, stock,
  accounting). Pages call the application services directly over the SignalR
  circuit; all domain rules (draft/post/cancel, balance checks) run server-side.
- `/openapi/v1.json` — OpenAPI document for the JSON API.

The SQLite database (`rgre-erp.db`) is created, migrated and seeded
automatically on first start with demo data mirroring the legacy master
data shape (units, goods, warehouses, partners, chart of accounts).

### Web UI pages

| Route | Page |
|---|---|
| `/` | Dashboard: KPIs, stock value by warehouse, recent documents |
| `/goods`, `/warehouses`, `/units` | Inventory master data with inline create forms |
| `/receipts` + `/receipts/{id}` | Resid list, draft line editor, post/cancel |
| `/issues` + `/issues/{id}` | Khorooj list, draft line editor, post/cancel |
| `/transfers` | EnteghalBeinAnbar: create, post, cancel |
| `/stock` | Balances per warehouse + goods move drill-down |
| `/partners` | Customers/suppliers with block/unblock |
| `/accounts` | Chart of accounts (Kol/Moin/Tafzili levels) |
| `/vouchers` | Journal entries: create draft, post (balance-enforced), cancel |

## API surface

| Area | Endpoints |
|---|---|
| Units | `GET/POST /api/units` |
| Goods | `GET/POST /api/goods`, `POST /api/goods/{id}/enable|disable` |
| Warehouses | `GET/POST /api/warehouses` |
| Receipts (Resid) | `GET/POST /api/receipts`, `GET /{id}`, `POST /{id}/lines`, `DELETE /{id}/lines/{lineId}`, `POST /{id}/post`, `POST /{id}/cancel` |
| Issues (Khorooj) | same shape under `/api/issues` |
| Transfers | same shape under `/api/transfers` |
| Stock | `GET /api/stock/{warehouseId}`, `GET /api/stock-moves/{goodsId}` |
| Partners | `GET/POST /api/partners`, `POST /{id}/block|unblock` |
| Accounts | `GET/POST /api/accounts` |
| Vouchers | `GET/POST /api/vouchers`, `POST /{id}/lines`, `POST /{id}/post`, `POST /{id}/cancel` |
| Health | `GET /health` |

### Example: receive stock, issue stock, read balance

```bash
BASE=http://localhost:5000
WH=$(curl -s $BASE/api/warehouses | python -c "import sys,json;print(json.load(sys.stdin)[0]['id'])")
G=$(curl -s $BASE/api/goods | python -c "import sys,json;print(json.load(sys.stdin)[0]['id'])")
U=$(curl -s $BASE/api/units | python -c "import sys,json;print(json.load(sys.stdin)[0]['id'])")

RID=$(curl -s -X POST $BASE/api/receipts -H "Content-Type: application/json" \
  -d "{\"documentNumber\":\"R-1\",\"documentDate\":\"2026-09-12T10:00:00Z\",\"warehouseId\":\"$WH\",\"lines\":[{\"goodsId\":\"$G\",\"unitId\":\"$U\",\"quantity\":10,\"unitPrice\":1000}]}" \
  | python -c "import sys,json;print(json.load(sys.stdin)['id'])")
curl -s -X POST $BASE/api/receipts/$RID/post

curl -s $BASE/api/stock/$WH
# [{"quantity":10.0,"averageCost":1000,"totalValue":10000.0}]
```

## Tests

```bash
dotnet test RGRE.ERP.slnx   # 11 tests: receipt/issue/transfer/voucher rules
```

## Next milestones (suggested)

1. **Data migration tool** — import legacy `MHK` SQL Server data (Goods,
   Account, Resid/Khorooj history) into the new schema.
2. **Sales & purchasing** — `Factor`/`BrgKharid` invoices with automatic
   GL vouchers and partner credit-limit enforcement.
3. **Auth & multi-branch** — legacy `Permissions*`/`Branchs` tables map to
   an ASP.NET Identity + claims model.
4. **Persian calendar + RTL** — Jalali date display/editing and RTL layout in
   the Blazor UI (dates currently Gregorian, layout LTR).
5. **Check/cheque management** — port `KhazanehDaryVaCheck` ( PaidChecks,
   DastehCheck, cancellation flows).
