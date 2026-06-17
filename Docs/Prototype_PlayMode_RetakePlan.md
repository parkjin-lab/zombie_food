# Prototype Play Mode Retake Plan

Generated: 2026-06-18 00:37 KST
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

No standalone PNG candidates remain for the required retake targets. Capture fresh evidence instead of trying to register old screenshots.

## Focused Retake Targets
| State | Unity menu path | Must see |
| --- | --- | --- |
| Wave Combat | `Tools > Food Truck Prototype > Prepare and Capture State > Wave Combat` | Strengthened action showcase is readable: attack trails, HIT>Z -12, HIT>Z KO, LEAK, BITE>TRK -7, lane flash, and one long truck. |

## Wave Combat Note
Wave Combat has legacy or incomplete action-showcase evidence. If Play Mode is available, retake Wave Combat too and confirm attack trails, `HIT>Z -12`, `HIT>Z KO`, `LEAK`, `BITE>TRK -7`, and lane flash are readable.

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
