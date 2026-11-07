# Demo Database

See [complete setup and safety instructions](../docs/DEMO_DATABASE.md).

Quick start, fresh clone in Windows PowerShell:

```powershell
Set-ExecutionPolicy -Scope Process Bypass -Force
& .\demo\setup-demo.ps1 -RepositoryPath (Get-Location).Path
# after DEMO SETUP PASS, in the SAME console:
dotnet run --project .\RentalPlatform\RentalPlatform.csproj -c Release --no-launch-profile
```

**Credentials (demo DB only):** `admin@rentalplatform.vn / Admin@123`; `owner1@rental.demo`, `owner2@rental.demo / Owner@123`; `customer1@rental.demo`, `customer2@rental.demo`, `customer3@rental.demo / Customer@123`. This creates 2 owners, 3 customers and 1 default admin, 12 vehicles spanning all 5 statuses and 8 rental orders spanning all 7 order statuses. Never reuse any demo password in production.
