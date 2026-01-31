# Browser COOP/COEP Headers Fix

## The Problem

Browsers require `Cross-Origin-Opener-Policy` (COOP) and `Cross-Origin-Embedder-Policy` (COEP) headers to enable `SharedArrayBuffer`, which is required for .NET WASM threading.

**However**, browsers only trust these headers from "secure contexts":
- ✅ `localhost` (trusted)
- ✅ `127.0.0.1` (usually trusted)
- ✅ HTTPS URLs
- ❌ **IP addresses over HTTP are NOT trusted**

## Why You Keep Seeing This Error

When you access `http://192.168.0.171:3141/BSO/`, the browser sees an "untrustworthy origin" and ignores the COOP/COEP headers, even though the server is sending them correctly.

## Solutions

### ✅ Solution 1: Use `localhost` (Recommended for Local Development)

**Change your URL from:**
```
http://192.168.0.171:3141/BSO/
```

**To:**
```
http://localhost:3141/BSO/
```

This works immediately - no configuration changes needed.

### ✅ Solution 2: Use `127.0.0.1` (Alternative)

```
http://127.0.0.1:3141/BSO/
```

### ⚠️ Solution 3: Set Up HTTPS (Required for Network Access)

If you need to access from another device on your network, you MUST use HTTPS:

1. Generate a self-signed certificate:
```powershell
# Run as Administrator
New-SelfSignedCertificate -DnsName "localhost", "192.168.0.171" -CertStoreLocation "cert:\LocalMachine\My" -FriendlyName "BSO Dev Cert"
```

2. Configure Kestrel in `MotelyApiHost.cs` to use HTTPS

3. Access via: `https://192.168.0.171:3141/BSO/`

### ❌ Solution 4: Browser Flags (NOT Recommended)

You can start Chrome with flags to allow insecure origins:
```powershell
chrome.exe --unsafely-treat-insecure-origin-as-secure=http://192.168.0.171:3141
```

**Warning:** This is insecure and should only be used for development.

## ✅ Permanent Fix Applied

**Update:** `ApiServerWindow.cs` now automatically listens on **both** `localhost` and the configured IP address. This means:

- ✅ Browser can use `http://localhost:3141/BSO/` (COOP/COEP headers work)
- ✅ Network access still works via `http://192.168.0.171:3141/BSO/` (for other devices)

**After restarting Motely.TUI**, the API will be available on both URLs. Just use `localhost` in your browser!

## Quick Fix Right Now

**Just use `localhost` instead of the IP address:**

```
http://localhost:3141/BSO/
```

The headers are already configured correctly - the browser just needs a trusted origin to accept them.
