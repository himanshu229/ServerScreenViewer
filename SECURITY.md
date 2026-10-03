# Security notes

## Intended use

Server Screen Viewer is for authorized, view-only administration of Windows systems. Obtain approval before deploying it on a system used by another person. The application intentionally provides no remote-control, command-execution, credential-capture, file-transfer, microphone, camera, or stealth mechanism.

## Network exposure

The default listener is `127.0.0.1`, so it is reachable only from the host. For remote access, prefer a corporate VPN, authenticated tunnel, or HTTPS reverse proxy. Direct remote HTTP is blocked unless `AllowInsecureRemote` is explicitly enabled.

Do not expose the listener directly to the public internet. Restrict any inbound firewall rule to the smallest possible management subnet.

## Authentication and sessions

- A cryptographically random access key is generated on first run.
- Access-key comparison uses fixed-time SHA-256 digest comparison.
- Failed login attempts are delayed.
- Successful login creates a random, in-memory, HttpOnly, SameSite=Strict cookie.
- Cookies use the Secure flag whenever the application is configured for HTTPS.
- Sessions expire after `SessionMinutes` and are cleared on restart.
- Snapshot responses are marked no-store/no-cache.

The application does not yet provide account lockout, MFA, per-user identities, or centralized audit export. Put it behind an identity-aware proxy if those controls are required.

## Secrets

The access key is stored in `%LOCALAPPDATA%\ServerScreenViewer\appsettings.json`. Apply normal NTFS user-profile protections and do not copy the file into source control. For TLS, prefer the `SSV_CERT_PASSWORD` environment variable over the `CertificatePassword` JSON setting.

## UI visibility

`HideFromTaskbar` affects only the Windows taskbar button. `EnableTrayIcon` defaults to true, the process remains visible in Task Manager, the install path and scheduled task are plainly named, and no attempt is made to evade monitoring.

## Reporting issues

When reporting a security problem, do not include real access keys, certificates, server addresses, or screen captures. Include the application version/commit, Windows version, configuration with secrets removed, and reproducible steps.
