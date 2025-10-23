using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

public sealed class PluginListItemViewModel
{
    public PluginListItemViewModel(string id, string name, string version, string status, string source)
    {
        Id = id;
        Name = name;
        Version = version;
        Status = status;
        Source = source;
    }

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public string Status { get; }

    public string Source { get; }
}

public sealed class PluginManagerViewModel : ObservableObject
{
    private readonly Plugins.PluginRegistry _registry;
    private readonly Plugins.PluginHost _host;
    private readonly ObservableCollection<PluginListItemViewModel> _all = new();
    private readonly ObservableCollection<PluginListItemViewModel> _filtered = new();
    private string _filterText = string.Empty;

    public PluginManagerViewModel(Plugins.PluginRegistry registry, Plugins.PluginHost host)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        Plugins = new ReadOnlyObservableCollection<PluginListItemViewModel>(_filtered);
    }

    public ReadOnlyObservableCollection<PluginListItemViewModel> Plugins { get; }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                ApplyFilter();
            }
        }
    }

    public void Refresh()
    {
        _all.Clear();

        var loaded = new HashSet<string>(_host.GetLoadedPlugins()
            .Select(handle => handle.Descriptor.Id), StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in _registry.GetDescriptors()
                     .OrderBy(d => d.Manifest.Name, StringComparer.OrdinalIgnoreCase))
        {
            var status = loaded.Contains(descriptor.Id) ? "Loaded" : "Installed";
            var source = descriptor.RootPath.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase)
                ? "Bundled"
                : "User";

            _all.Add(new PluginListItemViewModel(
                descriptor.Id,
                descriptor.Manifest.Name,
                descriptor.Manifest.Version,
                status,
                source));
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filtered.Clear();
        var text = FilterText?.Trim() ?? string.Empty;

        IEnumerable<PluginListItemViewModel> query = _all;
        if (text.Length > 0)
        {
            query = query.Where(plugin =>
                plugin.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                plugin.Id.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var plugin in query)
        {
            _filtered.Add(plugin);
        }

        OnPropertyChanged(nameof(Plugins));
    }
}

