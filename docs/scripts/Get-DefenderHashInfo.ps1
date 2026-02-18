<#
.SYNOPSIS
    Query Microsoft Defender for Endpoint for file hash information

.DESCRIPTION
    Authenticates via Azure AD and queries Defender API for file hash reputation.
    Retrieves metadata including classification, prevalence, and first/last seen dates.

.PARAMETER TenantId
    Azure AD Tenant ID (GUID)

.PARAMETER AppId
    Azure AD App Registration Client ID (GUID)

.PARAMETER AppSecret
    Azure AD App Registration Client Secret

.PARAMETER FileHash
    File hash to query (SHA-1, SHA-256, or MD5)

.EXAMPLE
    .\Get-DefenderHashInfo.ps1 -TenantId "00000000-0000-0000-0000-000000000000" `
                               -AppId "11111111-1111-1111-1111-111111111111" `
                               -AppSecret "your-secret" `
                               -FileHash "abc123def456..."

.NOTES
    Requires:
    - Azure AD App Registration with WindowsDefenderATP API permissions
    - File.Read.All permission granted with admin consent
    - Microsoft Defender for Endpoint license

.LINK
    https://learn.microsoft.com/en-us/defender-endpoint/api/get-file-information
#>

param(
    [Parameter(Mandatory=$true, HelpMessage="Azure AD Tenant ID")]
    [ValidatePattern('^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')]
    [string]$TenantId,
    
    [Parameter(Mandatory=$true, HelpMessage="Azure AD App Registration Client ID")]
    [ValidatePattern('^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')]
    [string]$AppId,
    
    [Parameter(Mandatory=$true, HelpMessage="Azure AD App Registration Client Secret")]
    [string]$AppSecret,
    
    [Parameter(Mandatory=$true, HelpMessage="File hash (SHA-1, SHA-256, or MD5)")]
    [ValidatePattern('^[0-9a-fA-F]{32}$|^[0-9a-fA-F]{40}$|^[0-9a-fA-F]{64}$')]
    [string]$FileHash
)

# Set error action preference
$ErrorActionPreference = "Stop"

function Get-DefenderAccessToken {
    <#
    .SYNOPSIS
        Acquire OAuth 2.0 access token for Defender API
    #>
    param(
        [string]$TenantId,
        [string]$AppId,
        [string]$AppSecret
    )
    
    $tokenUri = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token"
    $body = @{
        client_id     = $AppId
        scope         = "https://api.securitycenter.microsoft.com/.default"
        client_secret = $AppSecret
        grant_type    = "client_credentials"
    }
    
    Write-Verbose "Token URI: $tokenUri"
    
    try {
        $response = Invoke-RestMethod -Method Post -Uri $tokenUri -Body $body -ContentType "application/x-www-form-urlencoded"
        return $response.access_token
    }
    catch {
        Write-Error "Failed to acquire access token: $_"
        throw
    }
}

function Get-DefenderFileInfo {
    <#
    .SYNOPSIS
        Query Defender API for file hash information
    #>
    param(
        [string]$FileHash,
        [string]$AccessToken
    )
    
    $uri = "https://api.security.microsoft.com/api/files/$FileHash"
    $headers = @{
        Authorization = "Bearer $AccessToken"
        "Content-Type" = "application/json"
    }
    
    Write-Verbose "Query URI: $uri"
    
    try {
        $response = Invoke-RestMethod -Headers $headers -Uri $uri -Method Get
        return $response
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 404) {
            Write-Warning "Hash not found in Defender database: $FileHash"
            return $null
        }
        else {
            Write-Error "Failed to query file info: $_"
            if ($_.ErrorDetails.Message) {
                Write-Error "Error details: $($_.ErrorDetails.Message)"
            }
            throw
        }
    }
}

function Format-DefenderFileInfo {
    <#
    .SYNOPSIS
        Format and display Defender file information
    #>
    param(
        [object]$FileInfo
    )
    
    Write-Host "`n$('='*70)" -ForegroundColor Cyan
    Write-Host "Microsoft Defender for Endpoint - File Information" -ForegroundColor Cyan
    Write-Host "$('='*70)`n" -ForegroundColor Cyan
    
    # File Hashes
    Write-Host "File Hashes:" -ForegroundColor Yellow
    if ($FileInfo.md5) {
        Write-Host "  MD5:    $($FileInfo.md5)"
    }
    if ($FileInfo.sha1) {
        Write-Host "  SHA1:   $($FileInfo.sha1)"
    }
    if ($FileInfo.sha256) {
        Write-Host "  SHA256: $($FileInfo.sha256)"
    }
    
    # Classification
    Write-Host "`nClassification:" -ForegroundColor Yellow
    $determination = $FileInfo.determinationType
    $determinationColor = switch ($determination) {
        "Malware" { "Red" }
        "PUA" { "Yellow" }
        "Clean" { "Green" }
        default { "White" }
    }
    Write-Host "  Type: " -NoNewline
    Write-Host $determination -ForegroundColor $determinationColor
    
    if ($FileInfo.determinationValue) {
        Write-Host "  Value: $($FileInfo.determinationValue)"
    }
    
    # Prevalence
    Write-Host "`nPrevalence:" -ForegroundColor Yellow
    if ($FileInfo.globalPrevalence) {
        Write-Host "  Global: $($FileInfo.globalPrevalence) devices"
    }
    if ($FileInfo.globalFirstObserved) {
        Write-Host "  First Seen: $($FileInfo.globalFirstObserved)"
    }
    if ($FileInfo.globalLastObserved) {
        Write-Host "  Last Seen: $($FileInfo.globalLastObserved)"
    }
    
    # File Metadata
    Write-Host "`nFile Metadata:" -ForegroundColor Yellow
    if ($FileInfo.size) {
        $sizeKB = [math]::Round($FileInfo.size / 1KB, 2)
        Write-Host "  Size: $sizeKB KB ($($FileInfo.size) bytes)"
    }
    if ($FileInfo.fileType) {
        Write-Host "  Type: $($FileInfo.fileType)"
    }
    if ($FileInfo.isPeFile -ne $null) {
        Write-Host "  PE File: $($FileInfo.isPeFile)"
    }
    
    # Signer Information
    if ($FileInfo.signer) {
        Write-Host "`nSigner Information:" -ForegroundColor Yellow
        Write-Host "  Signer: $($FileInfo.signer)"
        if ($FileInfo.issuer) {
            Write-Host "  Issuer: $($FileInfo.issuer)"
        }
        if ($FileInfo.signerHash) {
            Write-Host "  Signer Hash: $($FileInfo.signerHash)"
        }
    }
    
    Write-Host "`n$('='*70)`n" -ForegroundColor Cyan
}

# Main execution
try {
    Write-Host "Microsoft Defender for Endpoint - Hash Lookup Tool" -ForegroundColor Green
    Write-Host "="*70 -ForegroundColor Green
    
    # Step 1: Acquire access token
    Write-Host "`n[1/2] Acquiring OAuth access token..." -ForegroundColor Cyan
    $token = Get-DefenderAccessToken -TenantId $TenantId -AppId $AppId -AppSecret $AppSecret
    Write-Host "      Access token acquired successfully" -ForegroundColor Green
    
    # Step 2: Query file hash
    Write-Host "`n[2/2] Querying file hash: $FileHash" -ForegroundColor Cyan
    $fileInfo = Get-DefenderFileInfo -FileHash $FileHash -AccessToken $token
    
    if ($fileInfo) {
        Write-Host "      File information retrieved successfully" -ForegroundColor Green
        Format-DefenderFileInfo -FileInfo $fileInfo
        
        # Return the object for further processing
        return $fileInfo
    }
    else {
        Write-Host "      Hash not found in Defender database" -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host "`nError: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
