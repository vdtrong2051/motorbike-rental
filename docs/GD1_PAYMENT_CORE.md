# GD1 — Payment Core (no database schema changes)

Baseline branch: `develop`. Prior baseline commit: `a760318`.

## Boundary

- **No new EF migration** and no schema changes. The existing `RentalOrders`, `RentalOrderPayments` tables and MySQL indexes are reused.
- The project's **existing baseline migrations still initialize a fresh MySQL database** for CI or a new local installation. Do not confuse “no new migration” with “a fresh DB needs no schema”.
- Legacy `Booking` / `Payment` remains intact. GD1 targets `RentalOrder` exclusively.
- All simulated payments are explicitly marked **DEMO ONLY**. They do not transfer money or verify a bank transfer.
- UI redesign, phone-scannable demo landing page, improved wallet/card UI, and reviews belong to subsequent phases.

## Payment states and schema constraint

The existing `TrangThaiThanhToanDon` enum provides `ChoThanhToan`, `ChoThuTienKhiNhanXe`, `DaThanhToan`, and `ThatBai`. Because schema changes are out of scope, expired, superseded, and cancelled attempts are stored as `ThatBai` with reason prefixes in the existing `GhiChuCongThanhToan` column:

- `GD1:EXPIRED`
- `GD1:REPLACED`
- `GD1:CANCELLED`
- `GD1:Demo:FAILED:...`
- `GD1:Webhook:FAILED:...`

Payment attempts are append-only from the user's perspective. When changing method, a pending attempt becomes failed and a new row/reference is created. Retrying the same method while still pending reuses the existing reference. The database unique index on `MaGiaoDich` remains.

**Limitation without schema changes:** the reason classification is a structured string, not an indexed status enum; there is no persistent provider-event inbox. We rely on row locking and current-payment reference checks for transactional idempotency. For a real payment provider, add webhook event id deduplication, signed provider-specific payload validation, a formal ledger, and reconciliation.

## Transaction correctness

Payment state changes and cancellation acquire `SELECT ... FOR UPDATE` on the **same `RentalOrders` row**, inside a `READ COMMITTED` MySQL transaction. All checks and writes are then performed within that transaction. The result prevents a callback from racing a cancellation and prevents two simultaneous callbacks from both transitioning the payment independently.

- Callback requires exact reference and exact nonzero amount.
- Paid callback replay is idempotent; it does not create another transaction row or another revenue event.
- Late callbacks, cancelled orders, superseded references, failed/expired payments, wrong method and wrong amount are rejected.
- Owner cash confirmation is restricted by owner ID and payment method within the row lock.
- Bank/wallet/card webhook or demo cannot pay a cash attempt.
- Payment attempts have TTL in `Payment:PendingMinutes` (1–1440, default 30); cash has no TTL. Expiration is enforced when confirming/selecting (lazy expiry), not by a scheduler.

## Demo flow API

| HTTP | Route | Access | Purpose |
|---|---|---|---|
| POST | `/OrderPayment/Select` | Customer + antiforgery | Existing MVC selection; preserved |
| GET | `/OrderPayment/Status?orderId=...` | Customer | Existing MVC status; preserved |
| POST | `/api/payments/attempts` | Customer + **antiforgery token** | JSON body `{ "orderId": "<guid>", "method": 1 }` (`0` cash, `1` bank, `2` wallet, `3` card) |
| GET | `/api/payments/orders/{orderId}/status` | Customer | Order's current payment status |
| GET | `/api/payments/demo/session?orderId=...` | Customer | Returns signed short-lived token, reference, amount, expiry and submit URL |
| POST | `/api/payments/demo/complete` | Bearer signed token; no login required | JSON body `{ "token": "...", "outcome": "PAID" }` or `FAILED` |
| POST | `/api/payments/webhook` | `X-Webhook-Secret` | Existing gateway integration: `PAID` or `FAILED` |

The customer must select a payment method **after both parties sign the contract and the Owner confirms the order**.

The demo session endpoint returns a capability token only to the owning Customer and only for a pending bank/wallet/card attempt. The token is authenticated and time-limited via ASP.NET Core Data Protection. The anonymous complete endpoint uses that bearer capability, then validates live database state. The amount is loaded from MySQL, never accepted from a scanned QR client. The complete endpoint uses POST to prevent GET/link-preview from charging an attempt. In GD2 a small QR landing page can POST to this API after the human taps Confirm.

## Configuration and safety

In `RentalPlatform/appsettings.json`:

```json
"Payment": {
  "BankId": "970422",
  "AccountNumber": "0123456789",
  "AccountName": "RENTAL PLATFORM",
  "WebhookSecret": "change-this-secret-before-production",
  "DemoEnabled": false,
  "PendingMinutes": 30
}
```

- Development environment enables demo APIs; outside Development they are **off by default**. Set environment variable `Payment__DemoEnabled=true` only for a clearly identified demonstration deployment.
- Never use default webhook secret in production. The controller rejects the placeholder or secrets shorter than 32 characters in every environment. Configure `Payment__WebhookSecret` in deployment secrets.
- Persist and protect ASP.NET Core Data Protection keys for any durable/multi-instance deployment. Temporary keys may invalidate tokens after restart. Demo tokens are short-lived but should not be written to logs.
- `Payment:BankId` / `AccountNumber` are legacy VietQR display details, **not a confirmed bank integration**. If bank transfer has no real webhook, it will not be marked paid until a DEMO event is explicitly submitted.
- JSON `/api/payments/attempts` requires ASP.NET antiforgery token. In a future UI call, send the antiforgery request token in `RequestVerificationToken` header; MVC form selection already supplies it automatically.
- No production database commands run by `apply-gd1.ps1`.

## How to verify

Run local build and actual MySQL integration test only against an **isolated, disposable** database:

```powershell
dotnet restore tests/Gd1.Payment.Tests/Gd1.Payment.Tests.csproj
dotnet build tests/Gd1.Payment.Tests/Gd1.Payment.Tests.csproj -c Release
# Set ConnectionStrings__DefaultConnection to a throwaway MySQL database initialized with existing migrations.
dotnet run --project tests/Gd1.Payment.Tests/Gd1.Payment.Tests.csproj -c Release
```

GitHub Actions workflow `GD1 payment core` automatically creates a disposable MySQL 8 container, initializes the **existing** schema and executes integration tests for selection idempotency, exact amounts, replay, replacement, failure/retry, cancellation, expiration, cash authorization, concurrent callbacks, and protected demo tokens.

## Acceptance

GD1 is complete only after Windows/local Release build and `GD1 payment core` GitHub Actions succeed. This kit does **not** assert tests passed before execution.
