# Prototype Gap And Update Policy

Last updated: 2026-05-25 KST

## Purpose
This document consolidates the 2026-05-25 sub-agent review into one operating
policy. It answers three questions:

- What is currently missing from the game?
- Which work can continue without direct designer/player intervention?
- How should the project choose and sequence future updates?

The central product rule remains rhythm-first: every gameplay or UX update must
improve one named beat in the loop:

`Read -> Commit -> Pressure -> Payoff -> Release`

## Sub-Agent Review Scope
Four independent review roles were assigned:

| Role | Focus | Main verdict |
| --- | --- | --- |
| Project/technical status auditor | Verification, evidence, Git/worktree, tooling risk | Source guards are stable, but Play Mode evidence is still partial. |
| Core-loop/game-design auditor | Fun, decision quality, long-term loop motivation | The loop has good ingredients, but choices need stronger cause-and-effect. |
| Screen/UX/feedback auditor | Battlefield readability, HUD, attack feedback, layout policy | Damage feedback exists, but cause labels are still too weak. |
| Roadmap/policy auditor | Update policy, no-user-intervention work, PASS/FIX/BLOCKED flow | The project needs explicit operating rules for blocked evidence states. |

## Current Baseline
- Code-level guards are healthy: HUD contract, static checks, layout checks, and
  integrated gate are expected to pass in local script verification.
- Unity compile/tests remain inconclusive in the current environment, so manual
  Play Mode evidence is still required before claiming UX PASS.
- Play Mode evidence is partial: Wave Combat has one registered screenshot, while
  Draw Choice, Pending Placement, and Invalid Placement still need focused
  retakes.
- Wave Combat evidence is also not final because the older screenshot lacks the
  latest action showcase labels.
- Large Unity/Asset Store imports remain untracked. They must not be staged
  unless the project explicitly decides to include them.

## Missing Areas
### 1. Evidence Gap
The largest project risk is not a code feature. It is missing visual proof.

Required evidence states:
- Draw Choice
- Pending Placement
- Invalid Placement
- Wave Combat with action showcase

Policy:
- Do not record PASS without current evidence for all four states.
- Do not treat source guards as visual approval.
- Treat PC/Unity capture failure as `BLOCKED` or `partial_evidence`, not as PASS.

### 2. Choice Meaning Gap
Draw currently has three choices and useful tags, but it can still feel like
choosing the best-looking stat instead of making a strategic decision.

Policy:
- Every Draw set should express three different intentions: `safe`, `greedy`,
  and `synergy/utility`.
- Each card should connect to at least one live context signal:
  Heat, lane pressure, board fit, recipe progress, merge opportunity, or recovery
  need.
- Card copy should answer: "Why would I pick this now?"

### 3. Placement Cause-And-Effect Gap
Placement has rotation, shape fit, and auto-merge, but the battle result does not
yet clearly say which placement caused which survival result.

Policy:
- Placement should be treated as a lane-defense decision, not only an inventory
  fill action.
- A placed block should visibly influence at least one of these: lane coverage,
  attack origin, recipe bingo, Heat profile, merge path, or recovery.
- Future payoff summaries should identify the best contributor when possible.

### 4. Combat Readability Gap
Damage, KO, LEAK, and TRUCK HP labels exist, but the player still may not know
which source caused the result.

Policy:
- Combat feedback should follow a three-part chain:
  `source -> target -> result`.
- Truck damage labels should distinguish the cause:
  `LEAK`, `BITE`, `OVERHEAT`, `PRESSURE`, or other explicit source.
- Attack trails should start from a visible block/recipe/source position when
  possible, then terminate on the damaged zombie.
- Wave-end payoff should summarize why the player survived or failed, not only
  what changed numerically.

### 5. Release/Reward Agency Gap
Rest reward now exists as an automatic `Repair`, `Cooling`, or `Stock` reward.
That is a good first Release beat, but it does not yet create player agency.

Policy:
- Keep the automatic reward until Play Mode evidence is stable.
- Later, promote rest rewards into a 2-choice or 3-choice decision only if the
  review shows Release feels too passive.
- Release reward choices should set up the next Draw intention, not merely grant
  flat resources.

### 6. Long-Term Motivation Gap
Recipes, bingo, Heat risk, and merge systems exist, but the run does not yet ask
the player to pursue a recognizable build plan.

Policy:
- Recipes should move from "random bonus" toward "build goal."
- Draw cards should surface recipe progress or near-bingo opportunities.
- Heat should remain the risk/reward economy, Threat should remain lane pressure,
  Combo should remain tempo, and Recipe should remain build identity.

## Work Authorization Policy
### Safe Without User Intervention
The agent may do these without waiting for the user:
- Read code and docs.
- Run non-destructive verification scripts.
- Run status, preflight, verifier, and preview-only commands.
- Update documentation that summarizes current state, policies, or handoff notes.
- Add or update source-level contract checks for already implemented behavior.
- Improve evidence tooling, capture helpers, review pack summaries, and stale-doc
  detection when files are clearly scoped.

### Needs User Or Evidence Approval
The agent should pause or require clear evidence before:
- Recording PASS/FIX/BLOCKED as an official manual result.
- Staging large untracked Asset Store/plugin/import folders.
- Treating a screenshot as a matching state when the state is visually ambiguous.
- Making broad new gameplay systems after evidence is still partial.
- Changing final art/import policy or deleting imported assets.

### Never Use As Status-Only
`Tools/Write-PrototypePlayModeResultFromSuite.ps1` can write result drafts even
when used for JSON-style output. Do not use it as a casual status command.
Reserve it for intentional result recording.

## Evidence-Blocked Work Policy
When Play Mode is blocked or the user cannot actively play, prioritize:

1. Focused retake planning and verification.
2. Screenshot triage and manual registration command preparation.
3. Review pack preview/readiness checks.
4. HUD/layout/source contract guards.
5. Documentation and policy consolidation.
6. Evidence tooling improvements.

Avoid:
- Wide content expansion.
- New unrelated mechanics.
- PASS claims.
- Large asset staging.
- Balance changes that cannot be inspected through telemetry or source guards.

Exception:
- A narrow gameplay/UX implementation is acceptable only if it improves a named
  rhythm beat, has source-level tests or verifier checks, and does not pretend to
  replace Play Mode review.

## PASS/FIX/BLOCKED Transition Policy
| Result | Meaning | Next action |
| --- | --- | --- |
| PASS | All four required states have current, readable evidence | Continue with rhythm tuning and core-loop upgrades. |
| FIX_LAYOUT | Screen composition, overlap, or touch/readability failed | Adjust layout, run layout/HUD guards, focused retake the failed state. |
| FIX_ASSET | Sprite, contrast, silhouette, or missing asset blocked readability | Fix/import asset intentionally, run asset verifier, retake affected states. |
| FIX_FEEDBACK | Cause/effect, copy, timing, or cue clarity failed | Classify failed beat, patch feedback, run HUD contract, focused retake. |
| BLOCKED | Environment, Unity, capture, compile, or unknown tool state prevented review | Record blocker, continue only safe local tooling/docs/guards. |

`FIX_FEEDBACK` must name the failed beat:
- `Read`: draw comparison unclear.
- `Commit`: placement/retry action unclear.
- `Pressure`: combat threat or ramp unclear.
- `Payoff`: result/learning unclear.
- `Release`: recovery/reward unclear.

## Future Update Policy
### P0: Close Evidence
- Capture or register Draw Choice, Pending Placement, Invalid Placement, and
  Wave Combat with action showcase.
- Verify suite, screenshots, review pack, and manual record in that order.

### P1: Fix The Weakest Beat
Pick exactly one failed beat and update only that surface.

Suggested mapping:
- Weak `Read`: strengthen card intent, recipe progress, lane/Heat context.
- Weak `Commit`: improve local board feedback, retry hint, placement cause.
- Weak `Pressure`: tune spawn/Heat ramp or make primary wave threat explicit.
- Weak `Payoff`: add best contributor and cause-to-next-decision summary.
- Weak `Release`: tune rest reward visibility or later add rest reward choice.

### P2: Strengthen Build Motivation
Only after P0/P1 are stable:
- Make recipes visible as goals during Draw.
- Add near-bingo/merge opportunity signals.
- Add limited reward choices that set up the next wave.

### P3: Content Expansion
Only after the loop is readable:
- Add wave-specific pressure themes.
- Add new enemies, recipes, modules, or events one at a time.
- Require each content item to name its affected beat and evidence state.

## Concrete Improvement Backlog
2026-05-25 source update: the first Combat cause label pass is complete. Truck
damage floaters now use cause labels such as `BITE`, `PRESSURE`, or `OVERHEAT`
instead of a generic `TRUCK` label, and the Wave Combat verification showcase
uses the same cause-labeled truck damage floater.

2026-05-25 payoff update: the first best-contributor pass is complete. Wave
outcome summaries now append a compact `Best ...` cause such as `Best combo x3`,
`Best KO chain`, `Best damage`, `Best cooling`, `Best recovery`, or `Best stock`
so the Payoff beat teaches what worked before the next Read beat.

2026-05-25 draw intent update: the first Draw card intent pass is complete.
Draw choices now keep their `Fit`, `Heat`, and `Role` tactical labels while
adding a compact live-context `Intent` label: `SAFE`, `GREEDY`, `SYNERGY`,
`UTILITY`, or `HOLD`. The label is derived from board fit count, Heat warning
thresholds, assist tag, value bucket, risk tag, shape size, and target type.

### Highest Priority
1. Play Mode focused retakes for the three missing states.
2. Wave Combat retake with action showcase labels.
3. Combat cause labels: continue from truck HP cause labels toward full
   `source -> target -> result` attack trails.
4. Payoff best-contributor summary: first source pass complete; next visual
   review should confirm the `Best ...` cause is readable and useful.
5. Draw card intent labels: first source pass complete; next visual review
   should confirm whether the added `Intent` line is readable in the card row.

### Medium Priority
6. Board-local invalid placement labels near the failed cell.
7. Recipe progress/near-bingo signal during Draw.
8. Rest reward visibility tuning.
9. Heat ramp review after pressure spawn ramp is visually judged.

### Later
10. Rest reward choice.
11. Wave-specific pressure themes.
12. Build archetype/replay motivation pass.
13. Audio/SFX rhythm policy.

## Acceptance Standards
- The game view occupies at least half of the first viewport in required states.
- The truck reads as one long food truck/defense line, not repeated lane trucks.
- The player can explain why HP changed without reading the full log.
- The player can explain why one Draw card is safe, one is greedy, and one is
  synergy/utility.
- The player can recover from a failed placement using only local feedback.
- A wave result explains at least one cause and one next decision.
- Every gameplay update names its beat and has either Play Mode evidence or a
  source-level guard plus explicit manual-review TODO.

## Reference Files
- `Docs/Prototype_NextStep_Playbook.md`
- `Docs/Prototype_Update_Roadmap.md`
- `Docs/Prototype_RhythmDesign_Audit.md`
- `Docs/Prototype_Session_Handoff.md`
- `Docs/Prototype_PlayMode_Verification.md`
- `Docs/Prototype_PlayMode_RetakePlan.md`
- `Docs/Prototype_PlayMode_Verification_Suite.txt`
- `Assets/Scripts/Prototype/FoodTruckRunModel.cs`
- `Assets/Scripts/Prototype/FoodTruckPrototypeHud.cs`
- `Assets/Scripts/Prototype/FoodTruckPrototypeHud.EnemyVisuals.cs`
- `Assets/Scripts/Prototype/FoodTruckPrototypeHud.PendingPlacementAssist.cs`
- `Tools/Verify-PrototypeHudStateContract.ps1`
- `Tools/Verify-PrototypePlayModeSuite.ps1`
- `Tools/Verify-PrototypePlayModeScreenshots.ps1`
