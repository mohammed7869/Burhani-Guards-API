# PowerShell script to configure Windows Firewall for API access
# Run this script as Administrator

Write-Host "Configuring Windows Firewall for Burhani Guards API..." -ForegroundColor Green

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Add inbound rule for port 5000
$ruleName = "BurhaniGuards-API-Port5000"
$existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

if ($existingRule) {
    Write-Host "Firewall rule already exists. Removing old rule..." -ForegroundColor Yellow
    Remove-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
}

Write-Host "Adding firewall rule for port 5000..." -ForegroundColor Cyan
New-NetFirewallRule -DisplayName $ruleName `
    -Direction Inbound `
    -LocalPort 5000 `
    -Protocol TCP `
    -Action Allow `
    -Description "Allow inbound connections to Burhani Guards API on port 5000"

if ($?) {
    Write-Host "Firewall rule added successfully!" -ForegroundColor Green
    Write-Host "Your API should now be accessible from other devices on the same network." -ForegroundColor Green
} else {
    Write-Host "Failed to add firewall rule. Please check the error above." -ForegroundColor Red
}

Write-Host "`nTo verify, check Windows Firewall -> Inbound Rules for '$ruleName'" -ForegroundColor Cyan

