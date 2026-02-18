#!/usr/bin/env python3
"""
Multi-Platform Hash Checker

Query multiple AV platforms for file hash reputation and aggregate results.

Supported Platforms:
    - VirusTotal (multi-engine scanning)
    - Hybrid Analysis (Falcon Sandbox)
    - AlienVault OTX (threat intelligence)
    - MetaDefender (OPSWAT multi-engine)

Usage:
    python multi_platform_checker.py <file_hash>

Environment Variables:
    VT_API_KEY: VirusTotal API key
    HA_API_KEY: Hybrid Analysis API key
    OTX_API_KEY: AlienVault OTX API key
    MD_API_KEY: MetaDefender API key

Example:
    export VT_API_KEY="your-vt-key"
    export HA_API_KEY="your-ha-key"
    export OTX_API_KEY="your-otx-key"
    export MD_API_KEY="your-md-key"
    python multi_platform_checker.py d41d8cd98f00b204e9800998ecf8427e

Requirements:
    pip install requests
"""

import sys
import os
import requests
from dataclasses import dataclass
from typing import Dict, Optional, List
from enum import Enum
import time

class Verdict(Enum):
    """Hash verdict enumeration."""
    MALICIOUS = "malicious"
    SUSPICIOUS = "suspicious"
    CLEAN = "clean"
    UNKNOWN = "unknown"
    ERROR = "error"

@dataclass
class HashResult:
    """Hash check result from a platform."""
    platform: str
    verdict: Verdict
    details: str
    error: Optional[str] = None
    raw_score: Optional[float] = None  # Normalized 0-100 score

def check_virustotal(file_hash: str, api_key: str) -> HashResult:
    """
    Check hash on VirusTotal.
    
    Args:
        file_hash: MD5, SHA-1, or SHA-256 file hash
        api_key: VirusTotal API key
        
    Returns:
        HashResult object
    """
    if not api_key:
        return HashResult(
            "VirusTotal", 
            Verdict.UNKNOWN, 
            "API key not provided",
            "Set VT_API_KEY environment variable"
        )
    
    url = f"https://www.virustotal.com/api/v3/files/{file_hash}"
    headers = {"x-apikey": api_key}
    
    try:
        response = requests.get(url, headers=headers, timeout=15)
        
        if response.status_code == 404:
            return HashResult("VirusTotal", Verdict.UNKNOWN, "Hash not found in database")
        
        if response.status_code == 429:
            return HashResult(
                "VirusTotal", 
                Verdict.ERROR, 
                "Rate limit exceeded",
                "Wait 1 minute before retrying (free tier: 4 req/min)"
            )
        
        response.raise_for_status()
        data = response.json()
        
        stats = data["data"]["attributes"]["last_analysis_stats"]
        malicious = stats.get("malicious", 0)
        suspicious = stats.get("suspicious", 0)
        undetected = stats.get("undetected", 0)
        total = sum(stats.values())
        
        if total == 0:
            return HashResult("VirusTotal", Verdict.UNKNOWN, "No scan data available")
        
        flagged = malicious + suspicious
        percentage = (flagged / total) * 100
        details = f"{flagged}/{total} engines flagged ({percentage:.1f}%)"
        
        # Determine verdict
        if malicious >= 5:
            verdict = Verdict.MALICIOUS
        elif malicious > 0:
            verdict = Verdict.SUSPICIOUS
        elif suspicious >= 3:
            verdict = Verdict.SUSPICIOUS
        elif suspicious > 0:
            verdict = Verdict.SUSPICIOUS
        else:
            verdict = Verdict.CLEAN
        
        return HashResult("VirusTotal", verdict, details, raw_score=percentage)
    
    except requests.exceptions.RequestException as e:
        return HashResult("VirusTotal", Verdict.ERROR, "Query failed", str(e))

def check_hybrid_analysis(file_hash: str, api_key: str) -> HashResult:
    """
    Check hash on Hybrid Analysis (Falcon Sandbox).
    
    Args:
        file_hash: SHA-256 file hash (required)
        api_key: Hybrid Analysis API key
        
    Returns:
        HashResult object
    """
    if not api_key:
        return HashResult(
            "Hybrid Analysis", 
            Verdict.UNKNOWN, 
            "API key not provided",
            "Set HA_API_KEY environment variable"
        )
    
    # Hybrid Analysis requires SHA256
    if len(file_hash) != 64:
        return HashResult(
            "Hybrid Analysis", 
            Verdict.UNKNOWN, 
            "SHA-256 required",
            f"Provided hash length: {len(file_hash)}, expected: 64"
        )
    
    url = f"https://www.hybrid-analysis.com/api/v2/overview/{file_hash}"
    headers = {
        "api-key": api_key,
        "User-Agent": "Falcon Sandbox",
        "Accept": "application/json"
    }
    
    try:
        response = requests.get(url, headers=headers, timeout=15)
        
        if response.status_code == 404:
            return HashResult("Hybrid Analysis", Verdict.UNKNOWN, "Hash not found in database")
        
        if response.status_code == 429:
            return HashResult(
                "Hybrid Analysis", 
                Verdict.ERROR, 
                "Rate limit exceeded",
                "Check your account tier limits"
            )
        
        response.raise_for_status()
        data = response.json()
        
        threat_score = data.get("threat_score", 0)
        verdict_str = data.get("verdict", "unknown").lower()
        av_detect = data.get("av_detect", 0)
        
        # Build details string
        details = f"Threat score: {threat_score}/100"
        if av_detect:
            details += f", AV detections: {av_detect}%"
        
        # Determine verdict based on threat score and verdict string
        if "malicious" in verdict_str or threat_score >= 70:
            verdict = Verdict.MALICIOUS
        elif "suspicious" in verdict_str or threat_score >= 30:
            verdict = Verdict.SUSPICIOUS
        elif threat_score <= 10:
            verdict = Verdict.CLEAN
        else:
            verdict = Verdict.UNKNOWN
        
        return HashResult("Hybrid Analysis", verdict, details, raw_score=float(threat_score))
    
    except requests.exceptions.RequestException as e:
        return HashResult("Hybrid Analysis", Verdict.ERROR, "Query failed", str(e))

def check_otx(file_hash: str, api_key: str) -> HashResult:
    """
    Check hash on AlienVault OTX (Open Threat Exchange).
    
    Args:
        file_hash: MD5, SHA-1, or SHA-256 file hash
        api_key: AlienVault OTX API key
        
    Returns:
        HashResult object
    """
    if not api_key:
        return HashResult(
            "AlienVault OTX", 
            Verdict.UNKNOWN, 
            "API key not provided",
            "Set OTX_API_KEY environment variable"
        )
    
    url = f"https://otx.alienvault.com/api/v1/indicators/file/{file_hash}/general"
    headers = {"X-OTX-API-KEY": api_key}
    
    try:
        response = requests.get(url, headers=headers, timeout=15)
        
        if response.status_code == 404:
            return HashResult("AlienVault OTX", Verdict.UNKNOWN, "Hash not found in database")
        
        if response.status_code == 403:
            return HashResult(
                "AlienVault OTX", 
                Verdict.ERROR, 
                "Invalid API key",
                "Check your OTX API key"
            )
        
        response.raise_for_status()
        data = response.json()
        
        pulse_info = data.get("pulse_info", {})
        pulse_count = pulse_info.get("count", 0)
        pulses = pulse_info.get("pulses", [])
        
        # Get malware families if available
        malware_families = set()
        for pulse in pulses[:5]:  # Check first 5 pulses
            families = pulse.get("malware_families", [])
            for family in families:
                if isinstance(family, dict):
                    malware_families.add(family.get("display_name", "Unknown"))
                else:
                    malware_families.add(str(family))
        
        if pulse_count > 0:
            verdict = Verdict.SUSPICIOUS
            details = f"Found in {pulse_count} threat pulse(s)"
            if malware_families:
                families_str = ", ".join(list(malware_families)[:3])
                details += f" - {families_str}"
            # Normalize score: more pulses = higher score
            raw_score = min((pulse_count / 10) * 100, 100)
        else:
            verdict = Verdict.CLEAN
            details = "No threat intelligence found"
            raw_score = 0.0
        
        return HashResult("AlienVault OTX", verdict, details, raw_score=raw_score)
    
    except requests.exceptions.RequestException as e:
        return HashResult("AlienVault OTX", Verdict.ERROR, "Query failed", str(e))

def check_metadefender(file_hash: str, api_key: str) -> HashResult:
    """
    Check hash on MetaDefender (OPSWAT).
    
    Args:
        file_hash: MD5, SHA-1, or SHA-256 file hash
        api_key: MetaDefender API key
        
    Returns:
        HashResult object
    """
    if not api_key:
        return HashResult(
            "MetaDefender", 
            Verdict.UNKNOWN, 
            "API key not provided",
            "Set MD_API_KEY environment variable"
        )
    
    url = f"https://api.metadefender.com/v4/hash/{file_hash}"
    headers = {"apikey": api_key}
    
    try:
        response = requests.get(url, headers=headers, timeout=15)
        
        if response.status_code == 404:
            return HashResult("MetaDefender", Verdict.UNKNOWN, "Hash not found in database")
        
        if response.status_code == 401:
            return HashResult(
                "MetaDefender", 
                Verdict.ERROR, 
                "Invalid API key",
                "Check your MetaDefender API key"
            )
        
        response.raise_for_status()
        data = response.json()
        
        scan_results = data.get("scan_results", {})
        total_detected = scan_results.get("total_detected_avs", 0)
        total_avs = scan_results.get("total_avs", 1)
        
        if total_avs == 0:
            return HashResult("MetaDefender", Verdict.UNKNOWN, "No scan data available")
        
        percentage = (total_detected / total_avs) * 100
        details = f"{total_detected}/{total_avs} engines detected threat ({percentage:.1f}%)"
        
        # Determine verdict
        if total_detected >= 5:
            verdict = Verdict.MALICIOUS
        elif total_detected > 0:
            verdict = Verdict.SUSPICIOUS
        else:
            verdict = Verdict.CLEAN
        
        return HashResult("MetaDefender", verdict, details, raw_score=percentage)
    
    except requests.exceptions.RequestException as e:
        return HashResult("MetaDefender", Verdict.ERROR, "Query failed", str(e))

def print_results(file_hash: str, results: List[HashResult]) -> Verdict:
    """
    Print formatted results and return overall verdict.
    
    Args:
        file_hash: The file hash that was checked
        results: List of HashResult objects
        
    Returns:
        Overall Verdict
    """
    print("\n" + "="*70)
    print("Multi-Platform Hash Check Results")
    print("="*70)
    print(f"\nHash: {file_hash}")
    print(f"Hash Type: ", end="")
    
    hash_len = len(file_hash)
    if hash_len == 32:
        print("MD5")
    elif hash_len == 40:
        print("SHA-1")
    elif hash_len == 64:
        print("SHA-256")
    else:
        print("Unknown")
    
    print("\n" + "-"*70)
    
    # Define verdict symbols and colors
    symbols = {
        Verdict.MALICIOUS: "🔴",
        Verdict.SUSPICIOUS: "🟡",
        Verdict.CLEAN: "🟢",
        Verdict.UNKNOWN: "⚪",
        Verdict.ERROR: "❌"
    }
    
    # Track statistics
    malicious_count = 0
    suspicious_count = 0
    clean_count = 0
    unknown_count = 0
    error_count = 0
    
    # Print individual results
    for result in results:
        symbol = symbols.get(result.verdict, "⚪")
        print(f"\n{symbol} {result.platform}")
        print(f"   Verdict: {result.verdict.value.upper()}")
        print(f"   Details: {result.details}")
        
        if result.raw_score is not None:
            print(f"   Score: {result.raw_score:.1f}/100")
        
        if result.error:
            print(f"   Error: {result.error}")
        
        # Update counters
        if result.verdict == Verdict.MALICIOUS:
            malicious_count += 1
        elif result.verdict == Verdict.SUSPICIOUS:
            suspicious_count += 1
        elif result.verdict == Verdict.CLEAN:
            clean_count += 1
        elif result.verdict == Verdict.ERROR:
            error_count += 1
        else:
            unknown_count += 1
    
    # Calculate overall verdict
    print("\n" + "="*70)
    print("Summary Statistics")
    print("="*70)
    print(f"Malicious:  {malicious_count}")
    print(f"Suspicious: {suspicious_count}")
    print(f"Clean:      {clean_count}")
    print(f"Unknown:    {unknown_count}")
    print(f"Errors:     {error_count}")
    
    # Overall assessment
    print("\n" + "="*70)
    print("Overall Assessment")
    print("="*70)
    
    if malicious_count >= 2:
        overall = Verdict.MALICIOUS
        print("🔴 MALICIOUS - Multiple platforms detected threats")
        recommendation = "⚠️  BLOCK this file immediately and investigate"
    elif malicious_count == 1 and suspicious_count >= 1:
        overall = Verdict.MALICIOUS
        print("🔴 MALICIOUS - One platform detected as malicious, another as suspicious")
        recommendation = "⚠️  BLOCK this file and investigate"
    elif malicious_count == 1:
        overall = Verdict.SUSPICIOUS
        print("🟡 SUSPICIOUS - One platform detected as malicious")
        recommendation = "⚠️  Exercise caution, consider blocking"
    elif suspicious_count >= 2:
        overall = Verdict.SUSPICIOUS
        print("🟡 SUSPICIOUS - Multiple platforms flagged this file")
        recommendation = "⚠️  Exercise caution, investigate further"
    elif suspicious_count == 1:
        overall = Verdict.SUSPICIOUS
        print("🟡 SUSPICIOUS - One platform flagged this file")
        recommendation = "⚠️  Monitor closely, investigate if needed"
    elif clean_count >= 2:
        overall = Verdict.CLEAN
        print("🟢 CLEAN - Multiple platforms report no threats")
        recommendation = "✅ File appears safe"
    elif clean_count == 1 and unknown_count > 0:
        overall = Verdict.UNKNOWN
        print("⚪ UNKNOWN - Limited threat intelligence available")
        recommendation = "❓ Unable to make definitive assessment"
    else:
        overall = Verdict.UNKNOWN
        print("⚪ UNKNOWN - Insufficient data for assessment")
        recommendation = "❓ Consider uploading file for analysis"
    
    print(f"\nRecommendation: {recommendation}")
    print("="*70 + "\n")
    
    return overall

def main():
    """Main execution function."""
    if len(sys.argv) < 2:
        print("Multi-Platform Hash Checker")
        print("="*70)
        print("\nQuery multiple AV platforms for file hash reputation")
        print("\nUsage:")
        print("  python multi_platform_checker.py <file_hash>")
        print("\nSupported Platforms:")
        print("  - VirusTotal (70+ engines)")
        print("  - Hybrid Analysis (Falcon Sandbox)")
        print("  - AlienVault OTX (threat intelligence)")
        print("  - MetaDefender (30+ engines)")
        print("\nEnvironment Variables:")
        print("  VT_API_KEY:  VirusTotal API key")
        print("  HA_API_KEY:  Hybrid Analysis API key")
        print("  OTX_API_KEY: AlienVault OTX API key")
        print("  MD_API_KEY:  MetaDefender API key")
        print("\nExample:")
        print("  export VT_API_KEY='your-key'")
        print("  python multi_platform_checker.py d41d8cd98f00b204e9800998ecf8427e")
        print("\nNote: At least one API key should be configured.")
        print("="*70)
        sys.exit(1)
    
    file_hash = sys.argv[1].strip().lower()
    
    # Validate hash format
    hash_length = len(file_hash)
    if hash_length not in [32, 40, 64]:
        print(f"❌ Error: Invalid hash length ({hash_length} characters)")
        print("   Expected: 32 (MD5), 40 (SHA-1), or 64 (SHA-256)")
        sys.exit(1)
    
    if not all(c in '0123456789abcdef' for c in file_hash):
        print("❌ Error: Hash contains invalid characters")
        print("   Expected: Hexadecimal characters only (0-9, a-f)")
        sys.exit(1)
    
    # Get API keys from environment
    vt_key = os.environ.get("VT_API_KEY")
    ha_key = os.environ.get("HA_API_KEY")
    otx_key = os.environ.get("OTX_API_KEY")
    md_key = os.environ.get("MD_API_KEY")
    
    # Check if at least one key is configured
    if not any([vt_key, ha_key, otx_key, md_key]):
        print("❌ Error: No API keys configured")
        print("\nSet at least one API key:")
        print("  export VT_API_KEY='your-key'")
        print("  export HA_API_KEY='your-key'")
        print("  export OTX_API_KEY='your-key'")
        print("  export MD_API_KEY='your-key'")
        sys.exit(1)
    
    print(f"🔍 Checking hash across multiple platforms: {file_hash}")
    print("   This may take 10-20 seconds...\n")
    
    # Check all platforms (with small delays to avoid rate limiting)
    results = []
    
    if vt_key:
        results.append(check_virustotal(file_hash, vt_key))
        time.sleep(0.5)
    
    if ha_key:
        results.append(check_hybrid_analysis(file_hash, ha_key))
        time.sleep(0.5)
    
    if otx_key:
        results.append(check_otx(file_hash, otx_key))
        time.sleep(0.5)
    
    if md_key:
        results.append(check_metadefender(file_hash, md_key))
    
    # Print results and get overall verdict
    overall = print_results(file_hash, results)
    
    # Exit with appropriate code
    if overall == Verdict.MALICIOUS:
        sys.exit(2)
    elif overall == Verdict.SUSPICIOUS:
        sys.exit(1)
    elif overall == Verdict.CLEAN:
        sys.exit(0)
    else:
        sys.exit(3)  # Unknown or Error

if __name__ == "__main__":
    main()
