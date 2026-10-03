#!/usr/bin/env python3
"""Static package checks that do not require Windows or the .NET SDK."""
from pathlib import Path
import json
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src" / "ServerScreenViewer"

required = [
    ROOT / "README.md",
    ROOT / "SECURITY.md",
    ROOT / "PROMPT.md",
    ROOT / "appsettings.example.json",
    ROOT / "scripts" / "Build.ps1",
    ROOT / "scripts" / "Install.ps1",
    ROOT / "scripts" / "Uninstall.ps1",
    PROJECT / "ServerScreenViewer.csproj",
    PROJECT / "Program.cs",
    PROJECT / "AppConfig.cs",
    PROJECT / "HostForm.cs",
    PROJECT / "WebHostService.cs",
    PROJECT / "ScreenCapture.cs",
]

errors = []
for path in required:
    if not path.is_file() or path.stat().st_size == 0:
        errors.append(f"missing or empty: {path.relative_to(ROOT)}")

try:
    config = json.loads((ROOT / "appsettings.example.json").read_text(encoding="utf-8"))
except Exception as exc:
    errors.append(f"invalid configuration JSON: {exc}")
    config = {}

expected_defaults = {
    "BindAddress": "127.0.0.1",
    "HideFromTaskbar": False,
    "EnableTrayIcon": True,
    "AllowInsecureRemote": False,
}
for key, expected in expected_defaults.items():
    if config.get(key) != expected:
        errors.append(f"unsafe or unexpected default for {key}: {config.get(key)!r}")

try:
    tree = ET.parse(PROJECT / "ServerScreenViewer.csproj")
    text = ET.tostring(tree.getroot(), encoding="unicode")
    for required_value in ["net8.0-windows", "Microsoft.AspNetCore.App", "UseWindowsForms"]:
        if required_value not in text:
            errors.append(f"project file missing {required_value}")
except Exception as exc:
    errors.append(f"invalid project XML: {exc}")

host_form = (PROJECT / "HostForm.cs").read_text(encoding="utf-8")
if "ShowInTaskbar = !_config.HideFromTaskbar;" not in host_form:
    errors.append("taskbar visibility is not wired to HideFromTaskbar")

config_code = (PROJECT / "AppConfig.cs").read_text(encoding="utf-8")
for required_value in ["AllowInsecureRemote", "IPAddress.IsLoopback", "RandomNumberGenerator.GetBytes(24)"]:
    if required_value not in config_code:
        errors.append(f"configuration security check missing {required_value}")

web_host = (PROJECT / "WebHostService.cs").read_text(encoding="utf-8")
for required_value in ["/login", "/logout", "/api/snapshot", "FixedTimeEquals", "HttpOnly = true", "SameSiteMode.Strict"]:
    if required_value not in web_host:
        errors.append(f"web security or viewer feature missing {required_value}")

if errors:
    print("FAILED")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print(f"PASS: {len(required)} required files and security/configuration invariants verified")
