using System;
using System.Globalization;

namespace Aog.UI.Avalonia.ViewModels;

public sealed class LayoutVersionViewModel
{
    public LayoutVersionViewModel(
        string versionLabel,
        string versionHash,
        string author,
        DateTime updatedAtUtc,
        bool isLiveLink,
        string summary)
    {
        VersionLabel = versionLabel ?? throw new ArgumentNullException(nameof(versionLabel));
        VersionHash = versionHash ?? throw new ArgumentNullException(nameof(versionHash));
        UpdatedBy = author ?? throw new ArgumentNullException(nameof(author));
        UpdatedAtUtc = DateTime.SpecifyKind(updatedAtUtc, DateTimeKind.Utc);
        IsLiveLink = isLiveLink;
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }

    public string VersionLabel { get; }

    public string VersionHash { get; }

    public string VersionHashShort => VersionHash.Length > 8 ? VersionHash[..8] : VersionHash;

    public string VersionDisplay => $"{VersionLabel} · {VersionHashShort}";

    public string UpdatedBy { get; }

    public DateTime UpdatedAtUtc { get; }

    public bool IsLiveLink { get; }

    public string Summary { get; }

    public string ModeDisplay => IsLiveLink ? "Live link" : "Snapshot";

    public string UpdatedAtDisplay => UpdatedAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    public string UpdatedByDisplay => $"Updated by {UpdatedBy}";
}
