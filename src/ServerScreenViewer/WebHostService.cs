using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ServerScreenViewer;

internal sealed class WebHostService : IAsyncDisposable
{
    private const string SessionCookie = "ssv_session";
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);
    private readonly AppConfig _config;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _sessions = new();
    private readonly ConcurrentDictionary<string, LoginAttemptState> _failedLogins = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _captureLock = new(1, 1);
    private WebApplication? _app;

    public WebHostService(AppConfig config)
    {
        _config = config;
    }

    public string? LastCaptureError { get; private set; }

    public async Task StartAsync()
    {
        if (_app is not null)
            return;

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>(),
            ContentRootPath = AppContext.BaseDirectory,
            ApplicationName = typeof(WebHostService).Assembly.FullName
        });
        builder.Logging.ClearProviders();

        var address = IPAddress.Parse(_config.BindAddress);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Listen(address, _config.Port, listen =>
            {
                if (_config.UsesHttps)
                {
                    var certificatePath = Environment.ExpandEnvironmentVariables(_config.CertificatePath);
                    var certificatePassword = Environment.GetEnvironmentVariable("SSV_CERT_PASSWORD")
                        ?? _config.CertificatePassword;
                    listen.UseHttps(certificatePath, certificatePassword);
                }
            });
        });

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' blob:; style-src 'unsafe-inline'; script-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
            await next();
        });

        app.MapGet("/", (HttpContext context) =>
            Results.Content(IsAuthenticated(context) ? HtmlPages.Viewer(_config.CaptureIntervalMs) : HtmlPages.Login(), "text/html; charset=utf-8"));

        app.MapPost("/login", async (HttpContext context) =>
        {
            if (!context.Request.HasFormContentType)
                return Results.BadRequest("Expected form data.");

            var clientAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (IsLoginBlocked(clientAddress, out var retryAfter))
            {
                context.Response.Headers["Retry-After"] = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                return Results.Content(HtmlPages.Login("Too many incorrect codes. Try again in 15 minutes."), "text/html; charset=utf-8", statusCode: StatusCodes.Status429TooManyRequests);
            }

            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            if (!SecureEquals(form["key"].ToString(), _config.ApiKey))
            {
                RecordFailedLogin(clientAddress);
                await Task.Delay(350, context.RequestAborted);
                return Results.Content(HtmlPages.Login("The access code is not valid."), "text/html; charset=utf-8", statusCode: StatusCodes.Status401Unauthorized);
            }

            _failedLogins.TryRemove(clientAddress, out _);
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var expires = DateTimeOffset.UtcNow.AddMinutes(_config.SessionMinutes);
            _sessions[token] = expires;
            context.Response.Cookies.Append(SessionCookie, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = _config.UsesHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = expires,
                IsEssential = true
            });
            return Results.Redirect("/");
        });

        app.MapPost("/logout", (HttpContext context) =>
        {
            if (context.Request.Cookies.TryGetValue(SessionCookie, out var token))
                _sessions.TryRemove(token, out _);
            context.Response.Cookies.Delete(SessionCookie, new CookieOptions { Path = "/" });
            return Results.NoContent();
        });

        app.MapGet("/api/snapshot", async (HttpContext context) =>
        {
            if (!IsAuthenticated(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _captureLock.WaitAsync(context.RequestAborted);
            try
            {
                var image = await Task.Run(() => ScreenCapture.CaptureJpeg(_config), context.RequestAborted);
                LastCaptureError = null;
                context.Response.ContentType = "image/jpeg";
                context.Response.ContentLength = image.Length;
                await context.Response.Body.WriteAsync(image, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The browser disconnected while a capture was in progress.
            }
            catch (Exception ex)
            {
                LastCaptureError = ex.Message;
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsync("Screen capture is temporarily unavailable.", context.RequestAborted);
                }
            }
            finally
            {
                _captureLock.Release();
            }
        });

        app.MapGet("/healthz", () => Results.Json(new { status = "ok" }));

        await app.StartAsync();
        _app = app;
    }

    private bool IsAuthenticated(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(SessionCookie, out var token))
            return false;
        if (!_sessions.TryGetValue(token, out var expires))
            return false;
        if (expires <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return false;
        }
        return true;
    }

    private bool IsLoginBlocked(string clientAddress, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        if (!_failedLogins.TryGetValue(clientAddress, out var state))
            return false;

        retryAfter = state.LockedUntil - DateTimeOffset.UtcNow;
        if (retryAfter > TimeSpan.Zero)
            return true;

        if (state.LockedUntil != default)
            _failedLogins.TryRemove(clientAddress, out _);
        return false;
    }

    private void RecordFailedLogin(string clientAddress)
    {
        _failedLogins.AddOrUpdate(
            clientAddress,
            _ => new LoginAttemptState(1, default),
            (_, state) => state.FailedAttempts + 1 >= MaxFailedLoginAttempts
                ? new LoginAttemptState(MaxFailedLoginAttempts, DateTimeOffset.UtcNow.Add(LoginLockoutDuration))
                : new LoginAttemptState(state.FailedAttempts + 1, default));
    }

    private sealed record LoginAttemptState(int FailedAttempts, DateTimeOffset LockedUntil);

    private static bool SecureEquals(string supplied, string expected)
    {
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _app.StopAsync(timeout.Token);
            await _app.DisposeAsync();
            _app = null;
        }
        _captureLock.Dispose();
    }
}
