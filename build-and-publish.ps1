<#
    Builds the React SPA into the API's wwwroot, then publishes the ASP.NET Core
    app (self-contained web.config for the IIS ASP.NET Core Module) to .\publish.

    Usage:
        pwsh ./build-and-publish.ps1                # publish to .\publish
        pwsh ./build-and-publish.ps1 -Output D:\inetpub\FreshserviceArchive
#>
param(
    [string]$Output = "$PSScriptRoot\publish",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$web = "$PSScriptRoot\src\freshservice-archives-web"
$api = "$PSScriptRoot\src\FreshServiceArchives.Api"

Write-Host "==> Building React front-end..." -ForegroundColor Cyan
Push-Location $web
npm ci
npm run build   # emits into ../FreshServiceArchives.Api/wwwroot
Pop-Location

Write-Host "==> Publishing ASP.NET Core API (includes wwwroot)..." -ForegroundColor Cyan
dotnet publish "$api\FreshServiceArchives.Api.csproj" -c $Configuration -o $Output

Write-Host ""
Write-Host "Done. Deploy the contents of '$Output' to your IIS site folder." -ForegroundColor Green
Write-Host "Then follow the IIS setup steps in README.md (App Pool + Windows Authentication)." -ForegroundColor Green
