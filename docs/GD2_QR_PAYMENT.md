# GD2 — QR Demo Payment & Responsive Checkout

Base: `develop` @ `0a7fef15e495d1d83887077619e4d3e148acb62f`. This phase must NOT edit `main`.

## Scope

The GD1 `PaymentCore`, database entities and existing `RentalOrders` / `RentalOrderPayments` schema remain intact. **No new EF migration, SQL ALTER, DB reset, or import.** Existing migrations are used in CI **only for a fresh, disposable MySQL 8 container**. The user's supplied dataset remains untouched.

GD2 adds:

- Backend-generated, scannable PNG QR using `QRCoder` 1.8.1 (Linux/Windows-compatible `PngByteQRCode`). No token is sent to an external QR image service. Package information: https://www.nuget.org/packages/qrcoder/1.8.1
- Customer-owned QR endpoint `GET /PaymentDemo/Qr?orderId=...`, GET same-device redirect `/PaymentDemo/Start?orderId=...`.
- Anonymous/mobile `GET /PaymentDemo/Checkout?token=...`: signed, expiring Data Protection bearer token. Only shows order code, amount, method and transaction reference (no customer identity or documents).
- Explicit human-click POST confirmation to **existing** `POST /api/payments/demo/complete`, with `PAID` or `FAILED`. GET must NEVER change payment state (avoids link previews).
- Responsive checkout for QR bank demo, wallet demo, card demo, cash Owner confirmation. Optional separately labeled real VietQR, **off by default**.
- Browser polling every ~3 seconds of existing `OrderPayment/Status`, plus `orderStatus` field so cancellation refreshes the desktop. The source of truth is MySQL, not the browser.
- Live countdown from payment `CreatedAt` and `Payment:PendingMinutes`. Expired current attempts use GD1's lazy expiry; the "Retry" button invokes existing `/OrderPayment/Select` to create a new payment.
- MySQL-backed HTTP smoke tests covering role isolation, PNG generation, phone landing, success/fail, replay, replacement, cancelled order, expired token and forged token.

## Exact routes

| Route | Method | Access | Description |
|---|---|---|---|
| `/OrderPayment/Index?orderId=...` | GET | Customer | Checkout, QR and status |
| `/OrderPayment/Select` | POST | Customer + CSRF | Pick/switch payment method; existing GD1 |
| `/PaymentDemo/Qr?orderId=...` | GET | Owning Customer | Generates QR PNG for active demo payment |
| `/PaymentDemo/Start?orderId=...` | GET | Owning Customer | Same-device alternative to scanning |
| `/PaymentDemo/Checkout?token=...` | GET | Signed token holder | Responsive anonymous confirmation UI; read only |
| `/api/payments/demo/complete` | POST | Signed bearer token | Existing GD1 payment confirmation/decline endpoint |
| `/OrderPayment/Status?orderId=...` | GET | Owning Customer | Existing status plus `orderStatus` |

## Demo activation

`Payment:DemoEnabled` is **true automatically in Development**. In Production it is **false by default**. Demo always displays **DEMO — NO REAL FUNDS** and can be enabled for a controlled demonstration only via environment variable `Payment__DemoEnabled=true`.

### Reachable QR URL (important)

Phone QR cannot use `localhost` on a different device. For a phone, use a publicly reachable HTTPS site or a development LAN address (same Wi-Fi and firewall port open). In Production the payment QR **requires** `Payment__PublicBaseUrl=https://your-real-domain` pointing at this application (or its trusted reverse proxy). Use the environment variable to avoid committing deployment-specific URLs.

For local LAN test, run the site in Development and set (adapt to your LAN IP):

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://0.0.0.0:5119"
$env:Payment__PublicBaseUrl = "http://192.168.1.20:5119"
dotnet run --project RentalPlatform/RentalPlatform.csproj --no-launch-profile
```

Use a **test database connection string**. For actual public deployments use HTTPS and persist Data Protection keys securely across app restarts/replicas. Do not deploy a demo gateway publicly without considering the implications of any bearer token holder being allowed to confirm a simulated payment.

## Real VietQR (optional and separate)

`Payment:RealBankQrEnabled` defaults **false**. If enabled with correct `Payment:BankId`, `Payment:AccountNumber`, `Payment:AccountName`, the existing external VietQR image may be shown in a separate disclosure panel. It is not a verified payment: scanning or opening VietQR does **not** update rental order status. Real bank confirmation still requires authenticated webhook from a real provider. **Never use placeholder bank account values for actual collection.** Demo QR is independent of VietQR and changes application state only as a simulation.

## Security and limitations

- Signed, time-limited tokens are bearer capabilities. QR generation is authenticated and limited to the Customer that owns the order; anyone with the already-issued QR/token can execute its demo outcome once. Do not distribute real payment links inadvertently.
- The anonymous landing page sends `Cache-Control: no-store`, `Referrer-Policy: no-referrer`, disallows third-party assets via CSP, prevents indexing and framing, and never logs token data in client JS.
- Every POST is validated against live MySQL by **GD1 `PaymentCore`** (method, current reference, amount, expiry, cancellation and row lock). Source demo does not update tables itself.
- Expiry is enforced on session creation and completion; no background cleanup and no extra DB table. In DB, lazy expiry remains as in GD1. The countdown is UX only.
- The mobile landing page intentionally does not need a user login because the signed QR is the authorization factor. A leaked active QR can confirm a simulated payment; this is acceptable **only for a demo environment**. For real payments, never treat this as proof of money received.
- `QRCoder` adds one .NET NuGet package; the Windows `dotnet restore` and CI will obtain it normally. No CDN is needed for QR generation.
- CI smoke validates PNG signature and successful HTTP flow, but automated browser-camera QR decoding and a physical phone scan are **manual acceptance checks**.

## Acceptance steps

1. Windows: `dotnet restore`, `dotnet build -c Release`, then push `develop` using `apply-gd2.ps1`.
2. CI: GD0 baseline, GD1 payment core, GD2 QR payment workflows must all be green.
3. Manual: access a completed-signed order as Customer; choose QR Demo, scan with phone, press confirm; desktop refreshes automatically to `DaThanhToan` without manual data edits.
4. Manual: repeat for wallet/card failed and retry, cash Owner confirmation, wrong-identity and expired QR.
5. Manual: repeat on smartphone viewport around 320–390px wide; no horizontal overflow or leaking confidential order details.

## Revisions and Git

One long-lived development branch only: `main` stays untouched; `develop` accumulates commits GD0 → GD1 → GD2. No DB migration file is created in GD2.
