# Prototype Autonomous Development Directive

Last updated: 2026-06-10 KST

## Purpose
This document tells Codex, sub-agents, and heartbeat automations how to keep the
prototype moving when the designer is unavailable.

The operating rule is:

`Evidence first, weakest beat next`

That means every unattended pass should first protect Play Mode evidence and
manual result integrity. Only then should it improve a narrow, named rhythm beat:
`Read`, `Commit`, `Pressure`, `Payoff`, or `Release`.

## Current Autonomous Baseline
- Suite evidence is machine-covered as `captured_manual` with all four required
  states present.
- Screenshot evidence is `suite_ready` with 7/7 machine-quality PNGs.
- Official manual result is still open; do not claim final PASS automatically.
- Wave Combat action showcase is still legacy/incomplete until evidence shows
  attack trails plus action labels.
- Large untracked Unity Asset Store/plugin/import folders are present and must
  stay unstaged unless explicitly selected by the user.

## Heartbeat Priority Order
1. Run the smallest status verifier set:
   - `Tools\Verify-PrototypePlayModeSuite.ps1 -JsonOnly`
   - `Tools\Verify-PrototypePlayModeScreenshots.ps1 -JsonOnly`
   - `Tools\Verify-PrototypePlayModeRecord.ps1 -JsonOnly`
2. If review evidence is stale or missing, regenerate review artifacts:
   - `Tools\Write-PrototypePlayModeReviewPack.ps1 -PreviewOnly -JsonOnly`
   - full review pack only when a tracked doc update is useful.
   - If suite/screenshot evidence is already complete and
     `review_readiness=ready_for_visual_review`, do not generate new retake work;
     prepare visual judgment/result-recording guidance instead.
3. If `wave_combat_action_showcase_ready=false`, do not record Wave Combat as
   PASS. Prefer retake guidance or `FIX_FEEDBACK`.
4. If official manual result is `not_recorded`, keep the next action focused on
   PASS/FIX/BLOCKED review, not broad gameplay expansion.
5. If evidence is blocked by environment limits, continue only safe local work:
   docs, guards, review tooling, stale-state detection, and narrow source guards.
6. If evidence is PASS or a clear FIX is recorded, pick one weakest rhythm beat
   and make the smallest implementation that improves it.

## Safe Without User Intervention
- Read code and docs.
- Run non-destructive verifiers and preview-only commands.
- Update handoff, playbook, roadmap, policy, and review docs.
- Add or tighten source-level contract checks.
- Improve evidence tooling, review pack wording, stale checks, and result guards.
- Make a narrow code change when it improves one named rhythm beat and has tests
  or source guards.
- Add review-pack decision aids when they reduce the next human judgment to a
  named beat (`Read`, `Commit`, `Pressure`, `Payoff`, or `Release`).

## Stop And Ask Or Leave Blocked
- Officially recording PASS/FIX/BLOCKED without fresh review evidence.
- Treating ambiguous screenshots as matching a required state.
- Staging large untracked Asset Store/plugin/import folders.
- Deleting imported assets.
- Adding broad mechanics or content while evidence is still partial or blocked.
- Running `Tools\Write-PrototypePlayModeResultFromSuite.ps1` as a casual status
  command; it is for intentional result drafting/applying.

## Autonomous Fun Work Queue
Use this order when the evidence state allows code work:

1. Payoff beat: make `Next` hints explain cause and next action.
   - Example: `Leak x2 -> Stabilize lanes`.
   - Status: complete as of 2026-06-10.
   - Acceptance: EditMode tests cover leak, Heat spike, damage payoff.
2. Commit-to-Pressure beat: make first successful placement feel like wave start.
   - Example: a short `Wave live` cue after the first placed block.
   - Status: source pass complete as of 2026-06-10.
   - Acceptance: HUD contract proves the cue path exists.
3. Pressure beat: tune wave ramp only through bounded, visible values.
   - Status: cadence overlap telemetry source pass complete as of 2026-06-11.
   - Acceptance: telemetry exposes Build/Climb/Peak and multiplier.
4. Release beat: tune rest reward visibility before adding reward choices.
   - Acceptance: review confirms the reward chip reads without crowding.
5. Read beat: simplify draw card intent if review says the card row is too dense.
   - Status: candidate deferred as of 2026-06-14 until review pack result is
     recorded, to avoid stale Draw Choice evidence.
   - Acceptance: card comparison remains readable in portrait screenshots.
6. Review beat naming: keep the review pack able to map visual failures to a
   rhythm beat before asking for new systems.
   - Status: source pass complete as of 2026-06-10.
   - Acceptance: HUD contract guards the review-pack rhythm checklist.

## Files To Update
- `Docs\Prototype_PlayMode_Verification.md`: official manual result and criteria.
- `Docs\Prototype_PlayMode_ReviewPack.md`: visual review contact sheet and result
  command guidance.
- `Docs\Prototype_PlayMode_RetakePlan.md`: focused retake instructions.
- `Docs\Prototype_PlayMode_Verification_Suite.txt`: suite/manual evidence manifest.
- `Docs\Prototype_Session_Handoff.md`: latest state and next autonomous handoff.
- `Docs\Prototype_NextStep_Playbook.md`: policy changes or sprint priority shifts.
- `Docs\Prototype_Update_Roadmap.md`: roadmap status and completed fun passes.
- `Docs\Prototype_RhythmDesign_Audit.md`: rhythm beat policy and acceptance.

## Minimum Verification Sets
Status-only pass:

```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
```

Code/HUD pass:

```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeHudStateContract.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeLayout.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Gate-Verification.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -RunTests -JsonOnly
```

Review pass:

```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -PreviewOnly -JsonOnly
```
