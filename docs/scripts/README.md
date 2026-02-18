# AV Hash Submission Scripts

This directory contains example scripts for automating file hash submission and lookup across various antivirus and threat intelligence platforms.

## Available Scripts

### 1. Get-DefenderHashInfo.ps1 (PowerShell)

Query Microsoft Defender for Endpoint for file hash information.

**Requirements:**
- PowerShell 5.1 or later
- Azure AD App Registration with appropriate permissions
- Microsoft Defender for Endpoint license

**Usage:**
```powershell
.\Get-DefenderHashInfo.ps1 -TenantId "tenant-guid" `
                           -AppId "app-guid" `
                           -AppSecret "secret" `
                           -FileHash "hash-value"
```

**Setup:**
1. Create an Azure AD App Registration
2. Grant `File.Read.All` permission (WindowsDefenderATP API)
3. Create a client secret
4. Grant admin consent

**Example:**
```powershell
.\Get-DefenderHashInfo.ps1 `
    -TenantId "00000000-0000-0000-0000-000000000000" `
    -AppId "11111111-1111-1111-1111-111111111111" `
    -AppSecret "your-secret-here" `
    -FileHash "275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538aabf651fd0f"
```

---

### 2. virustotal_hash_lookup.py (Python)

Query VirusTotal API v3 for comprehensive multi-engine scan results.

**Requirements:**
- Python 3.7+
- `requests` library: `pip install requests`
- VirusTotal API key

**Usage:**
```bash
export VT_API_KEY="your-api-key"
python virustotal_hash_lookup.py <file_hash>
```

**Features:**
- Detailed scan results from 70+ AV engines
- Threat classification and malware family identification
- Reputation scoring and popularity metrics
- Sandbox analysis verdicts (if available)
- Colored output with emoji indicators

**Example:**
```bash
export VT_API_KEY="abc123def456..."
python virustotal_hash_lookup.py d41d8cd98f00b204e9800998ecf8427e
```

**Exit Codes:**
- `0`: Clean or Unknown
- `1`: Suspicious
- `2`: Malicious (high or medium threat)
- `3`: Error

---

### 3. multi_platform_checker.py (Python)

Check file hash reputation across multiple platforms simultaneously and aggregate results.

**Supported Platforms:**
- VirusTotal (70+ engines)
- Hybrid Analysis (Falcon Sandbox)
- AlienVault OTX (threat intelligence)
- MetaDefender (30+ engines)

**Requirements:**
- Python 3.7+
- `requests` library: `pip install requests`
- API keys for desired platforms (at least one required)

**Usage:**
```bash
export VT_API_KEY="your-vt-key"
export HA_API_KEY="your-ha-key"
export OTX_API_KEY="your-otx-key"
export MD_API_KEY="your-md-key"
python multi_platform_checker.py <file_hash>
```

**Features:**
- Parallel checking across multiple platforms
- Aggregated threat assessment with recommendations
- Normalized scoring (0-100) for comparison
- Detailed statistics and summary
- Smart verdict logic based on consensus

**Example:**
```bash
# Configure API keys
export VT_API_KEY="your-virustotal-key"
export HA_API_KEY="your-hybrid-analysis-key"
export OTX_API_KEY="your-otx-key"
export MD_API_KEY="your-metadefender-key"

# Check hash
python multi_platform_checker.py 275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538aabf651fd0f
```

**Exit Codes:**
- `0`: Clean
- `1`: Suspicious
- `2`: Malicious
- `3`: Unknown/Error

---

## Getting API Keys

### VirusTotal
1. Sign up at https://www.virustotal.com
2. Navigate to your profile settings
3. Copy your API key from the API Key section
4. **Free tier**: 4 requests per minute

### Hybrid Analysis
1. Register at https://www.hybrid-analysis.com
2. Log in and navigate to your user profile
3. Generate and copy your API key
4. **Free tier**: Limited submissions

### AlienVault OTX
1. Sign up at https://otx.alienvault.com
2. Navigate to your profile/settings
3. Copy your OTX API key
4. **Free tier**: Community access

### MetaDefender (OPSWAT)
1. Register at https://metadefender.opswat.com
2. Access your account dashboard
3. Generate and copy your API key
4. **Free tier**: Limited daily submissions

### Microsoft Defender for Endpoint
1. Register an Azure AD App at https://portal.azure.com
2. Navigate to Azure Active Directory → App Registrations → New Registration
3. Grant API permissions: `WindowsDefenderATP` → `File.Read.All`
4. Create a client secret under Certificates & secrets
5. **Requires**: Enterprise Defender for Endpoint license

---

## Script Comparison

| Feature | Get-DefenderHashInfo | virustotal_hash_lookup | multi_platform_checker |
|---------|---------------------|------------------------|------------------------|
| **Language** | PowerShell | Python | Python |
| **Platforms** | 1 (Defender) | 1 (VirusTotal) | 4 (VT, HA, OTX, MD) |
| **Auth Type** | OAuth 2.0 | API Key | API Keys |
| **Output** | Detailed metadata | Comprehensive scan results | Aggregated assessment |
| **Best For** | Enterprise Defender | Quick multi-engine check | Comprehensive analysis |
| **Rate Limits** | Azure standard | 4/min (free) | Varies per platform |

---

## Common Use Cases

### 1. Quick Hash Check (VirusTotal)
```bash
export VT_API_KEY="your-key"
python virustotal_hash_lookup.py <hash>
```
**Best for**: Fast multi-engine verdict

### 2. Enterprise Threat Intel (Defender)
```powershell
.\Get-DefenderHashInfo.ps1 -TenantId $tid -AppId $aid -AppSecret $sec -FileHash $hash
```
**Best for**: Organizations with Defender for Endpoint

### 3. Comprehensive Analysis (Multi-Platform)
```bash
python multi_platform_checker.py <hash>
```
**Best for**: Critical decisions requiring multiple sources

### 4. Batch Processing (Loop)
```bash
# Check multiple hashes from file
while read hash; do
    python virustotal_hash_lookup.py "$hash"
    sleep 15  # Respect rate limits
done < hashes.txt
```

### 5. CI/CD Integration
```yaml
# Example GitHub Actions step
- name: Check file hash
  run: |
    export VT_API_KEY="${{ secrets.VT_API_KEY }}"
    python scripts/virustotal_hash_lookup.py ${{ steps.compute_hash.outputs.hash }}
  continue-on-error: false
```

---

## Security Best Practices

### API Key Management
- **Never commit API keys** to version control
- Use environment variables or secret managers (Azure Key Vault, AWS Secrets Manager)
- Rotate keys regularly
- Use separate keys for dev/prod environments

### Rate Limiting
- Implement exponential backoff for retries
- Cache results to avoid duplicate queries
- Respect vendor rate limits (VirusTotal: 4/min free tier)
- Monitor API usage and quotas

### Data Privacy
- Be aware of where data is processed (cloud regions)
- Public submissions on free tiers become community data
- Consider paid/private scanning for sensitive files
- Review vendor privacy policies

### Error Handling
- Implement robust try-catch blocks
- Log errors for debugging
- Provide user-friendly error messages
- Use appropriate exit codes for automation

---

## Troubleshooting

### "Authentication failed" Error
- Verify API key is correct and active
- Check for extra whitespace in key
- Ensure environment variable is set correctly
- For Defender: verify app registration permissions and admin consent

### "Rate limit exceeded" Error
- Wait for rate limit window to reset (typically 1 minute)
- Implement delays between requests
- Consider upgrading to paid API tier
- Cache previous results

### "Hash not found" Result
- Hash may not exist in platform's database
- Try uploading the actual file for analysis
- Check hash format (MD5/SHA-1/SHA-256)
- Verify hash is correctly computed

### Connection Timeouts
- Check internet connectivity
- Verify firewall/proxy settings
- Increase timeout values in scripts
- Some platforms may be temporarily unavailable

---

## Integration Examples

### SOAR Platform Integration
```python
# Splunk SOAR example
def check_hash_reputation(hash_value):
    result = run_script("multi_platform_checker.py", hash_value)
    if result.exit_code == 2:  # Malicious
        block_hash(hash_value)
        create_incident(hash_value, result.output)
    return result
```

### SIEM Alert Enrichment
```python
# Example: Enrich SIEM alert with hash reputation
def enrich_alert(alert):
    file_hash = alert.get('file_hash')
    if file_hash:
        vt_result = check_virustotal(file_hash)
        alert['vt_score'] = vt_result.raw_score
        alert['vt_verdict'] = vt_result.verdict
    return alert
```

### PowerShell Module
```powershell
# Create reusable function
function Test-FileHashReputation {
    param([string]$Hash)
    
    $vt = python virustotal_hash_lookup.py $Hash
    if ($LASTEXITCODE -eq 2) {
        Write-Warning "Malicious hash detected: $Hash"
        return $false
    }
    return $true
}
```

---

## Additional Resources

- [Main Research Document](../AV-HASH-SUBMISSION-RESEARCH.md) - Comprehensive guide to AV hash submission automation
- [VirusTotal API Docs](https://docs.virustotal.com/reference/overview)
- [Microsoft Defender API Docs](https://learn.microsoft.com/en-us/defender-endpoint/api/apis-intro)
- [Hybrid Analysis API](https://www.hybrid-analysis.com/docs/api/v2)
- [AlienVault OTX API](https://otx.alienvault.com/assets/static/external_api.html)
- [MetaDefender API](https://www.opswat.com/docs/mdcloud/metadefender-cloud-api-v4)

---

## Contributing

To add new scripts or improve existing ones:
1. Follow existing code style and conventions
2. Include comprehensive error handling
3. Add usage examples and documentation
4. Test with various hash types and edge cases
5. Document any new dependencies

---

## License

These scripts are provided as examples for educational and research purposes. Review vendor terms of service for API usage limitations and restrictions.
