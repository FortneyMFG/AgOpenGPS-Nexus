using System;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class LayoutDiffViewModelTests
{
    [Fact]
    public void CreateSample_ExposesHighImpactChange()
    {
        var viewModel = LayoutDiffViewModel.CreateSample();

        viewModel.HasChanges.Should().BeTrue();
        viewModel.HasBreakingChanges.Should().BeTrue();
        viewModel.BreakingChangeCount.Should().Be(1);
        viewModel.Changes.Should().ContainSingle(change => change.Impact == LayoutDiffImpact.High);
        viewModel.StatusMessage.Should().NotBeNull();
    }

    [Fact]
    public void QueueRollback_DisablesApplyUntilComplete()
    {
        var baseline = new LayoutVersionViewModel(
            versionLabel: "Baseline",
            versionHash: "abc12345",
            author: "Tester",
            updatedAtUtc: DateTime.SpecifyKind(new DateTime(2024, 5, 1, 12, 0, 0), DateTimeKind.Utc),
            isLiveLink: true,
            summary: "Baseline summary.");
        var candidate = new LayoutVersionViewModel(
            versionLabel: "Candidate",
            versionHash: "def67890",
            author: "Tester",
            updatedAtUtc: DateTime.SpecifyKind(new DateTime(2024, 5, 2, 12, 0, 0), DateTimeKind.Utc),
            isLiveLink: true,
            summary: "Candidate summary.");
        var changes = new[]
        {
            new LayoutDiffEntryViewModel(
                elementName: "Widget",
                changeKind: LayoutDiffChangeKind.Modified,
                impact: LayoutDiffImpact.Medium,
                description: "Tweaked widget placement.")
        };

        var viewModel = new LayoutDiffViewModel("Test layout", baseline, candidate, changes);

        viewModel.ApplyCandidateCommand.CanExecute(null).Should().BeTrue();
        viewModel.RollbackCommand.CanExecute(null).Should().BeTrue();

        viewModel.RollbackCommand.Execute(null);

        viewModel.IsRollbackPending.Should().BeTrue();
        viewModel.ApplyCandidateCommand.CanExecute(null).Should().BeFalse();
        viewModel.StatusMessage.Should().Contain("Rollback");

        viewModel.CompleteRollback();

        viewModel.IsRollbackPending.Should().BeFalse();
        viewModel.ApplyCandidateCommand.CanExecute(null).Should().BeTrue();
        viewModel.StatusMessage.Should().Contain("complete");
    }
}
