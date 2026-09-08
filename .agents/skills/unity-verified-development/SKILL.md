---
name: unity-verified-development
description: Implement and verify this Unity URP project using focused tests, reproducible input, actual screenshots and Windows Player checks. Use for Unity development or regression investigation, not design approval or generic coding.
---

# Unity verified development
Read AGENTS.md, docs/DEVELOPMENT.md and relevant docs/VALIDATION.md scenarios. Inspect Git changes and preserve user edits. Scene values may override script defaults.

Prefer Unity MCP. If native tools are absent, use the documented repository SDK client. Inspect live schemas, instances, project path, Editor state and relevant resources before actions. Bind to the intended project; never choose an arbitrary running Editor.

After edits wait for import/compilation/domain reload and inspect Console. Run the smallest meaningful check of changed behavior. Derive expected results from approved requirements or explicitly labeled existing-behavior regressions, never from a desire to pass.

For input changes cover Input System → Raycast → hold → commit. Direct evaluator calls and debug shortcuts cover only logic. Separate synthetic-input tests from Windows device tests. Restore fake devices, input settings, time scale and test objects even after failure.

Capture final Game View at a known scene/state, with UI and post-processing. Camera-only/Scene View renders are supplementary. Inspect returned images; verify capture source, color space, timing and resolution. Do not brighten game lighting merely to make screenshots legible.

Do not use -nographics to certify URP appearance. Do not open another Editor on this project. Do not discard unsaved user scenes; use isolated generated fixtures for tests.

Save evidence in artifacts/ with reproduction context. Separate compile, logic, synthetic input, visual inspection, Player, performance and human playtest results, including skips/failures/timeouts. Stop broadening tests when relevant checks pass unless new evidence warrants it.

For real UI/Player checks, announce foreground Computer Use, identify the window and refresh after actions. One click or screenshot does not prove continuous first-person control. Keep verification code excluded from ordinary builds; report evidence and limits, not a guarantee of fun or release readiness.
