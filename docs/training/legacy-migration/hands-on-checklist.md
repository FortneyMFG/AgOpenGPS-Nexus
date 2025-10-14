# Hands-on checklist

Bring a USB stick with the provided sample assets or your own legacy snapshots.
Record the operator name and workstation ID at the top of the printed copy.

1. **Stage the workspace**
   - Copy `sample-data/` to `C:\NexusTraining` (Windows) or `~/NexusTraining`
     (Linux/macOS).
   - Verify the folder contains `Machine.xml`, `Fields/`, and `udp-soak.bin`.
2. **Run the translator**
   - Open a terminal and execute:
     ```
     legacy-tool translate --machine ./Machine.xml --fields ./Fields --output ./translated
     ```
   - Confirm `translated/machine-profile.json` and `translated/field-assets/`
     are created with the current timestamp.
   - Review the console output for warnings and resolve any issues before
     proceeding.
3. **Validate IO timing**
   - Replay the provided soak capture:
     ```
     legacy-tool soak --capture ./udp-soak.bin --seconds 30 --report translated/soak-report.json
     ```
   - Open the JSON report and confirm pose, steering, and section frames all
     equal 30 with an effective rate near 90 Hz.
   - Save the report alongside the translated assets.
4. **Import into Nexus**
   - Launch Nexus and choose **File → Import legacy assets…**.
   - Select `translated/field-assets/` and import the sample field.
   - In **Settings → Machines**, import `translated/machine-profile.json`.
5. **Simulate the migration**
   - Load the `legacy-auto-run` scenario and verify the simulator shows motion.
   - Toggle manual/auto steering to feel the response and confirm section
     control overlays align with the imported boundaries.
6. **Capture evidence**
   - Export a screenshot of the imported machine profile summary.
   - Save the simulator run log or soak report to `translated/proof/`.
   - Initial the checklist once artifacts are uploaded to the shared drive.

Trainees must complete all steps before attempting migrations on production
machines.
