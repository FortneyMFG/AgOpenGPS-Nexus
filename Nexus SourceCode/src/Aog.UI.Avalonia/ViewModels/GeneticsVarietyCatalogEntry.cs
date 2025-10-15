using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Immutable catalog entry describing a genetics variety surfaced in the picker UI.
/// </summary>
/// <param name="OptionId">Stable identifier used for selections and persistence.</param>
/// <param name="Brand">Seed brand or company name.</param>
/// <param name="Product">Product identifier or hybrid string.</param>
/// <param name="TraitStack">Optional trait stack description.</param>
/// <param name="Lot">Optional lot identifier printed on the bag or invoice.</param>
/// <param name="Treatment">Optional seed treatment description.</param>
/// <param name="Source">Optional source description (plan, barcode, manual).</param>
/// <param name="Barcode">Optional barcode payload for wedge/serial scanners.</param>
/// <param name="Notes">Additional context shown in the picker.</param>
/// <param name="TagLabel">Optional tag label (Favorite, Recent, etc.).</param>
/// <param name="AccentColor">Hex color used for tags and selection accents.</param>
/// <param name="UsageSummary">Optional usage summary (sessions, acres) displayed to operators.</param>
/// <param name="LastUsedAt">Timestamp when the lot was last applied or scanned.</param>
/// <param name="IsFavorite">Flag indicating whether the entry should appear in the favorites list.</param>
public sealed record GeneticsVarietyCatalogEntry(
    string OptionId,
    string Brand,
    string Product,
    string? TraitStack,
    string? Lot,
    string? Treatment,
    string? Source,
    string? Barcode,
    string? Notes,
    string? TagLabel,
    string? AccentColor,
    string? UsageSummary,
    DateTimeOffset? LastUsedAt,
    bool IsFavorite);
