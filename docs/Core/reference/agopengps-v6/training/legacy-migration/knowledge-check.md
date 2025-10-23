# Knowledge check

Circle the best answer for each question. Passing score is 4/5.

1. What artifact becomes the source of truth for Nexus after running
   `legacy-tool translate`?
   - A. The original V6 `Machine.xml`
   - B. The generated `machine-profile.json`
   - C. Screenshots of the V6 steering dialog
   - D. The UDP soak capture
2. Which condition must be met before moving from the bench harness to a live
   machine?
   - A. Pose and steering frames match within the soak report
   - B. The simulator run lasted at least 10 minutes
   - C. The machine profile imports without warnings
   - D. Coverage exports have been shared with the dealer
3. During the lab you notice the soak report shows 30 pose frames and 29 section
   frames. What is the correct next step?
   - A. Ignore it; the difference is acceptable
   - B. Shorten the soak duration and retry
   - C. Inspect wiring or firmware before repeating the soak
   - D. Proceed to import and note it on the checklist
4. When should operators archive the translation outputs?
   - A. Only after the first successful field trial
   - B. Immediately after `legacy-tool translate` completes
   - C. After Nexus imports succeed without warnings
   - D. Once QA reviews the soak report
5. Which resource provides a simulator scenario aligned with the migration
   workflow?
   - A. [docs/Core/reference/agopengps-v6/scenarios/legacy-auto-run](../../scenarios/legacy-auto-run/README.md)
   - B. [docs/Core/howto/windows-packaging.md](../../../../howto/windows-packaging.md)
   - C. [tools/scripts/dealer-deploy.ps1](../../../../../tools/scripts/dealer-deploy.ps1)
   - D. [docs/Core/reference/agopengps-v6/porting/AutoSteerLite-Tuning.md](../../porting/AutoSteerLite-Tuning.md)
