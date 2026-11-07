# GĐ0 — Baseline (motorbike-rental)

## Branch policy

- `main`: original release; this phase does **not** modify it.
- `develop`: **single integration branch**, created from `main` and advanced with sequential commits GĐ0, GĐ1, GĐ2, …; no feature-branch fan-out.
- Future work should begin on `develop`. Merge to `main` only following separate acceptance.

## Changes in GĐ0

1. Register `IReviewRepository` and `IReviewService` in `Program.cs` so `ReviewController` can be activated.
2. Load `Vehicle.DanhGia` and each review's `Customer` only for detail requests; previous detail view read an unloaded navigation collection.
3. Expose review actions for completed **legacy `Booking`** in booking detail and booking history. `RentalOrderItem` reviews are explicitly scheduled for **GĐ3**, not implemented in GĐ0.
4. Fix broken placeholder image paths to `/image/no-image.png` (the actual repository path).
5. Move `pho-bien` ordering out of the gearbox dropdown into the sorting dropdown.
6. Avoid a null-reference error in the payment polling UI when e-wallet/card is selected (no `paymentPending` element on those screens).
7. Add a `develop` GitHub Actions check: .NET 10 restore/build, EF Core migrations against **temporary MySQL 8**, HTTP smoke test for public pages, Customer, Owner, Admin and role boundaries.

## What PASS means

- **Source changes prepared**: all exact text anchors matched expected baseline.
- **Local build PASS**: `dotnet restore` and `dotnet build -c Release` both succeeded on your machine.
- **GD0 CI PASS**: Actions workflow succeeded, including fresh MySQL migrations and role-level smoke.
- The CI smoke test does **not** exhaustively validate real payment transactions, full profile verification, uploading documents, contract signing, review persistence, or the entire end-to-end multi-vehicle rental flow. These require further integration tests.

## Security, preservation and scope

- No database schema change in GĐ0, no seed changes, no production data migration.
- The `-ApplyMigrations` switch should only target a backed-up **development** database.
- Payment gateway simulation, secure callback/idempotency, and new QR flows are **GĐ1/GĐ2**.
- Reviews attached to `RentalOrderItem`, review moderation, and review UX are **GĐ3**.
- Design-system/role-based layouts are **GĐ4**.

## Validate manually

1. Launch the app on local MySQL and log in as Customer, Owner, Admin.
2. Open `/Vehicle/Index` and confirm `Xe phổ biến nhất` appears only in the sorting dropdown.
3. Open an approved vehicle detail page with existing reviews; verify customer name and stars render.
4. Complete a legacy `Booking` through Owner flow; verify the Customer sees `Đánh giá`, submits one review and sees the rating on vehicle details.
5. Load existing `RentalOrder/Details` and `OrderPayment/Index` pages to check no behavior regressions.
6. Confirm Action CI completed successfully on `develop` before calling GĐ0 accepted.
