# Troubleshooting Local VitePress Development

If you're having issues accessing the local documentation preview at `http://localhost:5173/`, try these solutions.

## Issue: Connection Refused

### Symptoms
- Browser shows "Connection refused" or "Can't connect"
- Error: `ERR_CONNECTION_REFUSED`
- VitePress dev server starts successfully but browser can't access it

### Solutions

#### Solution 1: Try Different URLs

VitePress may bind to IPv6 only. Try all these URLs:

```
✓ http://localhost:5173/
✓ http://127.0.0.1:5173/
✓ http://[::1]:5173/        (IPv6 format)
✓ http://0.0.0.0:5173/
```

#### Solution 2: Use the Helper Script

We've created a helper script that handles everything:

```powershell
cd Docs
.\dev.ps1
```

This script:
- Checks for npm installation
- Installs dependencies if needed
- Starts the server with proper configuration
- Shows all available URLs

#### Solution 3: Check Windows Firewall

Windows Firewall may be blocking Node.js:

**Option A: Allow through PowerShell (Recommended)**
```powershell
# Run as Administrator
$nodePath = (Get-Command node).Source
New-NetFirewallRule -DisplayName "Node.js VitePress Dev Server" `
    -Direction Inbound `
    -Program $nodePath `
    -Action Allow `
    -Protocol TCP `
    -LocalPort 5173 `
    -Profile Private,Domain
```

**Option B: Allow through GUI**
1. Open **Windows Defender Firewall with Advanced Security**
2. Click **Inbound Rules** → **New Rule**
3. Select **Program** → Browse to `node.exe` (usually in `C:\Program Files\nodejs\`)
4. Allow the connection
5. Apply to Private and Domain profiles
6. Name it "Node.js Development Server"

#### Solution 4: Check if Port is Already in Use

```powershell
# Check what's using port 5173
netstat -ano | findstr :5173

# If something else is using it, kill that process or use a different port
# To use a different port, edit Docs/.vitepress/config.js:
```

Edit `Docs/.vitepress/config.js`:
```javascript
export default {
  vite: {
    server: {
      host: '0.0.0.0',
      port: 8080  // Change to any available port
    }
  },
  // ... rest of config
}
```

Then access at `http://localhost:8080/`

#### Solution 5: Disable IPv6 Preference (Windows)

If your system prefers IPv6 but has connectivity issues:

```powershell
# Prefer IPv4 over IPv6
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" `
    -Name "DisabledComponents" -Value 0x20 -Type DWord

# Restart computer for changes to take effect
Restart-Computer
```

#### Solution 6: Use WSL Environment Variable

If you're on Windows and still having issues:

```powershell
# Set environment variable before starting
$env:HOST = "127.0.0.1"
npm run docs:dev
```

Or create `.env` file in `Docs/` folder:
```
HOST=127.0.0.1
```

## Issue: Slow Loading / Hot Reload Not Working

### Solution 1: Clear VitePress Cache

```powershell
cd Docs
Remove-Item -Recurse -Force .vitepress/.temp
Remove-Item -Recurse -Force .vitepress/cache
npm run docs:dev
```

### Solution 2: Clear npm Cache

```powershell
npm cache clean --force
cd Docs
Remove-Item -Recurse -Force node_modules
npm install
```

## Issue: Build Errors

### Check Node.js Version

VitePress requires Node.js 18 or higher:

```powershell
node --version
# Should show v18.x.x or higher
```

If outdated, download from https://nodejs.org/

### Reinstall Dependencies

```powershell
cd Docs
Remove-Item -Recurse -Force node_modules package-lock.json
npm install
```

## Issue: CSS Not Loading / Broken Styles

### Clear Browser Cache

1. **Chrome/Edge**: Ctrl+Shift+Delete → Clear cached images and files
2. **Firefox**: Ctrl+Shift+Delete → Cached Web Content
3. Or try **Incognito/Private** mode

### Hard Refresh

- **Windows**: Ctrl+F5 or Ctrl+Shift+R
- **Mac**: Cmd+Shift+R

## Issue: Images Not Displaying

### Check Image Paths

Images should be in `Docs/public/images/` and referenced as:

```markdown
![Alt text](/images/filename.png)
```

**NOT**:
```markdown
❌ ![Alt text](./public/images/filename.png)
❌ ![Alt text](../public/images/filename.png)
```

### Check Image Files Exist

```powershell
Get-ChildItem -Path "Docs\public\images" -Recurse
```

## Testing Connection

### Test 1: Check Server is Running

```powershell
# Should show Node.js listening on port 5173
netstat -ano | findstr :5173
```

### Test 2: Test with curl

```powershell
# Should return HTML
curl http://localhost:5173/
```

### Test 3: Check from Different Browser

Try:
- Chrome
- Firefox
- Edge
- Brave

Sometimes one browser has cached issues while others work.

## Still Having Issues?

### Collect Debug Information

```powershell
# System info
Write-Host "Node Version: $((node --version))"
Write-Host "npm Version: $((npm --version))"
Write-Host "OS: $([System.Environment]::OSVersion.VersionString)"

# Network info
Write-Host "`nListening ports:"
netstat -ano | findstr :5173

# Firewall rules
Write-Host "`nNode.js Firewall Rules:"
Get-NetFirewallApplicationFilter | Where-Object {$_.Program -like "*node.exe*"} | Get-NetFirewallRule | Select-Object DisplayName, Enabled, Direction, Action
```

### Check VitePress Logs

The terminal where you ran `npm run docs:dev` may show helpful error messages.

### Common Error Messages

**"EADDRINUSE: address already in use"**
- Port 5173 is already in use
- Solution: Kill the process or use a different port (see Solution 4 above)

**"ERR_CONNECTION_REFUSED"**
- Firewall blocking connection
- Solution: See Solution 3 above

**"Cannot GET /"**
- VitePress failed to build the site
- Solution: Check terminal for build errors

## Quick Reference

| Command | Purpose |
|---------|---------|
| `.\dev.ps1` | Start dev server (easiest) |
| `npm run docs:dev` | Start dev server (manual) |
| `npm run docs:build` | Build static site |
| `npm run docs:preview` | Preview built site |

| URL to Try | When to Use |
|------------|-------------|
| `http://localhost:5173/` | Default, try first |
| `http://127.0.0.1:5173/` | If localhost doesn't work |
| `http://[::1]:5173/` | If system prefers IPv6 |
| `http://0.0.0.0:5173/` | If bound to all interfaces |

## Contact

If none of these solutions work, please provide:
- Node.js version (`node --version`)
- Operating system version
- Error messages from terminal
- Browser console errors (F12 → Console tab)

Open an issue at: https://github.com/asolutionit/PoshUI/issues
