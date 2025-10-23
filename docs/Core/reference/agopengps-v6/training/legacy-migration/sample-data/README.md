# Sample data preparation

Populate this folder before training with sanitized assets from a completed
migration dry-run. Include the following files:

| File | Notes |
| --- | --- |
| `Machine.xml` | Legacy machine profile with personal data removed. |
| `Fields/` | Directory containing `TrackLines.txt`, `Boundary.txt`, and related geometry exports. |
| `udp-soak.bin` | 30-second UDP capture recorded from the bench harness. |
| `translated/` | Optional reference output from `legacy-tool translate` for instructors. |

Store the canonical copy in secure storage and refresh the assets quarterly so
they reflect current firmware and configuration conventions.
