# Prototype Update Roadmap

Last updated: 2026-05-17 01:18 KST

## Purpose
This roadmap keeps the next prototype updates grounded in the verified core loop:
`Draw 3 -> Choose 1 -> Rotate/Place -> Survive Wave -> Read Outcome -> Next Decision`.

The priority is not adding broad new systems yet. First, keep the loop readable,
repeatable, and easy to verify with code guards, screenshots, and manual Play Mode
evidence.

## Current Status
- Code guard status: `gate_status=ok`, `layout_status=ok`, `hud_contract_status=ok`, `static_status=ok`.
- Environment limit: Unity compile/tests remain `inconclusive` in the current PC/headless setup.
- Play Mode suite: `playmode_suite_status=manual_partial`, `captured_count=1/4`, `suite_capture_source=manual screenshot registration`; Wave Combat has one manually registered PNG, while Draw Choice, Pending Placement, and Invalid Placement remain missing.
- Screenshot evidence: `playmode_screenshot_status=partial`; existing portrait PNG evidence is useful, Wave Combat is labeled through the suite manifest, `manual_registration_candidate_count=0`, `triaged_non_state_count=1`, but full suite coverage is still missing.
- Manual record: `playmode_record_status=not_recorded`; Wave Combat has a prior `PASS`, while Draw Choice, Pending Placement, and Invalid Placement remain `NOT_RECORDED`.
- Session status/review pack/retake plan/preflight: `Show-PrototypeSessionStatus.ps1` now carries Wave Combat action showcase readiness, suite capture source, manual registration candidate count, triaged non-state count, `review_pack_status`, `review_readiness`, `review_pack_visual_review_required`, `retake_plan_status`, `retake_plan_focused_retake_count`, `retake_plan_doc_status`, `top_issue`, `next_evidence_action`, and `next_code_target`; `Invoke-PrototypePlayModeEvidencePreflight.ps1` combines those checks with suite/screenshot/review preview and currently reports `playmode_evidence_preflight_status=ready_for_focused_retake`.
- Core loop update: source-level wave outcome/payoff summary is now implemented as a model summary plus HUD cue; Play Mode visual readability is still pending.
- Draw Choice update: cards now expose tactical `Fit`, `Heat`, and `Role` chips alongside existing shape, target, value, and risk signals; Play Mode readability is still pending.
- Invalid Placement update: blocked placement now surfaces a corrective next action together with the fail reason; Play Mode readability is still pending.
- Pending Placement update: R1/R2 recommendation copy now explains lane pressure, multi-lane coverage, and center positioning instead of leading with raw scores; Play Mode readability is still pending.
- Battlefield readability update: placement/draw states now reserve more than half of the viewport for the truck-and-zombie play area, show one long food truck marker, and use brighter attack trails/impact flashes; Play Mode readability is still pending.
- Combat result readability update: hits now surface floating `-damage`, `KO`, `LEAK`, and `TRUCK -HP` text in the battlefield; Play Mode readability is still pending.
- Wave Combat capture update: verification setup now injects a readable action showcase so suite evidence can catch combat labels and lane flash without long manual play; suite/session/review outputs now expose `wave_combat_action_showcase_ready/reason`.
- Rhythm design audit: `Docs/Prototype_RhythmDesign_Audit.md` now treats rhythm as the central difficulty/fun lens. Verdict: the prototype has strong rhythm ingredients, but needs an explicit beat map and review criteria so tension, variation, payoff, and release are tuned intentionally.
- 2026-05-17 sub-agent review: tracked source/docs were clean before this pass, code guards remain stable, Play Mode evidence is still the blocker, and imported Unity/Asset Store folders remain untracked. Do not let untracked asset imports blur the prototype checkpoint.

## Start Here
1. Print the current session status, including Wave Combat action showcase readiness, review pack readiness, and next-work focus.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
2. Generate the focused retake plan before opening Unity when missing states remain and no manual registration candidates are available.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   powershell -ExecutionPolicy Bypass -File "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
3. If the PC can run Unity Play Mode, use `Tools > Food Truck Prototype > Capture Verification Suite`.
   If only standalone PNGs are available, register them as explicit manual evidence.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Register-PrototypePlayModeManualEvidence.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -State "Wave Combat" -ScreenshotPath "Docs\PlayModeScreenshots\foodtruck-playmode-20260504-010153.png" -PreviewOnly -JsonOnly
   ```
4. Verify suite and screenshot evidence.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
5. Build the review pack before manual visual judgment, and check the Wave Combat action showcase status.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
6. Manually judge Draw Choice, Pending Placement, Invalid Placement, and Wave Combat as `PASS/FIX/BLOCKED` when the suite can be captured.
7. Record the result only after evidence is available.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS -Apply
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
   ```

## 1. Short-Term Verification Stability
### Goal
- Keep code, document, and screenshot evidence from drifting apart.
- Let automated guards catch contract/layout regressions, while human Play Mode review covers actual usability and readability.
- Treat current PC Play Mode limits as an evidence gap, not as a gameplay pass.

### Next Actions
- Keep `Show-PrototypeSessionStatus.ps1` as the session-start command.
- Treat `wave_combat_action_showcase_ready=false` in session status as an evidence gap, not a Wave Combat visual PASS.
- Treat `review_readiness=partial_evidence` in session status as a prompt to capture/retake evidence before recording manual results.
- Treat `top_issue` and `next_evidence_action` as the first action filter before choosing new gameplay code work.
- Use `Write-PrototypePlayModeRetakePlan.ps1` when `manual_registration_candidate_count=0` and Draw/Pending/Invalid states remain missing.
- Use `Verify-PrototypePlayModeRetakePlan.ps1` after writing the focused retake plan so stale capture instructions fail before Play Mode.
- Use `Invoke-PrototypePlayModeEvidencePreflight.ps1` before opening Unity so focused retake readiness, stale-doc state, screenshot health, and after-capture commands are visible in one output.
- Use `Register-PrototypePlayModeManualEvidence.ps1` when the PC can capture a standalone PNG but cannot complete direct Play Mode input or the full suite menu.
- Use screenshot verifier or review pack preview `manual_registration_commands` so standalone PNGs are registered only after visual confirmation of the matching state.
- Record visually reviewed non-required PNGs in the screenshot triage manifest so they stop appearing as manual registration candidates.
- Confirm all suite states are represented in the suite verifier output.
- Use screenshot verification to distinguish partial portrait evidence from full suite-ready evidence.
- Build the review pack before visual judgment.
- Require the review pack to show whether the Wave Combat action showcase is ready before recording Wave Combat as PASS.
- Use `Write-PrototypePlayModeResultFromSuite.ps1` or the Unity `Record PASS Manual Result` menu only after fresh evidence exists.

### Acceptance
- `playmode_suite_status=captured` / `captured_manual`, or the missing/manual partial capture reason is explicit.
- `playmode_screenshot_status=suite_ready`, or the partial/missing screenshot reason is explicit.
- `review_pack_status=ok` and `review_readiness=ready_for_visual_review`, or the partial evidence reason is explicit.
- `retake_plan_status=ok` reports a concrete focused retake count and next action while evidence is partial.
- `retake_plan_doc_status=ok` and documented/expected focused retake counts match.
- `playmode_evidence_preflight_status=ready_for_focused_retake` while Draw/Pending/Invalid evidence is missing, then `ready_for_visual_review` after suite-ready evidence exists.
- Session status reports a concrete `top_issue`, `next_evidence_action`, and `next_code_target`.
- Session/review output reports `suite_capture_source` so manual evidence is not confused with a full Unity suite capture.
- Screenshot/review output reports manual registration candidates and command templates for any unlabeled PNGs that pass machine quality.
- Screenshot/review output reports triaged non-state screenshots separately from usable manual registration candidates.
- Wave Combat session/review evidence reports `wave_combat_action_showcase_ready=true`, or the missing showcase reason is explicit.
- `Prototype_PlayMode_Verification.md` links the latest manual result to current evidence.
- HUD changes pass `Verify-PrototypeHudStateContract.ps1`.
- Layout changes pass `Verify-PrototypeLayout.ps1`.

## 2. Core Loop Fun Reinforcement
### Goal
- Make each draw feel like a tradeoff between safety, payoff, and utility.
- Make placement feedback explain what the player can do next.
- Make wave results clearly explain why the previous decision helped or hurt.
- Make the whole loop feel rhythmic: read, commit, pressure, payoff, release, then variation.

### Rhythm Design Direction
Rhythm is now the primary design lens for the prototype. A fun run should not feel
like a flat stream of systems. It should feel like a beat:
`Read -> Commit -> Pressure -> Payoff -> Release/Variation`.

Current assessment: the model already contains rhythm material: 20-second combat
waves, 10-second rest windows, 8-second combo timing, 7-second vent cooldowns,
Heat warning/overheat bands, wave 3 events, wave 4/7 unlocks, wave 4 weather
rotation, wave 5 boss/rest spikes, recipe durations, and payoff summaries. The
gap is that these are not yet composed as one cadence. They can stack accidentally
instead of producing a deliberate tension envelope.

Near-term rule: every next feature or tuning change should name which beat it
improves: `Read`, `Commit`, `Pressure`, `Payoff`, or `Release`.

Design reference: `Docs/Prototype_RhythmDesign_Audit.md`.

### Next Rhythm System Candidates
The next fun work after Play Mode evidence closes should be narrow and testable.
Recommended order:

1. `Wave Cadence Composer`: move the scattered wave 3 event, wave 4 weather,
   wave 5 boss/rest, wave 4/7 unlock, overheat, and recipe timing into an
   inspectable cadence schedule. Avoid accidental event/weather/boss/unlock
   overstacking unless the wave is intentionally a planned spike.
   Source status: first pass complete for wave event/weather/boss/rest/unlock
   beats. `FoodTruckRunModel` now exposes `LastWaveCadencePlan`,
   `LastWaveCadenceSummary`, scheduled beat count, and planned-spike state
   without consuming weather/event random rolls. Remaining work is visual
   review and later inclusion of overheat/recipe rhythm if needed.
2. `Payoff-to-Read Panel`: extend the existing wave payoff cue into a compact
   next-decision hint. Examples: `Leak x2 -> value lane control`,
   `PeakHeat +18 -> pick COOL/SAFE`. Keep this short and test the wording with
   Play Mode review before adding more content.
   Source status: first pass complete as a persistent `Next` chip beside the
   wave payoff chip. It derives from leak count, HP delta, Heat/peak Heat,
   damage/KOs, combo, and recovery without changing combat balance.
3. `Rhythm Beat HUD/Telemetry`: expose `Read`, `Commit`, `Pressure`, `Payoff`,
   and `Release` as a lightweight label or telemetry state only after the first
   two items prove the cadence and payoff direction.
   Source status: first pass complete. `FoodTruckRunModel` resolves the current
   beat, the HUD status line shows `Beat <label>`, and the UX telemetry panel/mini
   line reuse the same label. Remaining work is state duration and transition
   telemetry after Play Mode readability review.
4. `Pressure Ramp Tuning`: make the 20-second wave visibly rise from readably
   low pressure to a peak without causing Heat/Threat/Combo noise.
5. `Rest-Phase Reward`: convert release windows into a small 1-of-3 reward only
   after flow-lock and evidence tooling are stable.

Risk control: each candidate must include an EditMode or verifier check plus a
manual Play Mode review criterion. Do not implement multiple rhythm candidates in
one pass.

Composer acceptance now includes: Wave 3 event cadence, Wave 4 unlock/weather
planned spike, Wave 5 boss/rest planned spike, and Wave 7 non-spike unlock must
be covered by EditMode tests and the HUD state contract.

Payoff-to-Read acceptance now includes: leak, Heat spike, damage payoff, and
copy-trimming cases must be covered by EditMode tests; the HUD state contract
must guard the `Next` chip and bounded hint text.

Rhythm Beat acceptance now includes: `Read`, `Commit`, `Pressure`, `Payoff`, and
`Release` mapping must be covered by EditMode tests; the HUD state contract must
guard the beat label in both the HUD status line and UX telemetry text.

### Short-Term Focus: Wave Outcome / Payoff Summary
After each wave, show a concise combat result summary before the next draw decision.
This should become a near-term core loop improvement because it closes the feedback
gap between "I placed something" and "that choice mattered."

Implementation status: the model now records a concise wave payoff summary and the
HUD wave-change cue surfaces it. The remaining product task is to verify readability
in portrait Play Mode and decide whether the cue should become a persistent panel.

The payoff is now also suitable for a compact persistent chip so the player can
re-read the last wave result while making the next placement or draw decision.

### Short-Term Focus: Payoff-to-Read Panel
The payoff-to-read pass turns wave result into a short next decision prompt:
`Stabilize lanes`, `Pick COOL/SAFE`, `Push damage`, `Keep combo window`,
`Spend recovery`, or `Keep balanced draw`.

This should stay compact. If Play Mode shows the hint competing with urgent
combat warnings or recipe chips, shorten the copy or reserve it for Draw/Pending
states instead of expanding the panel.

### Short-Term Focus: Battlefield Readability
The play area should look like the main game, even while the player is choosing or
placing blocks. Keep the truck-and-zombie area above 50% of the viewport during
Draw Choice and Pending Placement, use one clear food truck marker, and make each
attack show a visible trail plus impact so the player can understand why enemies
lose HP or why truck HP changes.

Implementation status: the layout constants and layout guard now enforce a larger
battlefield during placement contexts. Enemy hit visuals now include a truck-to-zombie
attack trail, larger impact flashes, longer hit poses, brighter lane damage flash,
and a single longer truck marker. Floating combat text now labels enemy damage,
knockouts, lane leaks, and truck HP loss directly over the battlefield. Wave Combat
verification setup now injects a short action showcase for screenshots, and the
suite/review pack reports whether that showcase is present. The remaining product
task is to retake Play Mode screenshots and judge overlap/readability on the actual
Unity viewport.

Recipe activation feedback now follows the same cause-and-effect direction. When a
recipe comes online, the log and HUD banner should preserve the trigger source:
placement bonus, auto-merge bonus, manual merge bonus, Recipe Rush event, or a 3x
bingo condition with the best grade.

The Synergy Bar should also retain the latest recipe trigger as a compact recent
cue so a player can still connect "what I just did" to the active recipe after the
banner fades.

Active recipe chips should describe the payoff in-place: passive recipes read as
recovery/cooling support, while active recipes read as lane-hit pressure.

When a recipe expires, it should close the loop by reporting accumulated payoff:
damage, KOs, HP restored, and Heat relieved. This lets recipe rewards feel earned
instead of purely decorative.

While a recipe is still active, the Synergy Bar should show live progress so the
player can watch payoff build instead of waiting only for the expiry summary.

The summary should highlight:
- Wave result: cleared, failed, truck damage, remaining HP, and clear margin.
- Payoff: damage dealt, enemies stopped, Heat gained/relieved, and any combo contribution.
- Best contributor: the card/module/placement that had the clearest visible impact.
- Problem signal: leaked lane, overheat spike, blocked angle, or wasted coverage.
- Next-decision hint: a short reason the next draw should value safety, greedy payoff, utility, or Heat control.

### Next Actions
- Use the rhythm audit when choosing next work; do not add a mechanic unless it improves a named beat.
- During Play Mode review, judge whether the player can feel a clear pressure ramp and payoff/release before the next choice.
- Track accidental overlap between event, weather, unlock, boss/rest, overheat, and recipe beats.
- If evidence is PASS or a concrete FIX is recorded, review the source-level
  `Wave Cadence Composer` in Play Mode before wider content. If rhythm
  readability is still weak, implement `Payoff-to-Read Panel` next.
- If payoff readability is the main FIX, tune the source-level `Payoff-to-Read`
  hint copy/visibility before cadence tuning.
- Verify that the three draw cards keep `Fit`, `Heat`, `Role`, `Value/Risk`, and `Target` readable in compact portrait layout.
- Keep card choices visibly different: safe, greedy, and utility picks should not blur together.
- Verify Pending Placement recommendation reasons: lane pressure, multi-lane coverage, and center/near-center placement should be readable without raw score interpretation.
- Verify Invalid Placement reason plus next-action copy near the board: `occupied`, `out_of_bounds`, `invalid_anchor`, `no_pending`.
- Verify Recipe activation banners: recipe name, tier, and trigger cause should be readable before they fade.
- Verify the Synergy Bar recent-recipe cue remains readable without crowding active recipe duration chips.
- Verify active recipe chips communicate both duration and effect role without requiring the combat log.
- Verify recipe expiry payoff cues are readable and do not compete with urgent combat warnings.
- Verify live recipe progress chips stay readable as payoff starts at zero and ramps up.
- Verify the wave outcome/payoff cue at the end of Wave Combat before the next draw.
- Verify the persistent wave payoff chip stays readable beside recipe cues.
- Verify the `Next` hint remains readable during Draw/Pending and does not tell
  the player to optimize the wrong thing after leaks, Heat spikes, or damage payoff.
- If the cue is too fleeting or crowded, promote it into a compact persistent payoff panel.
- In the wave summary, keep at least one visible cause-and-effect item from the last placement or wave decision.

### Acceptance
- The player can name the current beat: reading, committing, surviving pressure, reading payoff, or recovering.
- At least one release follows a spike, and at least one variation appears every 2-3 waves without every system firing at once.
- Intentional spike waves are distinguishable from accidental overlap in logs, tests, or review notes.
- The next-decision payoff cue is short enough to read during the next Draw/Pending state.
- The player can explain the difference between the three draw cards within three seconds.
- The player can compare at least one board-fit signal and one Heat/risk signal before choosing a card.
- Repeated placement failures from the same reason decrease across playtest sessions.
- After a failed placement, the next useful action is understandable without opening the full log.
- The player can explain why R1/R2 are recommended without interpreting numeric scores.
- The player can tell whether a recipe came from a bonus roll or a 3x bingo condition.
- The player can tell whether an active recipe is recovery/cooling support or lane-hit pressure.
- The player can see what an expired recipe actually accomplished.
- The player can tell whether an active recipe is already paying off or is still warming up.
- The Wave Combat screen keeps HP, Heat, Wave, lane pressure, and truck position readable at the same time.
- The wave outcome/payoff summary makes the last decision's effect understandable without reading logs.
- Draw/Pending/Invalid/Wave readability reaches `PASS`, or each `FIX` has a concrete evidence-backed reason.

## 3. Content And Wave Expansion
### Goal
- Add a 3-wave learning/tension/reward structure only after the verified loop is readable.
- Reuse the existing card tags and failure-reason language when adding content.

### Roadmap
- Wave 1-3: teach basic placement, lane pressure, and card roles.
- Wave 4-6: introduce Heat risk, greedy picks, and vent/relief choices.
- Wave 7+: introduce shape modifiers, targeting profiles, combo triggers, and limited repositioning.

### Next Actions
- Define per-wave targets: lane spread, expected clear margin, and likely player failure point.
- Clean up card/block tags: safe, greedy, utility, heat, lane, burst, reroll.
- Add only one placeholder reward choice after wave 1 or wave 3 to test the reward loop.
- Change one card, reward, or wave variable at a time to avoid Play Mode capture bottlenecks.

### Acceptance
- Each new content item states which existing suite state it affects.
- Each wave has at least one KPI: pick rate, success rate, overheat count, or clear margin.
- Rewards do more than repeat flat stat increases; they should influence the next decision.

## 4. Art And Feedback Policy
### Goal
- While art is still placeholder, prioritize silhouettes and feedback that support real player judgment.
- Avoid final-art polish that hides unresolved UX verification issues.

### Policy
- Card icons must be distinguishable by role even without text.
- Food truck, kitchen module, enemy, Heat/overheat, and placement cell visuals need separate silhouettes.
- Critical actions should have one visual cue and one text cue.
- Failure feedback should appear near the interaction point before relying on full logs.
- Cue banners should describe only the current action.

### Next Actions
- Define presets for placement success pop, failure shake/red flash, overheat spike, reward claim, and wave payoff summary.
- Keep placeholder PNG names stable so final assets can replace them cleanly.
- Use `Verify-PrototypeAssets.ps1` to catch required resource or `.meta` gaps.
- Re-capture suite screenshots after art replacement and compare readability in the review pack.

### Acceptance
- In portrait view, cards, board, and combat lane do not obscure each other.
- State is not communicated by color alone; silhouette, position, motion, or text also helps.
- Manual result can be recorded without `FIX_ASSET` before final-art replacement.

## 5. Data And Playtest Operation
### Goal
- Turn "this felt good/bad" into the minimum signal needed for the next code task.
- Keep manual Play Mode evidence repeatable across sessions.

### Metrics
- `placed_success / place_attempt`
- `blocked_place_reason_count` by reason
- `draw_choice_pick_rate` by card slot/tag
- `draw_choice_decision_ms`
- `draw_to_place_time_sec`
- `rotations_per_pending`
- `overheat_events_per_wave`
- `wave_clear_margin`
- `reward_pick_rate`
- `wave_outcome_summary_viewed`
- `wave_payoff_top_contributor`
- `rhythm_state_duration_sec`
- `rhythm_beat_label`
- `beat_transition_count`
- `spike_overlap_count`
- `payoff_visible_sec`
- `release_window_sec`

### Operating Loop
1. Start session: use show status to check readiness and unresolved issues.
2. Run the relevant verifier before changing behavior.
3. Implement: change only one player-facing variable per session when possible.
4. Capture: generate and verify the retake plan, then use Capture Verification Suite or focused Prepare and Capture State when PC conditions allow.
5. Verify: run suite verifier, screenshot verifier, and review pack in order.
6. Judge: record `PASS/FIX/BLOCKED` with the result writer.
7. Report: carry forward one top failure reason and one next code target.

### Acceptance
- Every playtest note includes build/date/resolution/result status.
- `FIX` results are classified first as `FIX_LAYOUT`, `FIX_ASSET`, `FIX_FEEDBACK`, or `BLOCKED`.
- The next session starts from `Top issue` and `Next code target`, not a broad idea list.

## Command Reference
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeHudStateContract.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeLayout.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -PreviewOnly -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -PreviewOnly -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeAssets.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Gate-Verification.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -RunTests -JsonOnly
```

## Do Not Skip
- Do not edit other docs or code just to tidy the roadmap while other workers may be active.
- Before adding new gameplay systems, verify Draw Choice, Pending Placement, Invalid Placement, and Wave Combat evidence.
- Before adding new mechanics, name the rhythm beat being improved: `Read`, `Commit`, `Pressure`, `Payoff`, or `Release`.
- Even when automated verifiers are `ok`, make the final Play Mode readability call from the review pack and manual visual judgment.
