# Prototype Play Mode Retake Plan

Generated: 2026-06-08 22:59 KST
Retake plan status: `ok`

## Machine Summary
- Suite status: `captured_manual` (4/4)
- Suite evidence source: `manual screenshot registration`
- Screenshot status: `suite_ready` (4/4 covered)
- Missing labeled states: none
- Manual registration candidates: `0`
- Triaged non-state screenshots: `1`
- Review readiness: `ready_for_visual_review`
- Manual record status: `not_recorded`
- Wave Combat action showcase: `False` (legacy_wave_combat_capture_without_attack_trails_and_labels)

## Focused Retake Targets
No Draw/Pending/Invalid focused retakes are currently missing. Continue with review pack judgment or the next recorded blocker.

## Wave Combat Note
Wave Combat has legacy or incomplete action-showcase evidence. If Play Mode is available, retake Wave Combat too and confirm attack trails, `-12`, `KO`, `LEAK`, `TRUCK -7`, and lane flash are readable.

## Follow-Up Commands
Before opening Unity, verify this plan still matches the current evidence state:

```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
```

After retaking the missing screenshots, run these in order:

```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -PreviewOnly -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
```

Generate the full review pack only after `review_readiness` reaches `ready_for_visual_review` or when you intentionally want a partial evidence sheet for discussion.
