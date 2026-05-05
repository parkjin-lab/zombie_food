# Prototype Update Roadmap

Last updated: 2026-05-05 KST

## Purpose
This roadmap keeps the next prototype updates grounded in the verified core loop:
`Draw 3 -> Choose 1 -> Rotate/Place -> Survive Wave -> Read Outcome -> Next Decision`.

The priority is not adding broad new systems yet. First, keep the loop readable,
repeatable, and easy to verify with code guards, screenshots, and manual Play Mode
evidence.

## Current Status
- Code guard status: `gate_status=ok`, `layout_status=ok`, `hud_contract_status=ok`, `static_status=ok`.
- Environment limit: Unity compile/tests remain `inconclusive` in the current PC/headless setup.
- Play Mode suite: `playmode_suite_status=not_recorded`, `captured_count=0/4`; the PC-limited Play Mode suite is not recorded as of 2026-05-05 KST.
- Screenshot evidence: `playmode_screenshot_status=partial`; existing portrait PNG evidence is useful, but does not provide full suite coverage.
- Manual record: `playmode_record_status=not_recorded`; Wave Combat has a prior `PASS`, while Draw Choice, Pending Placement, and Invalid Placement remain `NOT_RECORDED`.
- Review pack: `Write-PrototypePlayModeReviewPack.ps1 -PreviewOnly -JsonOnly` reports `partial_evidence`.
- Core loop update: source-level wave outcome/payoff summary is now implemented as a model summary plus HUD cue; Play Mode visual readability is still pending.
- Draw Choice update: cards now expose tactical `Fit`, `Heat`, and `Role` chips alongside existing shape, target, value, and risk signals; Play Mode readability is still pending.
- Invalid Placement update: blocked placement now surfaces a corrective next action together with the fail reason; Play Mode readability is still pending.
- Pending Placement update: R1/R2 recommendation copy now explains lane pressure, multi-lane coverage, and center positioning instead of leading with raw scores; Play Mode readability is still pending.
- Battlefield readability update: placement/draw states now reserve more than half of the viewport for the truck-and-zombie play area, show one long food truck marker, and use brighter attack trails/impact flashes; Play Mode readability is still pending.
- Combat result readability update: hits now surface floating `-damage`, `KO`, `LEAK`, and `TRUCK -HP` text in the battlefield; Play Mode readability is still pending.

## Start Here
1. Print the current session status.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
2. If the PC can run Unity Play Mode, use `Tools > Food Truck Prototype > Capture Verification Suite`.
3. Verify suite and screenshot evidence.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
4. Build the review pack before manual visual judgment.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
5. Manually judge Draw Choice, Pending Placement, Invalid Placement, and Wave Combat as `PASS/FIX/BLOCKED` when the suite can be captured.
6. Record the result only after evidence is available.
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
- Confirm all suite states are represented in the suite verifier output.
- Use screenshot verification to distinguish partial portrait evidence from full suite-ready evidence.
- Build the review pack before visual judgment.
- Use `Write-PrototypePlayModeResultFromSuite.ps1` or the Unity `Record PASS Manual Result` menu only after fresh evidence exists.

### Acceptance
- `playmode_suite_status=captured`, or the missing capture reason is explicit.
- `playmode_screenshot_status=suite_ready`, or the partial/missing screenshot reason is explicit.
- `Prototype_PlayMode_Verification.md` links the latest manual result to current evidence.
- HUD changes pass `Verify-PrototypeHudStateContract.ps1`.
- Layout changes pass `Verify-PrototypeLayout.ps1`.

## 2. Core Loop Fun Reinforcement
### Goal
- Make each draw feel like a tradeoff between safety, payoff, and utility.
- Make placement feedback explain what the player can do next.
- Make wave results clearly explain why the previous decision helped or hurt.

### Short-Term Focus: Wave Outcome / Payoff Summary
After each wave, show a concise combat result summary before the next draw decision.
This should become a near-term core loop improvement because it closes the feedback
gap between "I placed something" and "that choice mattered."

Implementation status: the model now records a concise wave payoff summary and the
HUD wave-change cue surfaces it. The remaining product task is to verify readability
in portrait Play Mode and decide whether the cue should become a persistent panel.

The payoff is now also suitable for a compact persistent chip so the player can
re-read the last wave result while making the next placement or draw decision.

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
knockouts, lane leaks, and truck HP loss directly over the battlefield. The remaining
product task is to retake Play Mode screenshots and judge overlap/readability on the
actual Unity viewport.

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
- If the cue is too fleeting or crowded, promote it into a compact persistent payoff panel.
- In the wave summary, keep at least one visible cause-and-effect item from the last placement or wave decision.

### Acceptance
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

### Operating Loop
1. Start session: use show status to check readiness and unresolved issues.
2. Run the relevant verifier before changing behavior.
3. Implement: change only one player-facing variable per session when possible.
4. Capture: use Capture Verification Suite when PC conditions allow.
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
- Even when automated verifiers are `ok`, make the final Play Mode readability call from the review pack and manual visual judgment.
