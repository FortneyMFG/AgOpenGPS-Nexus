using System;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class IdentityRegistryViewModelTests
{
    [Fact]
    public void RegisterOrUpdate_AddsIdentityAndAuditEntry()
    {
        var now = new DateTimeOffset(2025, 2, 2, 10, 0, 0, TimeSpan.Zero);
        var registry = new IdentityRegistryViewModel(() => now);

        registry.RegisterOrUpdate(new IdentityRegistration(
            "rig:tractor-alpha",
            "Tractor Alpha",
            new[] { "autosteer.control" },
            new[] { "core.telemetry" },
            "ethernet",
            now));

        registry.Identities.Should().ContainSingle();
        registry.AuditLog.Should().ContainSingle(entry => entry.Action == IdentityAuditAction.Registered);
    }

    [Fact]
    public void Rename_UpdatesDisplayNameAndRecordsAudit()
    {
        var registry = IdentityRegistryViewModel.CreateSample();
        var actor = "operator";

        var result = registry.Rename("rig:tractor-alpha", "Field Tractor", actor);

        result.Should().BeTrue();
        registry.Identities.First(record => record.Id == "rig:tractor-alpha").DisplayName.Should().Be("Field Tractor");
        registry.AuditLog.Should().Contain(entry => entry.Action == IdentityAuditAction.Renamed && entry.Actor == actor);
    }

    [Fact]
    public void Retire_MarksIdentityAndLogsReason()
    {
        var now = new DateTimeOffset(2025, 3, 3, 12, 0, 0, TimeSpan.Zero);
        var registry = new IdentityRegistryViewModel(() => now);
        registry.RegisterOrUpdate(new IdentityRegistration(
            "implement:planter",
            "Planter",
            new[] { "sections.apply" },
            new[] { "core.telemetry" },
            "canbus",
            now));

        var result = registry.Retire("implement:planter", "operator", "Decommissioned");

        result.Should().BeTrue();
        var record = registry.Identities.Single();
        record.IsRetired.Should().BeTrue();
        record.RetiredAt.Should().Be(now);
        record.RetiredReason.Should().Be("Decommissioned");
        registry.AuditLog.Should().Contain(entry => entry.Action == IdentityAuditAction.Retired && entry.Details.Contains("Decommissioned"));
    }
}
