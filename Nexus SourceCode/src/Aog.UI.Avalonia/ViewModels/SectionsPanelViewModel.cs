using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.V1;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model representing the sections control plugin state.
/// </summary>
public sealed class SectionsPanelViewModel : ObservableObject
{
    private const int MaxSectionCount = 8;

    private readonly SectionToggleViewModel[] _sections;
    private int _sectionCount = MaxSectionCount;
    private bool _isAutoEnabled = true;
    private uint _automaticMask;
    private uint _currentMask;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionsPanelViewModel"/> class.
    /// </summary>
    public SectionsPanelViewModel()
    {
        _sections = Enumerable
            .Range(0, MaxSectionCount)
            .Select(index => new SectionToggleViewModel(index, OnSectionToggled))
            .ToArray();
        SetMask(0);
    }

    /// <summary>
    /// Gets the available section toggle view-models.
    /// </summary>
    public IReadOnlyList<SectionToggleViewModel> Sections => _sections;

    /// <summary>
    /// Gets or sets a value indicating whether automatic section control is enabled.
    /// </summary>
    public bool IsAutoEnabled
    {
        get => _isAutoEnabled;
        set
        {
            if (!SetProperty(ref _isAutoEnabled, value))
            {
                return;
            }

            if (_isAutoEnabled)
            {
                SetMask(_automaticMask);
            }
        }
    }

    /// <summary>
    /// Gets or sets the number of active sections reported by the plugin.
    /// </summary>
    public int SectionCount
    {
        get => _sectionCount;
        private set
        {
            var clamped = Math.Clamp(value, 0, MaxSectionCount);
            if (!SetProperty(ref _sectionCount, clamped))
            {
                return;
            }

            for (var index = 0; index < _sections.Length; index++)
            {
                _sections[index].IsVisible = index < clamped;
            }

            OnPropertyChanged(nameof(MaskDisplay));
        }
    }

    /// <summary>
    /// Gets the currently displayed mask in packed bit form.
    /// </summary>
    public uint CurrentMask
    {
        get => _currentMask;
        private set
        {
            if (!SetProperty(ref _currentMask, value))
            {
                return;
            }

            OnPropertyChanged(nameof(MaskDisplay));
        }
    }

    /// <summary>
    /// Gets a formatted string describing the active mask.
    /// </summary>
    public string MaskDisplay
    {
        get
        {
            if (SectionCount == 0)
            {
                return "Mask: —";
            }

            var formatted = Convert.ToString(CurrentMask, 2).PadLeft(SectionCount, '0');
            return $"Mask: 0b{formatted}";
        }
    }

    /// <summary>
    /// Applies the latest section mask produced by the plugin.
    /// </summary>
    /// <param name="mask">Mask snapshot.</param>
    public void ApplySectionMask(SectionMask mask)
    {
        ArgumentNullException.ThrowIfNull(mask);

        SectionCount = (int)mask.SectionCount;
        _automaticMask = SanitizeMask(mask.Mask, SectionCount);

        if (IsAutoEnabled)
        {
            SetMask(_automaticMask);
        }
    }

    private void OnSectionToggled(int index, bool isEnabled)
    {
        if (index >= SectionCount)
        {
            return;
        }

        if (IsAutoEnabled)
        {
            IsAutoEnabled = false;
        }

        var bit = 1u << index;
        var updated = isEnabled
            ? CurrentMask | bit
            : CurrentMask & ~bit;

        SetMask(updated);
    }

    private void SetMask(uint mask)
    {
        var sanitized = SanitizeMask(mask, SectionCount);
        for (var index = 0; index < _sections.Length; index++)
        {
            if (index >= SectionCount)
            {
                _sections[index].SetIsEnabledFromParent(false);
                continue;
            }

            var isEnabled = (sanitized & (1u << index)) != 0;
            _sections[index].SetIsEnabledFromParent(isEnabled);
        }

        CurrentMask = sanitized;
    }

    private static uint SanitizeMask(uint mask, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        if (count >= 32)
        {
            return mask;
        }

        var allowedBits = (1u << count) - 1u;
        return mask & allowedBits;
    }
}
