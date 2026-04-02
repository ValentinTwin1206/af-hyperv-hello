<#
.SYNOPSIS
    Registers an AF_HYPERV socket service GUID in the Windows registry.

.DESCRIPTION
    AF_HYPERV requires the service GUID to be registered under:
      HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Virtualization\GuestCommunicationServices

    Without this registration, Connect() fails with:
      "An attempt was made to access a socket in a way forbidden by its access permissions."

    The service GUID is derived from the VSOCK port number using the well-known template:
      {port_as_8_hex_digits}-facb-11e6-bd58-64006a7986d3

    This is a one-time setup per machine. The script self-elevates to Administrator
    if not already running elevated.

.PARAMETER Port
    The VSOCK port number to register. Must be between 1 and 65535.
    Default: 5000

.PARAMETER Unregister
    Remove the registry entry for the given port instead of creating it.

.EXAMPLE
    .\Register-HvService.ps1
    Registers the default port 5000 as service GUID 00001388-facb-11e6-bd58-64006a7986d3.

.EXAMPLE
    .\Register-HvService.ps1 -Port 6000
    Registers port 6000 as service GUID 00001770-facb-11e6-bd58-64006a7986d3.

.EXAMPLE
    .\Register-HvService.ps1 -Unregister
    Removes the registry entry for port 5000.

.EXAMPLE
    .\Register-HvService.ps1 -Port 6000 -Unregister
    Removes the registry entry for port 6000.

.LINK
    https://learn.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/make-integration-service
#>
param(
    [ValidateRange(1, 65535)]
    [int] $Port = 5000,

    [switch] $Unregister
)

# Self-elevate if not already running as Administrator.
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host 'Not running as Administrator - relaunching elevated...' -ForegroundColor Yellow
    $scriptPath = $MyInvocation.MyCommand.Definition
    $argList = "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" -Port $Port"
    if ($Unregister) { $argList += ' -Unregister' }
    Start-Process pwsh -ArgumentList $argList -Verb RunAs
    exit
}

$portHex    = $Port.ToString('x8')
$serviceGuid = "$portHex-facb-11e6-bd58-64006a7986d3"
$regBase    = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Virtualization\GuestCommunicationServices'
$regPath    = Join-Path $regBase $serviceGuid

if ($Unregister) {
    if (Test-Path $regPath) {
        Remove-Item -Path $regPath -Recurse
        Write-Host "Unregistered: $serviceGuid (port $Port)" -ForegroundColor Green
    } else {
        Write-Host "Not found in registry: $serviceGuid (port $Port)" -ForegroundColor Yellow
    }
    return
}

if (Test-Path $regPath) {
    Write-Host "Already registered: $serviceGuid (port $Port)" -ForegroundColor Yellow
} else {
    $key = New-Item -Path $regBase -Name $serviceGuid
    $key.SetValue('ElementName', "AF_HYPERV Hello World (port $Port)")
    Write-Host "Registered: $serviceGuid (port $Port)" -ForegroundColor Green
}
