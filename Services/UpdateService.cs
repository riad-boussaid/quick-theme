using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using QuickTheme.Models;

namespace QuickTheme.Services;

internal static class UpdateService
{
    private static readonly HttpClient Http = CreateHttpClient();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        string? feed = SettingsService.EffectiveUpdateFeedUrl;
        if (string.IsNullOrWhiteSpace(feed))
        {
            return new UpdateCheckResult { IsConfigured = false };
        }

        try
        {
            string json = await ReadTextAsync(feed, cancellationToken).ConfigureAwait(false);
            UpdateManifest manifest = JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions)
                ?? throw new InvalidDataException("The update manifest is empty.");

            if (!TryParseVersion(manifest.Version, out Version remoteVersion))
            {
                throw new InvalidDataException("The update manifest contains an invalid version.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Url))
            {
                throw new InvalidDataException("The update manifest does not contain a download URL.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Sha256))
            {
                throw new InvalidDataException("The update manifest does not contain a SHA-256 hash.");
            }

            manifest.Url = ResolveLocation(feed, manifest.Url);

            return new UpdateCheckResult
            {
                IsConfigured = true,
                IsNewer = remoteVersion > Normalize(AppInfo.Version),
                Manifest = manifest,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidDataException or UriFormatException or TaskCanceledException or UnauthorizedAccessException)
        {
            return new UpdateCheckResult
            {
                IsConfigured = true,
                Error = ex is TaskCanceledException
                    ? "The update check timed out."
                    : ex.Message,
            };
        }
    }

    public static async Task<string> DownloadAsync(
        UpdateManifest manifest,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        string directory = Path.Combine(Path.GetTempPath(), "QuickTheme");
        Directory.CreateDirectory(directory);

        string destination = Path.Combine(
            directory,
            $"quick-theme-{manifest.Version}-{Guid.NewGuid():N}.exe");

        try
        {
            if (TryGetHttpUri(manifest.Url, out Uri? uri))
            {
                using HttpResponseMessage response = await Http
                    .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;

                await using Stream input = await response.Content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
                await using var output = new FileStream(
                    destination,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await CopyWithProgressAsync(input, output, totalBytes, progress, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                string source = GetLocalPath(manifest.Url);

                await using var input = new FileStream(
                    source,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);
                await using var output = new FileStream(
                    destination,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await CopyWithProgressAsync(input, output, input.Length, progress, cancellationToken)
                    .ConfigureAwait(false);
            }

            string hash = await ComputeSha256Async(destination, cancellationToken).ConfigureAwait(false);
            if (!hash.Equals(manifest.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The downloaded update failed its integrity check.");
            }

            long size = new FileInfo(destination).Length;
            progress?.Report(new UpdateDownloadProgress(size, size));
            return destination;
        }
        catch
        {
            TryDelete(destination);
            throw;
        }
    }

    public static void LaunchUpdaterAndExit(string downloadedPath)
    {
        string? target = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(target) ||
            !Path.GetFileName(target).Equals("quick-theme.exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Self-update requires running quick-theme.exe directly.");
        }

        if (!File.Exists(downloadedPath))
        {
            throw new FileNotFoundException("The downloaded update is missing.", downloadedPath);
        }

        string directory = Path.Combine(Path.GetTempPath(), "QuickTheme");
        Directory.CreateDirectory(directory);
        string scriptPath = Path.Combine(directory, $"apply-update-{Guid.NewGuid():N}.ps1");

        string script = $$"""
$ErrorActionPreference = 'Stop'
$target = '{{EscapeForPowerShell(target)}}'
$source = '{{EscapeForPowerShell(downloadedPath)}}'
$processId = {{Environment.ProcessId}}
try {
    while (Get-Process -Id $processId -ErrorAction SilentlyContinue) {
        Start-Sleep -Milliseconds 400
    }
    Start-Sleep -Milliseconds 300
    Copy-Item -LiteralPath $source -Destination $target -Force
    Start-Process -FilePath $target
    Remove-Item -LiteralPath $source -Force -ErrorAction SilentlyContinue
}
catch {
    Start-Process -FilePath $target
}
finally {
    Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force -ErrorAction SilentlyContinue
}
""";

        File.WriteAllText(scriptPath, script);

        var startInfo = new ProcessStartInfo(
            "powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        Process.Start(startInfo);
    }

    private static async Task CopyWithProgressAsync(
        Stream input,
        Stream output,
        long? totalBytes,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[81920];
        long bytesReceived = 0;
        long lastReported = 0;

        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            bytesReceived += read;

            if (bytesReceived - lastReported >= 262144 || bytesReceived == totalBytes)
            {
                progress?.Report(new UpdateDownloadProgress(bytesReceived, totalBytes));
                lastReported = bytesReceived;
            }
        }

        progress?.Report(new UpdateDownloadProgress(bytesReceived, totalBytes));
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static async Task<string> ReadTextAsync(string location, CancellationToken cancellationToken)
    {
        if (TryGetHttpUri(location, out Uri? uri))
        {
            return await Http.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        string path = GetLocalPath(location);
        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveLocation(string feed, string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absolute))
        {
            return absolute.ToString();
        }

        if (TryGetHttpUri(feed, out Uri? feedUri))
        {
            return new Uri(feedUri, url).ToString();
        }

        string feedPath = GetLocalPath(feed);
        string baseDirectory = Path.GetDirectoryName(feedPath) ?? AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDirectory, url));
    }

    private static bool TryGetHttpUri(string location, [NotNullWhen(true)] out Uri? uri)
    {
        if (Uri.TryCreate(location, UriKind.Absolute, out Uri? parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            uri = parsed;
            return true;
        }

        uri = null;
        return false;
    }

    private static string GetLocalPath(string location)
    {
        if (Uri.TryCreate(location, UriKind.Absolute, out Uri? uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        return Path.GetFullPath(location);
    }

    private static bool TryParseVersion(string text, out Version version)
    {
        if (!Version.TryParse(text, out Version? parsed))
        {
            version = new Version(0, 0, 0, 0);
            return false;
        }

        version = Normalize(parsed);
        return true;
    }

    internal static bool IsNewerVersion(string candidate, Version currentVersion) =>
        TryParseVersion(candidate, out Version parsed) && parsed > Normalize(currentVersion);

    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));

    private static string EscapeForPowerShell(string value) => value.Replace("'", "''");

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"QuickTheme/{AppInfo.DisplayVersion}");
        return client;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}
