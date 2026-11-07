param(
    [string]$RepositoryPath = (Join-Path $PSScriptRoot '..'),
    [string]$HostName = '127.0.0.1',
    [int]$Port = 3306,
    [string]$MySqlUser = 'root'
)
# Windows PowerShell 5.1 safe: this script contains ASCII only.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path -LiteralPath $RepositoryPath).Path
$schema = Join-Path $root 'RentalPlatform\database-init-mysql.sql'
$reviews = Join-Path $root 'db\gd3_order_item_reviews.sql'
$seed = Join-Path $root 'demo\seed-demo.sql'
$verify = Join-Path $root 'demo\verify-demo.sql'
$fixture = Join-Path $root 'demo\demo-qa-fixture.json'
$img = Join-Path $root 'RentalPlatform\wwwroot\image\no-image.png'
foreach ($file in @($schema,$reviews,$seed,$verify,$fixture,$img)) {
    if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing file: $file" }
}
if (!(Get-Command mysql -ErrorAction SilentlyContinue)) { throw 'mysql.exe is not on PATH' }
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet SDK is not on PATH' }
$database = 'RentalPlatformDemo'
$secure = Read-Host 'MySQL local password (never saved to Git)' -AsSecureString
$ptr = [IntPtr]::Zero
try {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    $pass = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    # MYSQL_PWD is scoped to this local process and cleared after setup.
    # A local dev convenience: avoid using this on a shared machine or production.
    $env:MYSQL_PWD = $pass
    $common = @('--protocol=tcp','--default-character-set=utf8mb4',"--host=$HostName","--port=$Port","--user=$MySqlUser")
    $exists = & mysql @common --batch --skip-column-names --execute "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME='RentalPlatformDemo';"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot authenticate to local MySQL' }
    if (($exists | Out-String).Trim() -ne '0') {
        throw 'RentalPlatformDemo already exists. No modifications were made. Use an empty MySQL instance or inspect the existing demo DB.'
    }
    & mysql @common --execute 'CREATE DATABASE `RentalPlatformDemo` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create demo database' }

    function Import-Sql([string]$file,[string]$label) {
        Write-Host "Importing: $label" -ForegroundColor Cyan
        $args = @('--protocol=tcp','--default-character-set=utf8mb4',"--host=$HostName","--port=$Port","--user=$MySqlUser",'--database=RentalPlatformDemo')
        # RedirectStandardInput handles .sql file encoding reliably on PowerShell 5.1.
        $proc = Start-Process -FilePath (Get-Command mysql).Source -ArgumentList $args -RedirectStandardInput $file -Wait -PassThru -NoNewWindow
        if ($proc.ExitCode -ne 0) { throw "SQL failed: $label. Only RentalPlatformDemo may have been created/partially filled." }
    }
    # The checked-in EF-generated SQL can start with a UTF-8 BOM. Normalize the
    # temporary input; never rewrite the tracked schema file.
    $tmpSchema = Join-Path $env:TEMP ('rental-demo-schema-' + [Guid]::NewGuid().ToString('N') + '.sql')
    try {
        $schemaText = [IO.File]::ReadAllText($schema, [Text.Encoding]::UTF8).TrimStart([char]0xFEFF)
        [IO.File]::WriteAllText($tmpSchema, $schemaText, (New-Object Text.UTF8Encoding($false)))
        Import-Sql $tmpSchema 'baseline schema GD0-GD2'
    } finally {
        Remove-Item -LiteralPath $tmpSchema -Force -ErrorAction SilentlyContinue
    }
    Import-Sql $reviews 'GD3 reviews table'
    Import-Sql $seed 'synthetic demo users, vehicles, orders'
    Import-Sql $verify 'strict demo verification'

    $profile = Join-Path $root 'RentalPlatform\App_Data\IdentityDocuments'
    New-Item -ItemType Directory -Force -Path $profile | Out-Null
    Copy-Item -LiteralPath $img -Destination (Join-Path $profile 'demo-id-front.png') -Force
    Copy-Item -LiteralPath $img -Destination (Join-Path $profile 'demo-id-back.png') -Force

    # This environment change is intentionally retained for dotnet run in THIS shell.
    $builder = New-Object System.Data.Common.DbConnectionStringBuilder
    $builder['Server'] = $HostName
    $builder['Port'] = $Port
    $builder['Database'] = $database
    $builder['User ID'] = $MySqlUser
    $builder['Password'] = $pass
    $builder['CharSet'] = 'utf8mb4'
    $builder['SslMode'] = 'Required'
    $env:ConnectionStrings__DefaultConnection = $builder.ConnectionString
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'http://127.0.0.1:5119'
    $env:Payment__DemoEnabled = 'true'
    Write-Host 'DEMO SETUP PASS. App configured for RentalPlatformDemo in this PowerShell session.' -ForegroundColor Green
    Write-Host 'Run: dotnet run --project RentalPlatform/RentalPlatform.csproj -c Release --no-launch-profile'
    Write-Host 'Accounts: see demo/README.md; never deploy demo passwords.'
}
finally {
    Remove-Item Env:MYSQL_PWD -ErrorAction SilentlyContinue
    if ($ptr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
    Remove-Variable pass -ErrorAction SilentlyContinue
    Remove-Variable secure -ErrorAction SilentlyContinue
}
