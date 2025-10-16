using System.Linq;
using Aog.Core.Machines.Axle;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class KinematicsProfileEditorViewModelTests
{
    [Fact]
    public void CreateSample_BuildsValidProfile()
    {
        var editor = KinematicsProfileEditorViewModel.CreateSample();

        var result = editor.BuildProfile();

        result.IngestionResult.IsSuccess.Should().BeTrue();
        result.IngestionResult.Profile.Should().NotBeNull();
        result.IngestionResult.Profile!.Axles.Should().HaveCount(3);
        editor.HasValidationErrors.Should().BeFalse();
        editor.ExportedJson.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BuildProfile_MissingJoints_ProducesValidationError()
    {
        var editor = KinematicsProfileEditorViewModel.CreateSample();
        editor.Joints.Clear();

        var result = editor.BuildProfile();

        result.IngestionResult.IsSuccess.Should().BeFalse();
        result.IngestionResult.Messages.Should().Contain(message => message.Code == "KIN-030");
        editor.HasValidationErrors.Should().BeTrue();
    }

    [Fact]
    public void AddAxleCommand_AppendsAxleAndJoint()
    {
        var editor = new KinematicsProfileEditorViewModel();
        editor.Axles.Add(new KinematicsAxleViewModel("tractor.drive", AxleNodeRole.Drive, 0.35, 0.05));
        editor.Axles.Add(new KinematicsAxleViewModel("tractor.steer", AxleNodeRole.Steer, 0.4, 0.08));
        editor.Joints.Add(new KinematicsJointViewModel("tractor.drive", "tractor.steer", AxleJointType.Rigid));

        editor.AddAxleCommand.Execute(null);

        editor.Axles.Should().HaveCount(3);
        editor.Joints.Should().Contain(j => j.ParentId == "tractor.steer" && j.ChildId == editor.Axles.Last().Id);
    }
}
