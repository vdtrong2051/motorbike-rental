# Motorbike Rental — reproducible local demo database

This fixture is **synthetic**. It is for local development and GitHub CI only. Never use its public passwords or document placeholders on a production deployment.

## Data set

The demo is installed into a **new and separate** `RentalPlatformDemo` MySQL database. It does not alter `RentalPlatformDb` or reset real users.

| Account | Role | Password | Notes |
|---|---|---|---|
| `admin@rentalplatform.vn` | Admin | `Admin@123` | Original Development Admin from `Program.cs`, restored in demo DB only |
| `owner1@rental.demo` | Owner | `Owner@123` | 6 vehicles, all five vehicle states represented |
| `owner2@rental.demo` | Owner | `Owner@123` | 6 vehicles, all five vehicle states represented |
| `customer1@rental.demo` | Customer | `Customer@123` | Completed multi-vehicle rental with a reviewed and an unreviewed item |
| `customer2@rental.demo` | Customer | `Customer@123` | Pending, confirmed, in-progress and cancelled examples |
| `customer3@rental.demo` | Customer | `Customer@123` | Payment and in-progress examples |

There are **six accounts total**: two owners, three customers, one default admin. User-provided test IDs, numbers, addresses and identity images are deliberately fake.

| Vehicle status | Number | Owner 1 | Owner 2 |
|---|---:|---:|---:|
| `SanSang` | 4 | 2 | 2 |
| `ChoDuyet` | 2 | 1 | 1 |
| `DangThue` | 2 | 1 | 1 |
| `BaoTri` | 2 | 1 | 1 |
| `TuChoi` | 2 | 1 | 1 |

Also included: 12 placeholder images, 8 new-format rental orders covering **all seven** `TrangThaiDonThue` states, 9 rental items (one multi-vehicle order), payment/contract snapshots and one GD3 review. The reviewed order contains a second item deliberately left unreviewed for the customer to test GD3 review creation.

## Fresh clone quick start on Windows

Prerequisites: .NET 10 SDK, MySQL Server 8.0+, `mysql.exe` and `dotnet` on `PATH`, local MySQL root credentials. Use a throwaway local MySQL instance; the script deliberately refuses an existing database.

```powershell
cd E:\Projects\motorbike-rental
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
& .\demo\setup-demo.ps1 -RepositoryPath (Get-Location).Path
# On DEMO SETUP PASS, run in the SAME PowerShell window:
dotnet run --project .\RentalPlatform\RentalPlatform.csproj -c Release --no-launch-profile
```

Open `http://127.0.0.1:5119`. Database connection is passed **only by process environment** and is never committed to source control. The script clears its temporary `MYSQL_PWD` variable before it exits. Close the shell afterward to discard the session connection string.

The script deliberately **does not DROP or overwrite any database**, and will stop if `RentalPlatformDemo` already exists. For another clean run, create a fresh local MySQL instance, or explicitly delete **only** the disposable `RentalPlatformDemo` database after verifying it does not contain user data. No drop command is bundled.

## Demo browser audit

Once the website is running, in a *second* PowerShell window:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\qa\run-gd4-live.ps1 -RepositoryPath (Get-Location).Path `
  -BaseUrl 'http://127.0.0.1:5119' -FixturePath (Join-Path (Get-Location) 'demo\demo-qa-fixture.json') -InstallDependencies
```

The fixture contains only publicly documented dummy account passwords; it must never be modified to contain real credentials and then committed. `scripts/qa/gd4-live-audit.mjs` supports `ownerPassword` and `adminPassword`, falling back to the existing `password` field for older GĐ4 fixtures.

## Important implementation notes

1. Schema first: `RentalPlatform/database-init-mysql.sql` contains the accepted GD2 MySQL migrations; `db/gd3_order_item_reviews.sql` installs the separately managed GD3 table; `demo/seed-demo.sql` inserts the fixture only into `RentalPlatformDemo`.
2. Demo identity documents are harmless copies of the checked-in placeholder PNG, stored under ignored `RentalPlatform/App_Data/IdentityDocuments/`; they are not real documents. Profile completeness and verification statuses are synthetic to allow controlled journey testing.
3. All dates are relative to database current UTC time, so the same fixture can be used at different dates.
4. `demo/verify-demo.sql` uses `SIGNAL SQLSTATE '45000'` when account counts, state coverage or important relationships do not match expectations.
5. The script intentionally refuses reuse of an existing `RentalPlatformDemo` rather than overwriting passwords, vehicle statuses or orders somebody may have changed while testing.
6. `RentalPlatform/database-seed-mysql.sql` is a legacy seed that targets the regular database and does *not* meet this demo contract. **Do not run it** as part of this setup.
7. The web application itself still has an insecure default development admin fallback in `Program.cs`. This demo seed does not change that application policy; do not deploy Development mode or these credentials publicly.

## CI acceptance

The `demo-database` GitHub workflow provisions an isolated MySQL 8 service, installs the GD2+GD3 schema, seeds/validates all 5 vehicle statuses, builds the app and verifies login across three roles with Chromium using the checked-in demo fixture. It never touches a developer's local MySQL or the live server.
