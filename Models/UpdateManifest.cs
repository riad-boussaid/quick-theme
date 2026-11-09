namespace QuickTheme.Models;

internal sealed class UpdateManifest
{
    public string Version { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

internal sealed class UpdateCheckResult
{
    public bool IsConfigured { get; init; }
    public bool IsNewer { get; init; }
    public UpdateManifest? Manifest { get; init; }
    public string? Error { get; init; }
}

internal readonly record struct UpdateDownloadProgress(long BytesReceived, long? TotalBytes);
