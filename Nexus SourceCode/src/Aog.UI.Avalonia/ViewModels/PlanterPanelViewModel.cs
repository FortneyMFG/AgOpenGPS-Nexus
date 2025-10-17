using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aog.Core.V1;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Aggregates planter row telemetry for presentation in the UI.
/// </summary>
public sealed class PlanterPanelViewModel : ObservableObject
{
    private readonly ObservableCollection<PlanterRowViewModel> _rows = new();
    private readonly Dictionary<int, PlanterRowViewModel> _rowsByIndex = new();
    private string _summary = "No planter rows reported.";
    private int _ignoredRowCount;

    /// <summary>
    /// Gets the collection of row view-models displayed in the UI.
    /// </summary>
    public IReadOnlyList<PlanterRowViewModel> Rows => _rows;

    /// <summary>
    /// Gets a human readable summary of the planter state.
    /// </summary>
    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    /// <summary>
    /// Applies a batch of <see cref="PlanterRowStatus"/> messages to the panel.
    /// </summary>
    /// <param name="statuses">Row status messages.</param>
    public void ApplyRowStatuses(IEnumerable<PlanterRowStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        var ignored = 0;

        foreach (var status in statuses)
        {
            if (status.RowIndex > int.MaxValue)
            {
                ignored++;
                continue;
            }

            var index = (int)status.RowIndex;
            if (!_rowsByIndex.TryGetValue(index, out var row))
            {
                row = new PlanterRowViewModel(index);
                InsertRow(row);
            }

            row.ApplyStatus(status);
        }

        _ignoredRowCount = ignored;
        UpdateSummary();
    }

    private void InsertRow(PlanterRowViewModel row)
    {
        _rowsByIndex[row.RowIndex] = row;

        var insertIndex = 0;
        while (insertIndex < _rows.Count && _rows[insertIndex].RowIndex < row.RowIndex)
        {
            insertIndex++;
        }

        _rows.Insert(insertIndex, row);
    }

    private void UpdateSummary()
    {
        if (_rows.Count == 0)
        {
            Summary = _ignoredRowCount > 0
                ? $"No planter rows reported. Ignored {_ignoredRowCount} invalid update{(_ignoredRowCount == 1 ? string.Empty : "s")}."
                : "No planter rows reported.";
            return;
        }

        var ok = 0;
        var skips = 0;
        var doubles = 0;
        var unknown = 0;

        foreach (var row in _rows)
        {
            switch (row.Quality)
            {
                case PlanterRowQuality.Ok:
                    ok++;
                    break;
                case PlanterRowQuality.Skip:
                    skips++;
                    break;
                case PlanterRowQuality.Double:
                    doubles++;
                    break;
                default:
                    unknown++;
                    break;
            }
        }

        var summary = $"Rows: {_rows.Count} • OK {ok} • Skips {skips} • Doubles {doubles} • Unknown {unknown}";

        if (_ignoredRowCount > 0)
        {
            summary += $" • Ignored {_ignoredRowCount} invalid update{(_ignoredRowCount == 1 ? string.Empty : "s")}";
        }

        Summary = summary;
    }
}
