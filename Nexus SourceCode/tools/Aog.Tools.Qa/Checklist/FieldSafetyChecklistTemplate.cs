using System;
using System.Collections.Generic;

namespace Aog.Tools.Qa.Checklist;

public static class FieldSafetyChecklistTemplate
{
    public static FieldSafetyChecklist Create()
    {
        return new FieldSafetyChecklist
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Sections = new List<ChecklistSection>
            {
                new()
                {
                    Name = "Pre-run readiness",
                    Items = new List<ChecklistItem>
                    {
                        CreateItem("pre.ppe", "Operators wearing PPE and aware of emergency stop locations.", "Confirm high-visibility clothing, hearing protection, and accessible stop buttons."),
                        CreateItem("pre.boundary", "Field boundaries and exclusion zones marked and communicated.", "Flag hazards and brief spotters before powering hydraulics."),
                        CreateItem("pre.comms", "Radio/voice comms check completed between cab and ground crew.", "Perform a push-to-talk test and confirm hand signals."),
                    }
                },
                new()
                {
                    Name = "Hardware validation",
                    Items = new List<ChecklistItem>
                    {
                        CreateItem("hw.power", "Machine power, hydraulic pressure, and manual override tested.", "Cycle actuators manually before enabling autonomy."),
                        CreateItem("hw.gnss", "GNSS lock with acceptable HDOP and correction source verified.", "Target HDOP < 1.5 with RTK/SSR corrections active."),
                        CreateItem("hw.autosteer", "Autosteer and section controllers respond to engage/disengage.", "Use bench sequence before enabling live valves."),
                    }
                },
                new()
                {
                    Name = "Software & failsafes",
                    Items = new List<ChecklistItem>
                    {
                        CreateItem("sw.watchdog", "Failsafe watchdog heartbeat observed in Core and AGiO logs.", "Check the safety log exporter if in doubt."),
                        CreateItem("sw.scenario", "Simulation baseline replayed to confirm deterministic outputs.", "Run `nexus sim smoke` or the QA dashboard warm-up."),
                        CreateItem("sw.logging", "Safety log retention/export configuration reviewed.", "Ensure log directory writable and retention matches policy."),
                    }
                },
                new()
                {
                    Name = "Approvals",
                    Items = new List<ChecklistItem>
                    {
                        CreateItem("sign.operator", "Operator acknowledges checklist and assumes control when required.", "Signature or badge ID required."),
                        CreateItem("sign.safety", "Safety officer / QA lead authorises autonomous run.", "Requires review of above sections before enabling autonomy."),
                    }
                }
            }
        };
    }

    private static ChecklistItem CreateItem(string id, string description, string guidance)
    {
        return new ChecklistItem
        {
            Id = id,
            Description = description,
            Guidance = guidance
        };
    }
}
