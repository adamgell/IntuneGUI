#!/usr/bin/env python3
"""
VirusTotal Hash Lookup Script

Query VirusTotal API v3 for file hash reputation and scan results.

Usage:
    python virustotal_hash_lookup.py <file_hash>

Environment Variables:
    VT_API_KEY: VirusTotal API key

Example:
    export VT_API_KEY="your-api-key-here"
    python virustotal_hash_lookup.py d41d8cd98f00b204e9800998ecf8427e

Requirements:
    pip install requests

API Documentation:
    https://docs.virustotal.com/reference/overview
"""

import sys
import os
import requests
import json
from typing import Dict, Optional
from datetime import datetime

def get_virustotal_report(file_hash: str, api_key: str) -> Optional[Dict]:
    """
    Query VirusTotal for file hash report.
    
    Args:
        file_hash: MD5, SHA-1, or SHA-256 file hash
        api_key: VirusTotal API key
        
    Returns:
        Dict containing scan results or None on error
    """
    url = f"https://www.virustotal.com/api/v3/files/{file_hash}"
    headers = {
        "x-apikey": api_key,
        "Accept": "application/json"
    }
    
    try:
        response = requests.get(url, headers=headers, timeout=30)
        response.raise_for_status()
        return response.json()
    except requests.exceptions.HTTPError as e:
        if e.response.status_code == 404:
            print(f"❌ Hash not found in VirusTotal: {file_hash}")
        elif e.response.status_code == 401:
            print("❌ Authentication failed. Check your API key.")
        elif e.response.status_code == 429:
            print("❌ Rate limit exceeded. Wait before retrying.")
        else:
            print(f"❌ HTTP Error {e.response.status_code}: {e}")
        return None
    except requests.exceptions.Timeout:
        print("❌ Request timed out. Try again later.")
        return None
    except requests.exceptions.RequestException as e:
        print(f"❌ Error querying VirusTotal: {e}")
        return None

def format_timestamp(timestamp: int) -> str:
    """
    Format Unix timestamp to human-readable date.
    
    Args:
        timestamp: Unix timestamp
        
    Returns:
        Formatted date string
    """
    if not timestamp:
        return "N/A"
    try:
        dt = datetime.fromtimestamp(timestamp)
        return dt.strftime("%Y-%m-%d %H:%M:%S UTC")
    except:
        return str(timestamp)

def parse_scan_results(data: Dict) -> None:
    """
    Parse and display VirusTotal scan results.
    
    Args:
        data: VirusTotal API response data
    """
    if not data or "data" not in data:
        print("❌ No scan data available")
        return
    
    attributes = data["data"]["attributes"]
    stats = attributes.get("last_analysis_stats", {})
    
    print("\n" + "="*70)
    print("VirusTotal Scan Results")
    print("="*70)
    
    # File hashes
    print(f"\n📋 File Hashes:")
    print(f"   MD5:    {attributes.get('md5', 'N/A')}")
    print(f"   SHA1:   {attributes.get('sha1', 'N/A')}")
    print(f"   SHA256: {attributes.get('sha256', 'N/A')}")
    
    # Detection statistics
    print(f"\n🔍 Detection Summary:")
    malicious = stats.get('malicious', 0)
    suspicious = stats.get('suspicious', 0)
    undetected = stats.get('undetected', 0)
    harmless = stats.get('harmless', 0)
    timeout = stats.get('timeout', 0)
    unsupported = stats.get('type-unsupported', 0)
    failure = stats.get('failure', 0)
    
    print(f"   Malicious:     {malicious:3d}")
    print(f"   Suspicious:    {suspicious:3d}")
    print(f"   Undetected:    {undetected:3d}")
    print(f"   Harmless:      {harmless:3d}")
    print(f"   Timeout:       {timeout:3d}")
    print(f"   Unsupported:   {unsupported:3d}")
    print(f"   Failure:       {failure:3d}")
    
    # Overall verdict
    total_engines = sum(stats.values())
    flagged_count = malicious + suspicious
    
    print(f"\n📊 Overall: {flagged_count}/{total_engines} engines flagged this file")
    
    # Verdict with emoji
    if malicious >= 5:
        print("   Verdict: 🔴 HIGHLY MALICIOUS")
        threat_level = "HIGH"
    elif malicious > 0:
        print("   Verdict: 🟠 MALICIOUS")
        threat_level = "MEDIUM"
    elif suspicious > 0:
        print("   Verdict: 🟡 SUSPICIOUS")
        threat_level = "LOW"
    elif undetected > 0 or harmless > 0:
        print("   Verdict: 🟢 CLEAN")
        threat_level = "NONE"
    else:
        print("   Verdict: ⚪ UNKNOWN")
        threat_level = "UNKNOWN"
    
    # File metadata
    print(f"\n📄 File Metadata:")
    size = attributes.get('size', 0)
    size_mb = size / (1024 * 1024)
    print(f"   Size: {size_mb:.2f} MB ({size:,} bytes)")
    print(f"   Type: {attributes.get('type_description', 'N/A')}")
    print(f"   Magic: {attributes.get('magic', 'N/A')}")
    
    # Names
    names = attributes.get('names', [])
    if names:
        print(f"\n🏷️  Known Names:")
        for name in names[:5]:  # Show first 5 names
            print(f"   - {name}")
        if len(names) > 5:
            print(f"   ... and {len(names) - 5} more")
    
    # Timestamps
    first_submission = attributes.get('first_submission_date')
    last_analysis = attributes.get('last_analysis_date')
    last_modification = attributes.get('last_modification_date')
    
    print(f"\n⏰ Timestamps:")
    print(f"   First Submission: {format_timestamp(first_submission)}")
    print(f"   Last Analysis:    {format_timestamp(last_analysis)}")
    print(f"   Last Modified:    {format_timestamp(last_modification)}")
    
    # Reputation and popularity
    reputation = attributes.get('reputation', 0)
    times_submitted = attributes.get('times_submitted', 0)
    
    print(f"\n📈 Reputation:")
    print(f"   Score: {reputation}")
    print(f"   Times Submitted: {times_submitted}")
    
    # Popular threat labels
    popular_threat_category = attributes.get('popular_threat_classification', {})
    suggested_threat_label = popular_threat_category.get('suggested_threat_label')
    popular_threat_name = popular_threat_category.get('popular_threat_name', [])
    
    if suggested_threat_label or popular_threat_name:
        print(f"\n🦠 Threat Classification:")
        if suggested_threat_label:
            print(f"   Suggested Label: {suggested_threat_label}")
        if popular_threat_name:
            for threat in popular_threat_name[:3]:  # Show top 3
                threat_name = threat.get('value', 'Unknown')
                threat_count = threat.get('count', 0)
                print(f"   - {threat_name} (detected by {threat_count} engines)")
    
    # Detailed engine results (only flagged)
    print(f"\n🛡️  Detailed Engine Results (Flagged Only):")
    results = attributes.get("last_analysis_results", {})
    
    # Separate by category
    malicious_results = []
    suspicious_results = []
    
    for engine, result in results.items():
        category = result.get("category", "unknown")
        result_text = result.get("result", "N/A")
        
        if category == "malicious":
            malicious_results.append((engine, result_text))
        elif category == "suspicious":
            suspicious_results.append((engine, result_text))
    
    if malicious_results:
        print(f"\n   🔴 Malicious ({len(malicious_results)}):")
        for engine, result_text in sorted(malicious_results)[:10]:  # Show first 10
            print(f"      [{engine}] {result_text}")
        if len(malicious_results) > 10:
            print(f"      ... and {len(malicious_results) - 10} more")
    
    if suspicious_results:
        print(f"\n   🟡 Suspicious ({len(suspicious_results)}):")
        for engine, result_text in sorted(suspicious_results)[:10]:  # Show first 10
            print(f"      [{engine}] {result_text}")
        if len(suspicious_results) > 10:
            print(f"      ... and {len(suspicious_results) - 10} more")
    
    if not malicious_results and not suspicious_results:
        print("   ✅ No engines flagged this file as malicious or suspicious")
    
    # Sandbox verdicts (if available)
    sandbox_verdicts = attributes.get('sandbox_verdicts', {})
    if sandbox_verdicts:
        print(f"\n🔬 Sandbox Analysis:")
        for sandbox, verdict in sandbox_verdicts.items():
            category = verdict.get('category', 'unknown')
            malware_names = verdict.get('malware_names', [])
            print(f"   [{sandbox}] {category}")
            if malware_names:
                print(f"      Malware: {', '.join(malware_names[:3])}")
    
    # Links
    print(f"\n🔗 Links:")
    file_id = data["data"]["id"]
    print(f"   VirusTotal: https://www.virustotal.com/gui/file/{file_id}")
    
    print("="*70 + "\n")
    
    # Return threat level for scripting
    return threat_level

def main():
    """Main execution function."""
    if len(sys.argv) < 2:
        print("VirusTotal Hash Lookup Tool")
        print("="*70)
        print("\nUsage:")
        print("  python virustotal_hash_lookup.py <file_hash>")
        print("\nEnvironment Variables:")
        print("  VT_API_KEY: VirusTotal API key")
        print("\nExample:")
        print("  export VT_API_KEY='your-api-key'")
        print("  python virustotal_hash_lookup.py d41d8cd98f00b204e9800998ecf8427e")
        print("\nGet your API key at: https://www.virustotal.com")
        print("="*70)
        sys.exit(1)
    
    file_hash = sys.argv[1].strip()
    api_key = os.environ.get("VT_API_KEY")
    
    if not api_key:
        print("❌ Error: VT_API_KEY environment variable not set")
        print("\nSet it with:")
        print("  export VT_API_KEY='your-api-key'  # Linux/Mac")
        print("  $env:VT_API_KEY='your-api-key'    # PowerShell")
        sys.exit(1)
    
    # Validate hash format
    hash_length = len(file_hash)
    if hash_length not in [32, 40, 64]:
        print(f"❌ Error: Invalid hash length ({hash_length} characters)")
        print("   Expected: 32 (MD5), 40 (SHA-1), or 64 (SHA-256)")
        sys.exit(1)
    
    if not all(c in '0123456789abcdefABCDEF' for c in file_hash):
        print("❌ Error: Hash contains invalid characters")
        print("   Expected: Hexadecimal characters only (0-9, a-f, A-F)")
        sys.exit(1)
    
    print(f"🔍 Querying VirusTotal for hash: {file_hash}")
    print("   Please wait...\n")
    
    data = get_virustotal_report(file_hash, api_key)
    
    if data:
        threat_level = parse_scan_results(data)
        
        # Exit with appropriate code
        if threat_level in ["HIGH", "MEDIUM"]:
            sys.exit(2)  # Malicious
        elif threat_level == "LOW":
            sys.exit(1)  # Suspicious
        else:
            sys.exit(0)  # Clean or Unknown
    else:
        sys.exit(3)  # Error

if __name__ == "__main__":
    main()
