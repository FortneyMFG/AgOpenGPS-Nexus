# [Section Number] — [Section Title]
*(Status: [drafting/review/final])*

**Section ID:** [X]
**Version:** 0.1.0
**Editors:** @owner, @reviewer  
**Last Updated:** 2025-10-20  
**Related Sections:** [IDs or links]  
**Upstream Dependencies:** [IDs]  
**Downstream Impacts:** [IDs]

---

> **Template Coverage:** This single template replaces the standalone section, option, and
> decision-matrix stubs. Use §[X.5] for normative requirements, §[X.10]–§[X.12] for option and
> comparison material, and link supporting ADRs from §[X.16].

## [X.1 Purpose & Scope]

State what this section defines and its purpose within the broader system.  
Explain **what capability** or subsystem it governs, **why it matters**, and **what outcomes** it enables.

> **Example:**  
> This section defines requirements and design options for the [Subsystem or Feature Name].  
> It covers runtime behaviors, integration boundaries, and quality expectations for this capability.

---

## [X.2 Context]

Summarize dependencies, assumptions, and boundaries that influence this capability.

> **Example:**  
> - Depends on [framework, runtime, or component].  
> - Interacts with [other subsystem(s)].  
> - Out of scope: [explicitly state exclusions].

---

## [X.3 Legacy Comparison]

Describe how legacy or prior implementations handled this capability.  
Include limitations and modernization opportunities that motivate this new design.

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Architecture | Describe historical component structure. | Note coupling or scalability issues. | Describe improved modular or service-oriented approach. | [Link / Issue / Doc] |
| Performance | Describe observed throughput or resource usage. | Identify bottlenecks. | Define optimization or measurement goals. | [Benchmark / Log] |
| UX / Config | Explain how users previously interacted with this function. | Mention pain points or rigidity. | Define proposed UX or configuration improvements. | [Screenshot / Note] |

> **Informative:** Provides background only; does not impose requirements.

---

## [X.4 Definitions]

List all specialized terms, abbreviations, or acronyms used in this section.

| Term | Definition |
|------|-------------|
| [Term] | [Description of meaning within this context.] |
| [Abbreviation] | [Expanded term.] |
| [Concept] | [Clarification of intent or scope.] |

---

> **Requirement Grammar (RFC-2119):**  
> - **MUST / MUST NOT** = mandatory; test must exist.  
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.  
> - **MAY** = optional; document enabling conditions.  
>
> **Clarity Checklist:** Avoid weak words: *fast, robust, user-friendly, handle, support, adequate,* etc.  
> Prefer measurable forms: *“≤ 250 ms p95,” “error rate < 0.1%,” “99.5% success over 10k trials.”*  
> Each requirement: single behavior, single actor, single condition, single metric.

## [X.5 Requirements]

Define specific, testable, measurable statements of what the system must do.

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|----------|-----------------|-----------------------------|
| R-[X]000 | MUST | Capability | Define essential behavior or output. | C1, #issue | Define how to verify or measure. |
| R-[X]001 | SHOULD | Performance | Quantify timing, accuracy, or reliability goals. | C2 | Specify verification process. |
| R-[X]002 | MAY | Extensibility | Define optional or future roadmap capability. | C3 | Link to ADR / trace entry. |

> **Normative:** Each requirement must be objectively testable and traceable to a verification method.

### [X.5.1] Requirement Sources & Rationale

| Req ID      | Source (issue/discussion/standard) | Rationale (one line) |
|-------------|-------------------------------------|----------------------|
| R-[X]000    | #1234, WG-meeting-2025-10-10        | Required for baseline operability |
| R-[X]001    | Bench doc BR-017                    | Meets latency SLA for operators  |

---

## [X.6 Acceptance Criteria & Verification]

Describe how compliance with the requirements is validated.

> **Examples:**  
> - Automated unit or integration test coverage thresholds.  
> - Simulated scenario replay verification.  
> - Manual review or field test sign-off checklist.

### [X.6.1] Requirement-to-Verification Map

| Req ID     | Verification Type | Artifact / Location                  | Pass/Fail Threshold |
|------------|--------------------|--------------------------------------|---------------------|
| R-[X]000 | CI integration     | `/tests/integration/test_boot.cs`    | Exit code 0; logs clean |
| R-[X]001 | Benchmark          | `/bench/startup_bench.md`            | p95 ≤ 12 s          |
| R-[X]002 | Manual checklist   | `/docs/checklists/operator.md`       | All items ✓         |

---

## [X.7 Constraints]

List explicit boundaries, standards, or dependencies that cannot change.

> **Examples:**  
> - Must use [specific library or runtime].  
> - Must comply with [regulatory or safety constraint].  
> - Hardware minimums or environmental assumptions.

### [X.7.1] Non-Functional Requirement Classes

- **Performance:** latency, throughput, CPU/RAM caps  
- **Reliability & Availability:** MTBF/MTTR, restart behavior  
- **Security:** authn/z, transport, data at rest, SBOM  
- **Safety:** failure modes, mitigations (if applicable)  
- **Usability/UX:** discoverability, error recovery affordances  
- **Operability:** logs, metrics, health endpoints, rotation  
- **Portability:** OS/arch, containerization, config portability  
- **Maintainability:** complexity limits, module boundaries, docs

---

## [X.8 Risks & Open Issues]

Track known uncertainties, gaps, or external factors still under investigation.

| ID | Description | Impact | Mitigation / Status | Owner |
|----|--------------|---------|---------------------|-------|
| RISK-[X]-1 | Describe potential hazard or dependency. | High/Medium/Low | State mitigation plan. | @name |
| ISSUE-[X]-1 | Identify pending decision or dependency. | Medium | Link to ADR or task. | @name |

---

## [X.9 Design Considerations]

Enumerate major factors or guiding themes that shape design options.

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | [Consideration Name] | Describe the influence or constraint. |
| C2 | [Consideration Name] | Describe another factor affecting design direction. |
| C3 | [Consideration Name] | Include assumptions, environment, or interoperability goals. |

### [X.9.1] Assumptions & Preconditions

- [A1] [Example: GPS pose stream ≥ 25 Hz is available]  
- [A2] [Example: Network time is synchronized within ±50 ms]  
- [A3] [Example: Operator has write access to config directory]

---

## [X.10 Option Overview]

List possible technical or architectural approaches that could satisfy the requirements.  
All related files **must begin with the section number** (e.g., `11-O1-Title.md`).

| Option ID | Status | Type / Theme | Description | Reference Document |
|------------|---------|---------------|--------------|--------------------|
| **[X]-O1** | Proposed | [Approach Type] | Short description of option or strategy. | [[X]-O1-Title.md]([X]-O1-Title.md) |
| **[X]-O2** | Favored | [Approach Type] | Option currently considered most suitable. | [[X]-O2-Title.md]([X]-O2-Title.md) |
| **[X]-O3** | In Review | [Approach Type] | Candidate under technical evaluation. | [[X]-O3-Title.md]([X]-O3-Title.md) |
| **[X]-O4** | Approved | [Approach Type] | Final chosen option implemented. | [[X]-O4-Title.md]([X]-O4-Title.md) |
| **[X]-O5** | Deprecated | [Legacy Type] | Older or rejected proposal for record-keeping. | — |

> **Informative:** These are explored alternatives, not binding requirements.  
> **Lifecycle:** Proposed → Favored → In Review → Approved → Deprecated.

---

## [X.11 Comparison Matrix]

Provide a qualitative comparison of trade-offs between available options.

| Attribute / Criteria | [X]-O1 | [X]-O2 | [X]-O3 |
|----------------------|--------|--------|--------|
| Core Approach | [Example summary] | [Example summary] | [Example summary] |
| Implementation Effort | Low | Medium | High |
| Maintainability | Medium | High | Low |
| Performance Potential | High | High | Medium |
| Extensibility | Low | High | Medium |
| Risk Level | Medium | Low | High |

---

## [X.12 Decision Matrix]

This subsection documents the quantitative evaluation used to select among candidate options.

### [X.12.1] Weighting Method

**Purpose:** Explain why each criterion matters and how its relative weight was derived.  
Weights should total **1.0**.

| Criterion | Rationale for Inclusion | Weight |
|------------|------------------------|--------|
| Implementation Complexity | Effort and cost of development and integration. | 0.25 |
| Performance / Quality Impact | Effect on reliability, throughput, accuracy. | 0.25 |
| Maintainability | Long-term sustainability, update cost, readability. | 0.20 |
| Extensibility / Roadmap Fit | Alignment with future features and plugin growth. | 0.20 |
| Ecosystem Alignment | Community familiarity, library maturity, support. | 0.10 |
| **Total** |  | **1.0** |

### [X.12.2] Scoring Scale

Scores use a **1–5 ordinal scale** with descriptive anchors to ensure consistent interpretation.

| Score | Meaning | Qualitative Description |
|-------|----------|-------------------------|
| **1** | Very Poor | Fundamentally unsuited; major blockers. |
| **2** | Poor | Feasible but with unacceptable trade-offs. |
| **3** | Adequate | Meets minimal expectations with caveats. |
| **4** | Good | Performs well and aligns with design goals. |
| **5** | Excellent | Ideal fit; strong performance and maintainability. |

> *Optional:* Use half-steps (e.g., 3.5) when granularity helps, but round totals to two decimals.  
> **Evidence:** Each score must cite a benchmark, prototype, or prior art.

### [X.12.3] Scoring Evidence

Provide a brief justification or data source for each option’s score.

| Criterion | [X]-O1 Justification | [X]-O2 Justification | [X]-O3 Justification |
|------------|---------------------|----------------------|----------------------|
| Implementation Complexity | Prototype required 2 new modules. | Reuses existing interface. | Needs major refactor. |
| Performance / Quality Impact | No known perf gains. | Verified +15% throughput. | Experimental, untested. |
| Maintainability | Adds 3 KLOC. | Simple config-based toggle. | Heavy codegen dependency. |
| Extensibility / Roadmap Fit | Difficult to extend. | Aligns with plugin model. | Tied to legacy API. |
| Ecosystem Alignment | Obscure libs. | Uses supported stack. | External vendor SDK. |

### [X.12.4] Weighted Scoring Table

| Criterion | Weight | [X]-O1 | [X]-O2 | [X]-O3 |
|------------|--------|--------|--------|--------|
| Implementation Complexity | 0.25 | 5 | 3 | 2 |
| Performance / Quality Impact | 0.25 | 3 | 4 | 2 |
| Maintainability | 0.20 | 4 | 4 | 3 |
| Extensibility / Roadmap Fit | 0.20 | 2 | 5 | 3 |
| Ecosystem Alignment | 0.10 | 3 | 5 | 2 |
| **Weighted Total** | **1.0** | **3.4** | **4.3** | **2.6** |

### [X.12.5] Decision Summary

**Selected Option:** [X]-O2 — [Short title]  
**Rationale:** Highest weighted total; aligns with requirements and roadmap.  
**Formal Record:** [[X]-ADR-###_Title.md]([X]-ADR-###_Title.md)

> **Verification:** Decision results and scoring assumptions reviewed by the working group on [date].

---

## [X.13 Evaluation & Verification]

Define how the implemented solution will be measured and validated.

**Benchmark Examples**

- [Metric 1]: [Target threshold or KPI].  
- [Metric 2]: [Latency, throughput, accuracy, etc.].  

**Test Procedure Summary**

1. Define standard test or simulation workflow.  
2. List required tools, datasets, or configurations.  
3. Specify result collection and reporting methods.

**Acceptance Criteria**

- Each requirement (R-[X]###) must be verified through its linked test artifact.  
- Include explicit pass/fail conditions or numerical thresholds.

---

## [X.14 Implementation Policy]

Describe guidelines for integrating or configuring the resulting implementation.

> **Examples:**  
> - Configuration format and storage location.  
> - Interface contracts or discovery mechanisms.  
> - Versioning and backward-compatibility expectations.

---

## [X.15 Community Sentiment]

Capture relevant contributor discussions and alignment of community feedback.

> **Examples:**  
> - Shared priorities identified in contributor meetings.  
> - Agreed constraints or philosophical goals (e.g., “favor plugin modularity”).  
> - Lessons learned from previous iterations.

### [X.15.1] Section Change Log

| Date       | Summary                                | PR / Issue |
|------------|----------------------------------------|------------|
| 2025-10-20 | Initial draft of requirements & options | #0000      |
| - | Approve [X]-O2; add verification plan   | #0000      |

---

## [X.16 Traceability]

Show linkage between requirements, options, decisions, and implementation artifacts.

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-[X]000 | [X]-O2 | [X]-ADR-0XX | `/tests/...` | `/src/...` |
| R-[X]001 | [X]-O1 | [X]-ADR-0XY | `/benchmarks/...` |../Plugins/...` |
| R-[X]002 | — | — | — | — |

---

## [X.17] Conformance

An implementation **conforms** to this section when:
1) All **MUST** requirements are satisfied and verified by mapped artifacts;  
2) All **SHOULD** requirements are either satisfied or explicitly waived with justification;  
3) No **MUST NOT** requirement is violated.

---

## Standards Context

This template aligns with **ISO/IEC/IEEE 29148:2018** (*Systems and Software Requirements Specification*)  
and **IEEE 1016:2017** (*Software Design Description*).

> **Normative content**: measurable, testable requirements (**MUST/SHOULD/MAY**).  
> **Informative content**: context, rationale, and design exploration.  
> **Traceability**: every requirement links to a verification method and code artifact.
