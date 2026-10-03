# Server Screen Viewer

Server Screen Viewer 1.0.0 is a small, **view-only** Windows desktop host that captures the current interactive desktop and makes it available in a browser. Use it only on systems you own or are authorized to administer.

The host has no remote keyboard, mouse, shell, file-transfer, microphone, camera, hidden recording, or stealth feature. It uses a generated access key, short-lived browser sessions, secure cookies when TLS is enabled, anti-caching headers, and a safe loopback-only default.

## Important behavior

Server Screen Viewer must run in the **same signed-in interactive Windows session** whose desktop you want to view. Do not install it as a Windows service: Windows Session 0 isolation prevents a service from reliably capturing a user's desktop. A locked, disconnected, or display-less session may produce a black or unavailable image.

The optional start-at-logon task is created for the Windows user who runs the installer. Run the elevated installer from the actual user account whose interactive desktop should be captured; do not use a different administrator account.

## Included features

- Windows Forms host on .NET 8
- Browser-based screen viewer; no viewer-side installation
- Entire virtual desktop or one selected monitor
- Random six-digit access code generated on first run, with temporary lockout after repeated incorrect attempts
- Optional TLS using a PFX certificate
- Configurable taskbar, minimized-start, and notification-area behavior
- PowerShell build, install, and uninstall scripts

## Requirements

- Windows Server 2019, 2022, or 2025 with Desktop Experience, or Windows 10/11
- PowerShell 5.1 or later
- .NET 8 SDK to build or run the installer
- Administrator rights to install under `%ProgramFiles%` and register or remove the start-at-logon task

The published application is self-contained, so a target machine does not need a separately installed .NET runtime after the application has been built.

## Complete setup commands

Open PowerShell. If installing, open it **as Administrator** while signed in as the user whose desktop will be captured.

If the .NET 8 SDK is not installed and `winget` is available:

```powershell
winget install --id Microsoft.DotNet.SDK.8 --exact --source winget
```

Close and reopen PowerShell after installing the SDK, then confirm it is available:

```powershell
dotnet --info
dotnet --list-sdks
```

At least one SDK version beginning with `8.` must be listed.

If starting from the source ZIP, place it in the current directory and run:

```powershell
Expand-Archive -LiteralPath .\ServerScreenViewer-1.0.0-source.zip -DestinationPath . -Force
Set-Location .\ServerScreenViewer
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
```

`Set-ExecutionPolicy -Scope Process` affects only the current PowerShell process. If the archive is already extracted, simply change to its `ServerScreenViewer` directory and run the execution-policy command.

## Build commands

From the extracted `ServerScreenViewer` directory, build a self-contained, single-file 64-bit Intel/AMD Windows executable:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Build.ps1 -Runtime win-x64
```

For Windows on ARM64:

```powershell
.\scripts\Build.ps1 -Runtime win-arm64
```

The output is written to:

```text
.\publish\ServerScreenViewer.exe
```

Equivalent direct .NET command for x64:

```powershell
dotnet publish .\src\ServerScreenViewer\ServerScreenViewer.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output .\publish `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None `
  -p:DebugSymbols=false
```

## Install and run commands

The installer builds the application, copies it to `%ProgramFiles%\ServerScreenViewer`, and starts it immediately.

### Install without start-at-logon

Run in an elevated PowerShell window from the extracted source directory:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Install.ps1 -Runtime win-x64
```

ARM64:

```powershell
.\scripts\Install.ps1 -Runtime win-arm64
```

### Install and enable start-at-logon

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Install.ps1 -Runtime win-x64 -StartAtLogon
```

This creates or replaces a scheduled task named **Server Screen Viewer** for the current user. The task:

- triggers when that user signs in;
- runs only in that user's interactive session;
- runs with the highest privileges available to that user; and
- starts `%ProgramFiles%\ServerScreenViewer\ServerScreenViewer.exe`.

The installer also starts the application immediately; signing out is not required.

### Run a build without installing it

```powershell
Start-Process -FilePath "$PWD\publish\ServerScreenViewer.exe"
```

### Start the installed application later

Directly start the installed executable:

```powershell
Start-Process -FilePath "$env:ProgramFiles\ServerScreenViewer\ServerScreenViewer.exe"
```

Or, if the start-at-logon task is registered, start it on demand:

```powershell
Start-ScheduledTask -TaskName 'Server Screen Viewer'
```

Only one instance can run in a Windows session. Starting it again displays an already-running message and exits the second instance.

## First run and browser access

1. Start `ServerScreenViewer.exe`. The installer does this automatically.
2. The application creates `%LOCALAPPDATA%\ServerScreenViewer\appsettings.json` for the user running it and generates a random six-digit access code.
3. In the **Server Screen Viewer** host window, select **Open viewer** for local testing.
4. Sign in to the browser viewer with the access code shown in the host window. **Copy access key** copies it to the clipboard. Five incorrect codes from one device temporarily block further attempts for 15 minutes.
5. For remote use, configure a VPN/tunnel or TLS before exposing the listener.

The default local URL is `http://127.0.0.1:8787/`.

## View from another device on the same network

For a trusted private network only, stop the application and edit `%LOCALAPPDATA%\ServerScreenViewer\appsettings.json` to set:

```json
{
  "BindAddress": "0.0.0.0",
  "AllowInsecureRemote": true
}
```

Keep the other settings in the file. Restart the application; its host window shows the viewer URL to open on the other device. If Windows Firewall blocks the connection, run this once in an elevated PowerShell window:

```powershell
New-NetFirewallRule -DisplayName 'Server Screen Viewer LAN' `
  -Direction Inbound -Action Allow -Protocol TCP -LocalPort 8787 -Profile Private
```

The LAN option uses HTTP, so the access code and screen are not encrypted in transit. Use it only on a trusted private network, never on public or guest Wi-Fi. For stronger protection, use the HTTPS or VPN options below.

## Stop the application

### Stop it from the host window

1. Open the **Server Screen Viewer** host window.
2. Select **Exit**.

If the window is hidden, double-click the **Server Screen Viewer** notification-area icon, or right-click it and choose **Open host window**; then select **Exit**.

### Stop it directly from the notification area

1. Find the **Server Screen Viewer** icon in the Windows notification area. It may be under **Show hidden icons** (`^`).
2. Right-click the icon.
3. Select **Exit**.

When `EnableTrayIcon` is `true`, clicking the host window's **X** only hides the window and leaves the viewer running. Use **Exit** to stop it.

### Stop it with a command

PowerShell:

```powershell
Get-Process -Name ServerScreenViewer -ErrorAction SilentlyContinue | Stop-Process -Force
```

Command Prompt alternative:

```cmd
taskkill /IM ServerScreenViewer.exe /F
```

These commands terminate the running process. They do not disable the start-at-logon task, uninstall the application, or delete user configuration.

## Disable start-at-logon

Open PowerShell as Administrator.

To disable the task but keep it registered for possible re-enabling:

```powershell
Disable-ScheduledTask -TaskName 'Server Screen Viewer'
```

Re-enable it later with:

```powershell
Enable-ScheduledTask -TaskName 'Server Screen Viewer'
```

To remove start-at-logon entirely:

```powershell
Unregister-ScheduledTask -TaskName 'Server Screen Viewer' -Confirm:$false
```

Verify whether the task is present and see its current state:

```powershell
Get-ScheduledTask -TaskName 'Server Screen Viewer' -ErrorAction SilentlyContinue
```

Disabling or unregistering the task does not stop an instance that is already running. Stop it separately using **Exit** or one of the stop commands above.

## Uninstall commands

Open PowerShell as Administrator in the extracted source directory.

Uninstall the application, stop any running instance, and remove the start-at-logon task while preserving the current user's configuration and access key:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Uninstall.ps1
```

Uninstall and also remove the current user's configuration and access key:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Uninstall.ps1 -RemoveUserConfiguration
```

The uninstall script removes `%ProgramFiles%\ServerScreenViewer`. It does not remove the source folder or source ZIP.

If the source directory is unavailable, the equivalent manual uninstall commands are:

```powershell
Get-Process -Name ServerScreenViewer -ErrorAction SilentlyContinue | Stop-Process -Force
Unregister-ScheduledTask -TaskName 'Server Screen Viewer' -Confirm:$false -ErrorAction SilentlyContinue
Remove-Item "$env:ProgramFiles\ServerScreenViewer" -Recurse -Force -ErrorAction SilentlyContinue
```

## Remove user configuration only

Stop the application before deleting its configuration. To remove the current user's settings and generated access key without uninstalling the application:

```powershell
Get-Process -Name ServerScreenViewer -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item "$env:LOCALAPPDATA\ServerScreenViewer" -Recurse -Force -ErrorAction SilentlyContinue
```

At the next start, the application recreates `appsettings.json` with defaults and generates a new access key.

Configuration is per Windows user. To remove another user's configuration, sign in as that user and run the command, or, as an administrator, delete the corresponding `AppData\Local\ServerScreenViewer` directory from that user's profile after confirming the application is stopped.

## Taskbar and startup-window configuration

Stop the program, then edit `%LOCALAPPDATA%\ServerScreenViewer\appsettings.json`:

```json
{
  "HideFromTaskbar": true,
  "StartMinimized": true,
  "EnableTrayIcon": true
}
```

- `HideFromTaskbar: true` removes the main host window's normal taskbar button.
- `HideFromTaskbar: false` gives the host window a normal taskbar button.
- `StartMinimized: true` hides or minimizes the host window after the listener starts.
- `EnableTrayIcon: true` provides the notification-area icon used to reopen or exit the application.

The combination `HideFromTaskbar: true`, `StartMinimized: true`, and `EnableTrayIcon: false` is rejected so that the UI cannot become inaccessible. These settings affect normal UI presentation only; the process remains visible to Task Manager, installed-app inventory, security tools, and administrators.

Restart the application after changing configuration:

```powershell
Get-Process -Name ServerScreenViewer -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Process -FilePath "$env:ProgramFiles\ServerScreenViewer\ServerScreenViewer.exe"
```

## Remote access options

### Recommended: VPN, reverse proxy, or SSH tunnel

Keep the default listener:

```json
"BindAddress": "127.0.0.1"
```

Use an existing VPN, an HTTPS reverse proxy, or an authenticated SSH tunnel. This avoids exposing unencrypted screen images on the LAN.

### Direct HTTPS listener

Stop the application and configure a PFX certificate plus a non-loopback address in `%LOCALAPPDATA%\ServerScreenViewer\appsettings.json`:

```json
{
  "BindAddress": "0.0.0.0",
  "Port": 8787,
  "CertificatePath": "C:\\Certificates\\server-viewer.pfx",
  "CertificatePassword": "",
  "AllowInsecureRemote": false
}
```

Prefer setting the PFX password in the `SSV_CERT_PASSWORD` environment variable rather than storing it in JSON. Allow only the required management subnet in Windows Firewall; adjust the subnet and port in this example:

```powershell
New-NetFirewallRule -DisplayName 'Server Screen Viewer HTTPS' `
  -Direction Inbound -Action Allow -Protocol TCP -LocalPort 8787 `
  -RemoteAddress 10.20.30.0/24
```

Restart the application, browse to `https://SERVER-NAME:8787/`, and use a certificate whose subject or SAN matches the server name.

### Insecure LAN mode (not recommended)

A non-loopback listener without a certificate is blocked by default. Setting `AllowInsecureRemote` to `true` overrides that protection, but the access key and screen images then travel over HTTP and can be intercepted. Use this only on a trusted, isolated network for temporary testing.

## Configuration reference

| Setting | Default | Meaning |
|---|---:|---|
| `BindAddress` | `127.0.0.1` | Listener IP; `0.0.0.0` listens on all IPv4 interfaces. |
| `Port` | `8787` | Browser listener port. |
| `HideFromTaskbar` | `false` | Hides only the host window's taskbar button when true. |
| `StartMinimized` | `false` | Starts the host window minimized or hidden. |
| `EnableTrayIcon` | `true` | Shows the administrator notification-area icon. |
| `CaptureIntervalMs` | `500` | Browser refresh interval; valid range 200-10000 ms. |
| `JpegQuality` | `70` | JPEG quality; valid range 20-95. |
| `MaxWidth` | `1920` | Downscales wide captures; valid range 640-7680. |
| `MonitorIndex` | `-1` | `-1` for all monitors; `0`, `1`, and so on for one monitor. |
| `SessionMinutes` | `60` | Browser login lifetime; valid range 5-1440 minutes. |
| `AllowInsecureRemote` | `false` | Explicitly permits non-loopback HTTP. |
| `CertificatePath` | empty | PFX path; enables HTTPS when set. |
| `CertificatePassword` | empty | PFX password; the environment variable is preferred. |
| `ApiKey` | generated | Six-digit access code; the app replaces values that are not exactly six digits when it starts. |

Configuration changes take effect after the application is restarted.

## Security checklist

- Use the application only on servers you own or are authorized to administer.
- Prefer a VPN/tunnel or HTTPS; never expose it directly to the public internet.
- Limit any firewall rule to a management subnet or jump host.
- Protect `%LOCALAPPDATA%\ServerScreenViewer\appsettings.json`; it contains the access key.
- Rotate the access key if it may have been disclosed.
- Use a dedicated, least-privileged Windows account where practical.
- Keep the capture interval conservative to reduce CPU and bandwidth use.
- Review `SECURITY.md` before deployment.

## Known limitations

- View-only snapshot streaming, not a low-latency RDP replacement
- No Windows-service mode because interactive desktop capture would be unreliable
- No audio or remote input
- In-memory browser sessions end when the host restarts
- Multiple simultaneous viewers increase capture and network load, although capture calls are serialized
