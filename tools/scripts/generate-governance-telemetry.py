#!/usr/bin/env python3
"""Generate governance telemetry outputs for NX-610.

This script reads the backlog (tasks.md), plugin manifest metadata,

and recorded ADR review minutes to publish machine-readable telemetry

and a human-friendly summary for governance stakeholders.
"""

from __future__ import annotations

import argparse
import json
from collections import Counter
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
import re
from typing import Dict, List, Optional


@dataclass
class TaskEntry:
    ticket: str
    title: str
    notes: str
    stage: Optional[str]
    state: str
    section: str


@dataclass
class MinutesDecision:
    identifier: str
    title: str
    status: str
    notes: str


@dataclass
class MinutesActionItem:
    identifier: str
    owner: str
    description: str
    status: str
    due_date: Optional[str]
    completed_date: Optional[str]


@dataclass
class MinutesEntry:
    meeting_date: str
    meeting_type: str
    summary: str
    attendees: List[str]
    decisions: List[MinutesDecision]
    action_items: List[MinutesActionItem]
    links: List[Dict[str, str]]


def repo_root_from_script() -> Path:
    return Path(__file__).resolve().parents[2]


def parse_program_board(tasks_path: Path, section_filter: str) -> Dict[str, object]:
    section_heading = re.compile(r"^###\s+(.*\S)")
    task_line = re.compile(r"^- \[( |x)\]\s+(NX-\d+)\s+(.+)$")
    stage_pattern = re.compile(r"_\(([^)]+)\)_")

    current_section: Optional[str] = None
    entries: List[TaskEntry] = []

    for raw_line in tasks_path.read_text(encoding="utf-8").splitlines():
        line = raw_line.rstrip()
        if not line:
            continue
        section_match = section_heading.match(line)
        if section_match:
            current_section = section_match.group(1).strip()
            continue
        task_match = task_line.match(line)
        if not task_match or current_section is None:
            continue

        mark, ticket, rest = task_match.groups()
        stage_match = stage_pattern.search(rest)
        stage: Optional[str] = None
        if stage_match:
            stage = stage_match.group(1).strip()
            rest = (rest[: stage_match.start()] + rest[stage_match.end() :]).strip()
        title, notes = _split_title_notes(rest)

        normalized_state = _normalize_state(mark, stage)

        entry = TaskEntry(
            ticket=ticket,
            title=title,
            notes=notes,
            stage=stage,
            state=normalized_state,
            section=current_section,
        )
        entries.append(entry)

    filtered = [entry for entry in entries if entry.section == section_filter]
    counts = Counter(entry.state for entry in filtered)
    summary = {
        "section": section_filter,
        "total": len(filtered),
        "done": counts.get("done", 0),
        "in_progress": counts.get("in_progress", 0),
        "planned": counts.get("planned", 0),
        "blocked": counts.get("blocked", 0),
        "tickets": [
            {
                "ticket": entry.ticket,
                "title": entry.title,
                "notes": entry.notes,
                "stage": entry.stage,
                "state": entry.state,
            }
            for entry in filtered
        ],
    }
    return summary


def _split_title_notes(text: str) -> (str, str):
    dash_split = re.split(r"\s+—\s+", text, maxsplit=1)
    if len(dash_split) == 2:
        return dash_split[0].strip(), dash_split[1].strip()
    hyphen_split = re.split(r"\s+-\s+", text, maxsplit=1)
    if len(hyphen_split) == 2:
        return hyphen_split[0].strip(), hyphen_split[1].strip()
    return text.strip(), ""


def _normalize_state(mark: str, stage: Optional[str]) -> str:
    if mark.lower() == "x":
        return "done"
    if stage:
        lowered = stage.lower()
        if "progress" in lowered or "flight" in lowered:
            return "in_progress"
        if "block" in lowered or "hold" in lowered:
            return "blocked"
    return "planned"


def build_dependency_digest(manifest_root: Path) -> Dict[str, object]:
    plugin_entries = []
    capability_counter: Counter[str] = Counter()
    api_counter: Counter[str] = Counter()

    manifest_paths = sorted(manifest_root.glob("**/*.json"))
    for manifest_path in manifest_paths:
        data = json.loads(manifest_path.read_text(encoding="utf-8"))
        plugin_info = {
            "id": data.get("id"),
            "name": data.get("name"),
            "version": data.get("version"),
            "required_apis": _dict_to_sorted_pairs(data.get("requiredApis", {})),
            "required_transports": sorted(data.get("requiredTransports", [])),
            "supported_capabilities": sorted(data.get("supportedCapabilities", [])),
            "minimum_runtime": data.get("minimumRuntimeVersion"),
        }
        plugin_entries.append(plugin_info)

        for api_name in plugin_info["required_apis"]:
            api_counter[api_name["name"]] += 1
        for capability in plugin_info["supported_capabilities"]:
            capability_counter[capability] += 1

    digest = {
        "plugin_count": len(plugin_entries),
        "api_frequency": _counter_to_ranked_list(api_counter, "api"),
        "capability_frequency": _counter_to_ranked_list(capability_counter, "capability"),
        "plugins": plugin_entries,
    }
    return digest


def _dict_to_sorted_pairs(source: Dict[str, str]) -> List[Dict[str, str]]:
    pairs = [
        {"name": key, "version": str(value)}
        for key, value in sorted(source.items(), key=lambda item: item[0])
    ]
    return pairs


def _counter_to_ranked_list(counter: Counter[str], label_key: str) -> List[Dict[str, object]]:
    ranked = sorted(counter.items(), key=lambda item: (-item[1], item[0]))
    return [{label_key: name, "count": count} for name, count in ranked]


def load_review_minutes(minutes_root: Path) -> Dict[str, object]:
    entries: List[MinutesEntry] = []
    for path in sorted(minutes_root.glob("*.json")):
        raw = json.loads(path.read_text(encoding="utf-8"))
        entry = MinutesEntry(
            meeting_date=raw.get("meeting_date"),
            meeting_type=raw.get("meeting_type", "Governance Review"),
            summary=raw.get("summary", ""),
            attendees=sorted(raw.get("attendees", [])),
            decisions=[
                MinutesDecision(
                    identifier=decision.get("id", ""),
                    title=decision.get("title", ""),
                    status=decision.get("status", ""),
                    notes=decision.get("notes", ""),
                )
                for decision in raw.get("decisions", [])
            ],
            action_items=[
                MinutesActionItem(
                    identifier=item.get("id", ""),
                    owner=item.get("owner", ""),
                    description=item.get("description", ""),
                    status=_normalize_action_status(item.get("status")),
                    due_date=item.get("due_date"),
                    completed_date=item.get("completed_date"),
                )
                for item in raw.get("action_items", [])
            ],
            links=[dict(link) for link in raw.get("links", [])],
        )
        entries.append(entry)

    entries.sort(key=lambda item: item.meeting_date)
    open_actions = sum(1 for item in entries for action in item.action_items if action.status != "done")
    done_actions = sum(1 for item in entries for action in item.action_items if action.status == "done")
    return {
        "meetings": [
            {
                "meeting_date": entry.meeting_date,
                "meeting_type": entry.meeting_type,
                "summary": entry.summary,
                "attendees": entry.attendees,
                "decisions": [
                    {
                        "id": decision.identifier,
                        "title": decision.title,
                        "status": decision.status,
                        "notes": decision.notes,
                    }
                    for decision in entry.decisions
                ],
                "action_items": [
                    {
                        "id": action.identifier,
                        "owner": action.owner,
                        "description": action.description,
                        "status": action.status,
                        "due_date": action.due_date,
                        "completed_date": action.completed_date,
                    }
                    for action in entry.action_items
                ],
                "links": entry.links,
            }
            for entry in entries
        ],
        "stats": {
            "meetings_recorded": len(entries),
            "action_items_open": open_actions,
            "action_items_done": done_actions,
        },
    }


def _normalize_action_status(value: Optional[str]) -> str:
    if not value:
        return "open"
    lowered = value.lower()
    if lowered in {"done", "complete", "completed", "closed"}:
        return "done"
    if "progress" in lowered:
        return "in_progress"
    if "block" in lowered or "risk" in lowered:
        return "blocked"
    return "open"


def build_markdown(report: Dict[str, object]) -> str:
    lines: List[str] = []
    lines.append("# ADR Governance Telemetry Report")
    lines.append("")
    lines.append(f"Generated: {datetime.now(UTC).strftime('%Y-%m-%d %H:%M:%S')} UTC")
    lines.append("")

    program_board = report["program_board"]
    lines.append(f"## Program Board — {program_board['section']}")
    lines.append("")
    lines.append(
        "* Totals: "
        f"{program_board['total']} tracked / "
        f"{program_board['done']} done / "
        f"{program_board['in_progress']} in progress / "
        f"{program_board['planned']} planned / "
        f"{program_board['blocked']} blocked"
    )
    lines.append("")
    if program_board["tickets"]:
        lines.append("| Ticket | Stage | State | Title | Notes |")
        lines.append("| --- | --- | --- | --- | --- |")
        for ticket in program_board["tickets"]:
            lines.append(
                "| {ticket} | {stage} | {state} | {title} | {notes} |".format(
                    ticket=ticket["ticket"],
                    stage=_escape_markdown(ticket.get("stage") or "—"),
                    state=_escape_markdown(ticket.get("state", "")),
                    title=_escape_markdown(ticket.get("title", "")),
                    notes=_escape_markdown(ticket.get("notes", "")) or " ",
                )
            )
        lines.append("")

    dependency_digest = report["dependency_digest"]
    lines.append("## Dependency Digest Highlights")
    lines.append("")
    lines.append(f"* Plugins tracked: {dependency_digest['plugin_count']}")
    if dependency_digest["api_frequency"]:
        top_api = dependency_digest["api_frequency"][0]
        lines.append(
            f"* Most referenced API: {top_api['api']} (used by {top_api['count']} plugin(s))"
        )
    if dependency_digest["capability_frequency"]:
        top_capability = dependency_digest["capability_frequency"][0]
        lines.append(
            f"* Most common capability: {top_capability['capability']} (declared by {top_capability['count']} plugin(s))"
        )
    lines.append("")

    lines.append("### Plugin dependency matrix snapshot")
    lines.append("")
    lines.append("| Plugin | Version | Required APIs | Required transports | Capabilities |")
    lines.append("| --- | --- | --- | --- | --- |")
    for plugin in dependency_digest["plugins"]:
        api_text = ", ".join(
            f"{api['name']} {api['version']}" for api in plugin.get("required_apis", [])
        ) or "—"
        transport_text = ", ".join(plugin.get("required_transports", [])) or "—"
        capability_text = ", ".join(plugin.get("supported_capabilities", [])) or "—"
        lines.append(
            "| {name} | {version} | {apis} | {transports} | {caps} |".format(
                name=_escape_markdown(plugin.get("name", plugin.get("id", ""))),
                version=_escape_markdown(plugin.get("version", "")),
                apis=_escape_markdown(api_text),
                transports=_escape_markdown(transport_text),
                caps=_escape_markdown(capability_text),
            )
        )
    lines.append("")

    minutes = report["review_minutes"]
    lines.append("## Review Minutes")
    lines.append("")
    lines.append(
        f"Recorded meetings: {minutes['stats']['meetings_recorded']} | "
        f"Open action items: {minutes['stats']['action_items_open']} | "
        f"Completed action items: {minutes['stats']['action_items_done']}"
    )
    lines.append("")

    for meeting in minutes["meetings"]:
        lines.append(
            f"### {meeting['meeting_date']} — {meeting['meeting_type']}"
        )
        if meeting.get("summary"):
            lines.append("")
            lines.append(meeting["summary"])
        if meeting.get("attendees"):
            lines.append("")
            lines.append(
                "**Attendees:** "
                + ", ".join(sorted(meeting.get("attendees", [])))
            )
        if meeting.get("decisions"):
            lines.append("")
            lines.append("**Decisions**")
            for decision in meeting["decisions"]:
                lines.append(
                    f"- {decision['id']}: {decision['title']} — {decision['status']} ({decision['notes']})"
                )
        if meeting.get("action_items"):
            lines.append("")
            lines.append("**Action Items**")
            for action in meeting["action_items"]:
                checkbox = "x" if action["status"] == "done" else " "
                descriptor = action["description"]
                owner = action.get("owner")
                due = action.get("due_date")
                suffix_parts = []
                if due:
                    suffix_parts.append(f"due {due}")
                if action.get("completed_date"):
                    suffix_parts.append(f"completed {action['completed_date']}")
                if suffix_parts:
                    descriptor += f" ({'; '.join(suffix_parts)})"
                lines.append(
                    f"- [{checkbox}] {action['id']} — {owner}: {descriptor}"
                )
        if meeting.get("links"):
            lines.append("")
            lines.append("**Links**")
            for link in meeting["links"]:
                label = link.get("label", link.get("url", "Link"))
                url = link.get("url", "")
                lines.append(f"- [{label}]({url})")
        lines.append("")

    return "\n".join(lines).rstrip() + "\n"


def _escape_markdown(value: str) -> str:
    escaped = value.replace("|", "\\|")
    return escaped


def parse_args() -> argparse.Namespace:
    root = repo_root_from_script()
    parser = argparse.ArgumentParser(description="Generate governance telemetry artifacts.")
    parser.add_argument(
        "--output-json",
        default=str(root / "artifacts/governance/governance-telemetry.json"),
        help="Path to the JSON telemetry artifact.",
    )
    parser.add_argument(
        "--output-markdown",
        default=str(root / "artifacts/governance/review-minutes.md"),
        help="Path to the Markdown summary artifact.",
    )
    parser.add_argument(
        "--section",
        default="Section A — Foundations & Contracts",
        help="Backlog section to treat as the governance program board.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    root = repo_root_from_script()

    tasks_path = root / "tasks.md"
    manifest_root = root / "docs/plugins/manifests"
    minutes_root = root / "docs/ADR/reviews"

    program_board = parse_program_board(tasks_path, args.section)
    dependency_digest = build_dependency_digest(manifest_root)
    review_minutes = load_review_minutes(minutes_root)

    report = {
        "task": "NX-610",
        "generated_at": datetime.now(UTC).isoformat(),
        "program_board": program_board,
        "dependency_digest": dependency_digest,
        "review_minutes": review_minutes,
    }

    output_json_path = Path(args.output_json)
    if not output_json_path.is_absolute():
        output_json_path = (root / output_json_path).resolve()
    output_json_path.parent.mkdir(parents=True, exist_ok=True)
    output_json_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    markdown_output = build_markdown(report)
    output_markdown_path = Path(args.output_markdown)
    if not output_markdown_path.is_absolute():
        output_markdown_path = (root / output_markdown_path).resolve()
    output_markdown_path.parent.mkdir(parents=True, exist_ok=True)
    output_markdown_path.write_text(markdown_output, encoding="utf-8")

    print(f"Wrote governance telemetry JSON to {output_json_path}")
    print(f"Wrote governance telemetry summary to {output_markdown_path}")


if __name__ == "__main__":
    main()
