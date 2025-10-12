# Variable Rate Application SRS Repository

This repository now serves as the home for the Software Requirements Specification (SRS)
for the Variable Rate Application (VRA) control system. The historical AgOpenGPS
artifacts that were previously maintained here are preserved for reference, but the
primary focus has shifted to the definition, validation, and traceability of the VRA
system requirements.

## Purpose of the SRS

The SRS captures the complete set of functional, non-functional, and interface
requirements for the VRA solution. It is the authoritative source for describing what
the system must do, the constraints it operates under, and the success criteria for the
project. Maintaining the SRS in a version-controlled repository allows the whole team to
collaborate on requirements, review changes, and link specifications directly to
implementation artifacts.

## Project Overview

The VRA system is intended to deliver precision application of inputs (seed, fertilizer,
chemicals, etc.) based on spatial data and agronomic prescriptions. Key capabilities
include:

- Integrating prescription maps and live field telemetry to determine optimal rates.
- Communicating commands to rate controllers and section hardware.
- Logging operational data for agronomic analysis and regulatory compliance.
- Providing operators with feedback on application quality and system status.

These requirements evolve through collaboration between agronomists, operators,
hardware engineers, and software developers. The SRS records agreed upon expectations so
implementations can be verified against a stable baseline.

## Goals and Non-Goals

**Goals**

- Document a complete, testable set of requirements for the VRA control system.
- Capture assumptions, dependencies, and constraints that inform design decisions.
- Support traceability between requirements, design artifacts, tests, and releases.
- Enable iterative refinement of requirements as stakeholder needs change.

**Non-Goals**

- Replacing the AgOpenGPS software contained in this repository. Existing binaries and
  source code remain for historical reference only.
- Providing detailed design or implementation documentation; those are tracked in other
  artifacts.
- Serving as an operator manual. End-user documentation will be authored separately.

## Repository Structure

- `docs/` – Primary location for the SRS and supporting requirement documentation.
- `SourceCode/` – Legacy AgOpenGPS solution files, kept to aid requirement discovery and
  context. These files are not actively maintained.
- `README.md` (this document) – Overview of the repository purpose, goals, and usage.

Future documentation updates should focus on the SRS located in the `docs/` directory.
Any modifications to the legacy code should be made only if they support requirement
analysis or archival needs.

## Working With the SRS

1. Clone the repository and create a feature branch for your requirement updates.
2. Edit the relevant Markdown or diagram sources within the `docs/` folder.
3. Submit a pull request describing the changes and how they affect the requirements
   baseline.
4. Request reviews from stakeholders (product, agronomy, QA, engineering) as
   appropriate.

Version control history provides a record of requirement evolution. Use semantic commit
messages and reference issue trackers or change requests to maintain traceability.

## Legacy AgOpenGPS Resources

The AgOpenGPS materials remain available for teams who need to reference prior
implementations, hardware interfaces, or mapping workflows. These resources are not
updated but can inform requirement discussions, especially when adapting proven
functionality to the VRA context.

- Documentation: https://docs.agopengps.com/
- Community forum: https://discourse.agopengps.com/
- PCB and firmware repository: https://github.com/agopengps-official/Boards

## License

All content in this repository, including the SRS, is distributed under the terms of the
GNU General Public License v3.0 (GPLv3). See the `LICENSE` file for the full text.
