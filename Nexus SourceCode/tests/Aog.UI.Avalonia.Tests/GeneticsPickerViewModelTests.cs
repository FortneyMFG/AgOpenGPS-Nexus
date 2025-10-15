using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class GeneticsPickerViewModelTests
{
    [Fact]
    public void CreateSample_SeedsFavoritesRecentsAndSelection()
    {
        var viewModel = GeneticsPickerViewModel.CreateSample();

        viewModel.FavoriteLots.Should().HaveCount(2);
        viewModel.RecentLots.Should().HaveCountGreaterOrEqualTo(2);
        viewModel.SearchResults.Should().NotBeEmpty();

        viewModel.SelectedOption.Should().NotBeNull();
        viewModel.SelectedOption!.OptionId.Should().Be("favorite-p1185q");
        viewModel.StatusMessage.Should().Contain("Active variety");
        viewModel.DetailMessage.Should().Contain("Lot LOT-445");
    }

    [Fact]
    public void SearchQueryFiltersCatalog()
    {
        var viewModel = GeneticsPickerViewModel.CreateSample();

        viewModel.SearchQuery = "lumisure";

        viewModel.SearchResults.Should().ContainSingle();
        viewModel.SearchResults.Single().Product.Should().Be("P1185Q");
        viewModel.SearchSummary.Should().Contain("1 match");
    }

    [Fact]
    public void BarcodeScanSelectsMatchingVariety()
    {
        var viewModel = GeneticsPickerViewModel.CreateSample();

        viewModel.ApplyBarcodeScan("ASG-LOT-BQX-2025");

        viewModel.SelectedOption.Should().NotBeNull();
        viewModel.SelectedOption!.OptionId.Should().Be("recent-xtendflex");
        viewModel.StatusMessage.Should().Contain("Active variety");
        viewModel.BarcodeStatus.Should().Contain("Matched barcode");
        viewModel.HasBarcodeError.Should().BeFalse();
    }

    [Fact]
    public void ManualSelectionUpdatesStatus()
    {
        var viewModel = GeneticsPickerViewModel.CreateSample();
        var target = viewModel.FavoriteLots.Single(option => option.OptionId == "favorite-dkc6435");

        target.SelectCommand.Execute(null);

        target.IsSelected.Should().BeTrue();
        viewModel.StatusMessage.Should().Contain("DKC64-35RIB");
        viewModel.DetailMessage.Should().Contain("Lot LOT-882");
        viewModel.BarcodeStatus.Should().Be("Manual selection applied.");
    }
}
