# Reusable development prompt

Copy and use the following prompt with a coding assistant when you want to extend this project:

---

Build and maintain a production-minded **view-only Windows Server screen viewer** named `ServerScreenViewer` using C# and .NET 8 Windows Forms.

Requirements:

1. Run in the signed-in interactive Windows session and capture either the entire virtual desktop (`MonitorIndex = -1`) or a selected monitor.
2. Host a browser viewer from the same application with an adjustable JPEG snapshot interval, quality, and maximum width.
3. Require a cryptographically random access key. After login, use short-lived, random, HttpOnly, SameSite=Strict browser sessions held only in memory. Mark the cookie Secure under HTTPS.
4. Default to `127.0.0.1`. Support a PFX certificate for HTTPS. Refuse a non-loopback HTTP listener unless the administrator explicitly enables `AllowInsecureRemote`.
5. Add no-store/no-cache, frame-denial, content-type, and restrictive content-security headers. Do not place credentials in a URL.
6. Make `HideFromTaskbar` configurable: when true, set the host form's `ShowInTaskbar` property to false; when false, show a normal taskbar button. Keep `EnableTrayIcon` as a separate setting. Never hide the process from Task Manager, security software, inventory, or administrators.
7. Support `StartMinimized`, but reject a configuration that combines `HideFromTaskbar = true`, `StartMinimized = true`, and `EnableTrayIcon = false`, because that would make the UI inaccessible.
8. Provide a visible host status window with viewer URL, access-key copy button, configuration shortcut, and Exit action.
9. Remain strictly view-only. Do not add remote keyboard/mouse control, a command shell, arbitrary process execution, credential capture, file transfer, persistence beyond an optional clearly named start-at-logon task, monitoring evasion, or hidden recording.
10. Include build/install/uninstall PowerShell scripts, a configuration example, README deployment steps, and security documentation.
11. Preserve compatibility with Windows Server Desktop Experience. Explain that the program cannot reliably capture the desktop from a Windows service because of Session 0 isolation and may not capture a locked or disconnected session.
12. Keep dependencies minimal and use only the .NET shared frameworks where practical.

Before returning changes, compile for `net8.0-windows`, publish a self-contained `win-x64` build, test login/session expiration/snapshot access, verify taskbar behavior for both boolean values, and summarize security-impacting changes.

---
