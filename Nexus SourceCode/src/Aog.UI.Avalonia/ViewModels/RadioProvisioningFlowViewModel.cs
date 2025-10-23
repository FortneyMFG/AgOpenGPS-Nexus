using System;
using System.Collections.Generic;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Surfaces the RadioBridge provisioning workflow in the shell UI.
/// </summary>
public sealed class RadioProvisioningFlowViewModel
{
    public RadioProvisioningFlowViewModel(
        string summary,
        string securityNote,
        string documentationSummary,
        string profileSchemaSummary,
        IReadOnlyList<RadioProvisioningStageViewModel> stages,
        IReadOnlyList<RadioProvisioningProfileFieldViewModel> profileFields)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        SecurityNote = securityNote ?? throw new ArgumentNullException(nameof(securityNote));
        DocumentationSummary = documentationSummary ?? throw new ArgumentNullException(nameof(documentationSummary));
        ProfileSchemaSummary = profileSchemaSummary ?? throw new ArgumentNullException(nameof(profileSchemaSummary));
        Stages = stages ?? throw new ArgumentNullException(nameof(stages));
        ProfileFields = profileFields ?? throw new ArgumentNullException(nameof(profileFields));
    }

    /// <summary>Gets a short description of the provisioning workflow.</summary>
    public string Summary { get; }

    /// <summary>Gets the security reminder surfaced with the workflow.</summary>
    public string SecurityNote { get; }

    /// <summary>Gets a documentation pointer for deeper guidance.</summary>
    public string DocumentationSummary { get; }

    /// <summary>Gets a description of the provisioning profile schema.</summary>
    public string ProfileSchemaSummary { get; }

    /// <summary>Gets the staged workflow sections rendered in the UI.</summary>
    public IReadOnlyList<RadioProvisioningStageViewModel> Stages { get; }

    /// <summary>Gets the provisioning profile field descriptions.</summary>
    public IReadOnlyList<RadioProvisioningProfileFieldViewModel> ProfileFields { get; }

    /// <summary>Creates the sample provisioning workflow surfaced in the shell.</summary>
    public static RadioProvisioningFlowViewModel CreateSample()
    {
        var stages = new List<RadioProvisioningStageViewModel>
        {
            new(
                "Prepare the workstation",
                "Confirm the provisioning kit prerequisites are satisfied before minting device profiles.",
                new List<RadioProvisioningGuideStepViewModel>
                {
                    new("Install the .NET 8.0 SDK on the provisioning workstation."),
                    new(
                        "Clone or download the Nexus source tree so the RadioBridge CLI is available.",
                        "The provisioning utility lives in Nexus SourceCode/tools/Aog.Tools.RadioBridge."),
                    new(
                        "Collect mesh credentials for the farm or lab environment where the bridge will be staged.")
                }),
            new(
                "Generate a provisioning profile",
                "Use the RadioBridge tooling to mint the per-device JSON profile that stores identifiers, capabilities, and keys.",
                new List<RadioProvisioningGuideStepViewModel>
                {
                    new(
                        "Run the provisioning command for the device you are onboarding.",
                        "Override --output to write directly to a secure share or omit it to stream the JSON to stdout.",
                        """
                        dotnet run -- provision \
                            --device-id bridge.lora.alpha \
                            --label "LoRa Bridge Alpha" \
                            --radio-kind lora \
                            --capability radio \
                            --capability bridge \
                            --capability lora \
                            --key-bytes 16 \
                            --output /secure-share/radio/bridge.lora.alpha.json
                        """.Trim()),
                    new(
                        "Deterministic keys are supported for lab fixtures via --key 0123456789ABCDEF when needed."),
                    new(
                        "Store the generated JSON in a secure vault or configuration repository and never commit real keys to git.")
                }),
            new(
                "Configure the AGiO host",
                "Copy the profile onto the device running Aog.Agio and wire it into the RadioBridge adapter options.",
                new List<RadioProvisioningGuideStepViewModel>
                {
                    new(
                        "Place the provisioning profile on the host, for example /opt/nexus/radio/bridge.lora.alpha.json, with restricted permissions."),
                    new(
                        "Update appsettings.json (or environment variables) so the RadioBridge adapter loads the profile and desired transport settings.",
                        null,
                        "\"RadioBridge\": {\n  \"Lora\": {\n    \"Enabled\": true,\n    \"DeviceId\": \"bridge.lora.alpha\",\n    \"DeviceLabel\": \"LoRa Radio Bridge\",\n    \"Endpoint\": \"lora://ttyACM0?baud=57600\",\n    \"EnableForwardErrorCorrection\": true,\n    \"DiagnosticsSeasonId\": \"system\",\n    \"DiagnosticsJobId\": \"radio-lora\"\n  }\n}"),
                    new("Restart the Aog.Agio service so the adapter picks up the new provisioning profile and transport configuration.")
                },
                callout: "ELRS adapters use the same structure under RadioBridge:Elrs and typically leave forward error correction disabled."),
            new(
                "Validate the deployment",
                "Run through the validation checklist to confirm the bridge negotiates correctly with the mesh.",
                new List<RadioProvisioningGuideStepViewModel>
                {
                    new RadioProvisioningGuideStepViewModel(
                        primaryText: "Execute the RadioBridge transport tests to verify retry logic and Hamming decoding remain healthy.",
                        detail: "Run the automated RadioBridge transport tests to validate retry handling and Hamming decoding before promoting the bridge to production.",
                        status: RadioProvisioningStepStatus.Pending,
                        updatedAt: null,
                        command: "dotnet test tests/Aog.Core.Tests --filter RadioBridgeTransportTests"),
                    new RadioProvisioningGuideStepViewModel(
                        primaryText: "Inspect mesh diagnostics for the device and confirm radio.kind, radio.fec, RSSI, and retry counters are reported.",
                        detail: "Review the mesh diagnostics dashboard to ensure the bridge is emitting radio metadata and reliability counters in real time.",
                        status: RadioProvisioningStepStatus.Pending,
                        updatedAt: null),
                    new RadioProvisioningGuideStepViewModel(
                        primaryText: "Replay a sample mesh publication or coverage topic and verify frames reach the radio modem or simulator.",
                        detail: "Replay a known-good publication through the mesh to confirm frames traverse the bridge and reach the modem or simulator endpoints.",
                        status: RadioProvisioningStepStatus.Pending,
                        updatedAt: null)
                })
        };

        var profileFields = new List<RadioProvisioningProfileFieldViewModel>
        {
            new("deviceId", "Mesh device identifier registered with the Live Telemetry Mesh."),
            new("label", "Friendly label shown in diagnostics payloads."),
            new("radioKind", "Specifies \"elrs\" or \"lora\" so adapters select defaults and diagnostics metadata."),
            new("capabilities", "Capability strings advertised during registration (for example radio, bridge, lora)."),
            new("preSharedKey", "Hex-encoded pre-shared key used for AES-CCM transport encryption."),
            new("topics", "Optional allow-list describing mesh topics the bridge can publish or subscribe to."),
        };

        return new RadioProvisioningFlowViewModel(
            summary: "Provision RadioBridge devices with repeatable tooling that aligns with ADR-048.",
            securityNote: "Keep provisioning JSON files in a secured location; treat keys as secrets and rotate them via the same workflow.",
            documentationSummary: "Full guide: docs/development/howto/radio/radiobridge-provisioning.md",
            profileSchemaSummary: "Provisioning profiles follow the RadioBridgeProvisioningProfile contract shipped with the tooling.",
            stages: stages,
            profileFields: profileFields);
    }
}

/// <summary>
/// Represents a provisioning stage that the operator follows.
/// </summary>
public sealed class RadioProvisioningStageViewModel
{
    public RadioProvisioningStageViewModel(
        string title,
        string description,
        IReadOnlyList<RadioProvisioningGuideStepViewModel> steps,
        string? callout = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        Callout = callout;
    }

    /// <summary>Gets the stage title.</summary>
    public string Title { get; }

    /// <summary>Gets the stage description.</summary>
    public string Description { get; }

    /// <summary>Gets an optional callout rendered alongside the stage.</summary>
    public string? Callout { get; }

    /// <summary>Gets the step collection belonging to the stage.</summary>
    public IReadOnlyList<RadioProvisioningGuideStepViewModel> Steps { get; }

    /// <summary>Gets a value indicating whether the stage exposes a callout.</summary>
    public bool HasCallout => !string.IsNullOrWhiteSpace(Callout);
}

/// <summary>
/// Represents a single provisioning step surfaced in the UI.
/// </summary>
public sealed class RadioProvisioningGuideStepViewModel
{
    public RadioProvisioningGuideStepViewModel(string primaryText, string? secondaryText = null, string? command = null)
    {
        PrimaryText = primaryText ?? throw new ArgumentNullException(nameof(primaryText));
        SecondaryText = secondaryText;
        Command = command;
        Status = RadioProvisioningStepStatus.Pending;
        UpdatedAt = null;
    }

    public RadioProvisioningGuideStepViewModel(
        string primaryText,
        string detail,
        RadioProvisioningStepStatus status,
        DateTimeOffset? updatedAt,
        string? command = null)
        : this(primaryText, detail, command)
    {
        Status = status;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets the primary description for the step.</summary>
    public string PrimaryText { get; }

    /// <summary>Gets an optional secondary note for the step.</summary>
    public string? SecondaryText { get; }

    /// <summary>Gets an optional command or configuration snippet.</summary>
    public string? Command { get; }

    /// <summary>Gets the provisioning status associated with the guide step.</summary>
    public RadioProvisioningStepStatus Status { get; private set; }

    /// <summary>Gets the last time the guide step status was updated, if available.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Gets a value indicating whether a secondary note should be rendered.</summary>
    public bool HasSecondaryText => !string.IsNullOrWhiteSpace(SecondaryText);

    /// <summary>Gets a value indicating whether a command snippet should be rendered.</summary>
    public bool HasCommand => !string.IsNullOrWhiteSpace(Command);
}

/// <summary>
/// Describes a provisioning profile field surfaced in the UI.
/// </summary>
public sealed class RadioProvisioningProfileFieldViewModel
{
    public RadioProvisioningProfileFieldViewModel(string field, string description)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>Gets the field name.</summary>
    public string Field { get; }

    /// <summary>Gets the field description.</summary>
    public string Description { get; }
}
