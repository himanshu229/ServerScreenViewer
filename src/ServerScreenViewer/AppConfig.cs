using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Text.Json;

namespace ServerScreenViewer;

internal sealed class AppConfig
{
    public string BindAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8787;
    public bool HideFromTaskbar { get; set; }
    public bool StartMinimized { get; set; }
    public bool EnableTrayIcon { get; set; } = true;
    public int CaptureIntervalMs { get; set; } = 500;
    public int JpegQuality { get; set; } = 70;
    public int MaxWidth { get; set; } = 1920;
    public int MonitorIndex { get; set; } = -1;
    public int SessionMinutes { get; set; } = 60;
    public bool AllowInsecureRemote { get; set; }
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    public bool UsesHttps => !string.IsNullOrWhiteSpace(CertificatePath);
    public string ViewerUrl => $"{(UsesHttps ? "https" : "http")}://{DisplayHost}:{Port}/";

    private string DisplayHost => BindAddress switch
    {
        "0.0.0.0" or "::" => GetLanAddress()?.ToString() ?? "localhost",
        _ => BindAddress
    };

    public static AppConfig LoadOrCreate()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        AppConfig config;

        if (File.Exists(AppPaths.ConfigFile))
        {
            var json = File.ReadAllText(AppPaths.ConfigFile);
            config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions())
                ?? throw new InvalidOperationException("The configuration file is empty or invalid.");
        }
        else
        {
            config = new AppConfig();
        }

        var changed = false;
        if (!IsSixDigitCode(config.ApiKey))
        {
            config.ApiKey = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            changed = true;
        }

        config.Validate();
        if (changed || !File.Exists(AppPaths.ConfigFile))
        {
            config.Save();
        }

        return config;
    }

    public void Save()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.WriteAllText(AppPaths.ConfigFile, JsonSerializer.Serialize(this, JsonOptions()));
    }

    public void Validate()
    {
        if (!IPAddress.TryParse(BindAddress, out var address))
            throw new InvalidOperationException("BindAddress must be an IP address such as 127.0.0.1 or 0.0.0.0.");
        if (Port is < 1 or > 65535)
            throw new InvalidOperationException("Port must be between 1 and 65535.");
        if (CaptureIntervalMs is < 200 or > 10000)
            throw new InvalidOperationException("CaptureIntervalMs must be between 200 and 10000.");
        if (JpegQuality is < 20 or > 95)
            throw new InvalidOperationException("JpegQuality must be between 20 and 95.");
        if (MaxWidth is < 640 or > 7680)
            throw new InvalidOperationException("MaxWidth must be between 640 and 7680.");
        if (SessionMinutes is < 5 or > 1440)
            throw new InvalidOperationException("SessionMinutes must be between 5 and 1440.");
        if (HideFromTaskbar && StartMinimized && !EnableTrayIcon)
            throw new InvalidOperationException("EnableTrayIcon must be true when HideFromTaskbar and StartMinimized are both true, so the app remains accessible.");
        var isLoopback = IPAddress.IsLoopback(address)
            || (address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4()));
        if (!isLoopback && !UsesHttps && !AllowInsecureRemote)
            throw new InvalidOperationException("Remote listening without TLS is blocked. Configure CertificatePath or explicitly set AllowInsecureRemote to true after accepting the LAN-security risk.");
        if (UsesHttps && !File.Exists(Environment.ExpandEnvironmentVariables(CertificatePath)))
            throw new InvalidOperationException($"The TLS certificate file does not exist: {CertificatePath}");
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static bool IsSixDigitCode(string? value) =>
        value is { Length: 6 } && value.All(character => character is >= '0' and <= '9');

    private static IPAddress? GetLanAddress() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up
            && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
        .Select(address => address.Address)
        .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
        .OrderByDescending(IsPrivateNetworkAddress)
        .FirstOrDefault();

    private static bool IsPrivateNetworkAddress(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }
}
