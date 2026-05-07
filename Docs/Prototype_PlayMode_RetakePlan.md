# Prototype Play Mode Retake Plan

Generated: 2026-05-08 01:44 KST
Retake plan status: `ok`

## Machine Summary
- Suite status: `manual_partial` (1/4)
- Suite evidence source: `manual screenshot registration`
- Screenshot status: `partial` (1/4 covered)
- Missing labeled states: Draw Choice, Pending Placement, Invalid Placement
- Manual registration candidates: `0`
- Triaged non-state screenshots: `1`
- Review readiness: `partial_evidence`
- Manual record status: `not_recorded`
- Wave Combat action showcase: `False` (legacy_wave_combat_capture_without_attack_labels)

No standalone PNG candidates remain for the missing required states. Capture fresh evidence instead of trying to register old screenshots.

## Focused Retake Targets
| State | Unity menu path | Must see |
| --- | --- | --- |
| Draw Choice | `Tools > Food Truck Prototype > Prepare and Capture State > Draw Choice` | Three comparable cards; shape, ingredient, value/risk, Fit, Heat, and Role readable; battlefield is still visible. |
| Pending Placement | `Tools > Food Truck Prototype > Prepare and Capture State > Pending Placement` | Pending block, 3x3 board, recommendation reason, rotation, and next action readable; battlefield still occupies the main screen. |
| Invalid Placement | `Tools > Food Truck Prototype > Prepare and Capture State > Invalid Placement` | Blocked reason and Next recovery hint appear near the board or cue, without requiring the full log. |

## Wave Combat Note
Wave Combat has legacy or incomplete action-showcase evidence. If Play Mode is available, retake Wave Combat too and confirm `-12`, `KO`, `LEAK`, `TRUCK -7`, and lane flash are readable.

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
