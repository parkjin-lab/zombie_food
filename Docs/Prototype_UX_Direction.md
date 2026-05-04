# Food Truck Apocalypse Prototype - UX Direction

## Product Pillar
- 10-second readability: player understands the current step immediately.
- 30-second tension: each micro-loop creates a meaningful choice.
- 5-minute mastery arc: player sees clear skill growth in one session.

## Core Loop (Target)
1. Draw 3 options.
2. Choose 1 option.
3. Rotate and place.
4. Survive wave pressure and heat.
5. Convert result into next decision (draw/sell/reposition timing).

Related roadmap: `Docs/Prototype_Update_Roadmap.md`.

## UX Procedure Risks (Audit)
- Event modal and placement flow can conflict if input is not hard-gated.
- Next-wave trigger can skip tactical setup if usable outside rest.
- Feedback can feel weak when invalid placement reason is unclear.
- Flow guidance can become static if not updated each frame/state transition.

## What Is Already Patched
- Event keyboard path: 1/2/3 option selection enabled in EventPending state.
- Next-wave guard in HUD: only allowed in rest phase.
- Flow checklist now updates continuously with hover/drag context.
- Cue banner now fades and updates in runtime loop.
- Pending hint text now supports event/draw/pending contexts consistently.
- Action buttons now respect phase gating (event/draw/has pending/can vent/can burst).
- Source-level HUD state contract guard now checks Draw Choice, Pending Placement, Invalid Placement, telemetry, and regression coverage when Play Mode capture is blocked.
- Editor Play Mode helpers can now prepare and capture Draw Choice, Pending Placement, Invalid Placement, and Wave Combat states with minimal direct input.
- The Play Mode verification suite can batch all four required capture states into one evidence pass and preserve a state-by-state manifest.
- The suite manifest is now machine-checkable so missing screenshots are caught before manual PASS/FIX recording.
- Captured screenshot files now have a local PNG quality and state-coverage verifier before visual PASS/FIX review.
- Suite, screenshot, and record status can now be assembled into one review pack with a contact sheet and result command templates.
- PASS recording now reads the saved suite manifest so screenshot evidence is retained after Editor reloads.
- PASS/FIX/BLOCKED manual outcomes can now be drafted from suite evidence without hand-editing the verification markdown.

## Core Fun Reinforcement Plan
### Loop Design Target
- Draw should feel like a tempting fork, not a random menu.
- Choose should force a readable tradeoff: power now, heat risk, lane coverage, or future setup.
- Rotate and place should reward hand skill with immediate grid feedback and a small success snap.
- Survive should prove whether the choice worked, then turn the result into a reward or unlock decision.
- Failure should teach the next action within one beat: what was wrong, where to try, and whether the wave state made it costly.

### Connected Fun Levers
| Loop beat | Concrete update | Why it adds tension or feel | Telemetry to watch |
| --- | --- | --- | --- |
| Draw | Make the 3 cards intentionally uneven: safe, greedy, utility. Show EV, heat risk, and targeting profile on each card. | The player can argue with the choice instead of picking the largest number. | `draw_choice_pick_rate`, `draw_choice_decision_ms`, pick spread by EV/risk/tag |
| Choose | Add a light next-wave pressure hint before confirming the card. | A risky card becomes exciting when the incoming lane or heat context is visible. | greedy pick rate during high pressure, post-pick survival delta |
| Rotate | Preview covered cells, anchor cell, blocked cells, and heat footprint while rotating. | Rotation becomes a small spatial puzzle instead of a hidden validation step. | `rotations_per_pending`, `pending_hover_ms`, `pending_to_place_success_rate` |
| Place | Use a short valid-place pop and an invalid-place shake/red flash near the grid. | The hand action gets a reward beat, and misses feel intentional rather than buggy. | valid placement time, `blocked_place_reason_count`, retry-after-fail time |
| Survive | Show one local combat readout for the placed item: kills, heat added, leak stopped, or combo enabled. | The wave pays off the earlier decision with visible proof. | kills/heat per placed item, `overheat_events_per_wave`, wave clear margin |
| Reward | After selected waves, offer one reward from the same vocabulary: new part, heat relief, reroll, lane tool, or shape upgrade. | The loop closes by converting survival into the next decision. | reward pick rate, unlock use rate, next-wave survival delta |

### Can Test Now
- Draw tension with dummy labels:
  - Add EV, Risk, and Target tags even before final balance data exists.
  - Force one safe / one greedy / one utility card in prototype draws.
  - Validate with source checks and Draw Choice screenshot capture.
- Placement readability:
  - Surface `LastPlacementFailReasonText` in pending hint and checklist.
  - Keep reason buckets stable: out-of-bounds, occupied, invalid-anchor, shape-mismatch, no-pending.
  - Validate with `Tools\Verify-PrototypeHudStateContract.ps1`, Pending Placement capture, and Invalid Placement capture.
- Placement hand feel:
  - Add one timing preset for success pop (0.12-0.18s) and one preset for invalid shake/red flash.
  - Use manual Play Mode only for timing judgment; use screenshot evidence to confirm state coverage.
- Wave result feedback:
  - Add a simple post-wave line that names why the last placement mattered.
  - Start with console/CSV counters before building a full results panel.
- Lightweight reward experiment:
  - Prototype a rest-phase reward choice using placeholder rewards.
  - Gate it after wave 1 or wave 3 so the draw/place/survive loop has a visible payoff.
  - Record manual PASS/FIX/BLOCKED from the existing suite evidence instead of hand-editing verification notes.

### Later Experiments
- True reward economy:
  - 3-wave unlock cadence:
    - wave 1-3: basic lane pressure literacy.
    - wave 4-6: heat-risk management decisions.
    - wave 7+: advanced shape and target synergies.
  - Add unlock families for shape modifiers, heat tools, combo triggers, rerolls, and lane control.
- Stronger wave pressure:
  - Add enemy or route variants that make targeting profile matter.
  - Add wave preview rules only after the current draw/place flow is stable.
- Deeper placement mastery:
  - Add adjacency bonuses, heat vents, combo sockets, or limited reposition windows.
  - Track whether players rotate for optimization, not just to find any legal placement.
- VFX and audio bus:
  - Standardize triggers for event select, valid place, invalid place, combo burst, reward claim, and overheat spike.
  - Tune juice after the core telemetry says players understand the loop.
- Broader playtest pass:
  - Run repeated manual Play Mode sessions only after the automated evidence pack is green.
  - Test one new variable at a time because manual Play Mode capture is still the bottleneck.

### Telemetry Readout Targets
- Choice tension:
  - No single card family should dominate early picks unless it is intentionally tutorial-safe.
  - `draw_choice_decision_ms` should rise when risk tags conflict with wave pressure.
- Placement feel:
  - `pending_to_place_success_rate` should improve after reason text and preview fixes.
  - `rotations_per_pending` should stay high enough to show spatial play but not so high that players are lost.
- Failure feedback:
  - `blocked_place_reason_count` should shift from repeated same-reason failures toward faster retries.
  - Retry-after-fail time should fall after shake, flash, and reason copy are added.
- Wave payoff:
  - Overheat, leak, and clear-margin counters should explain why a wave was lost.
  - Reward picks should correlate with the problem the player just experienced.

## Manual Verification Guardrails
- Treat automated checks as the acceptance path for HUD contracts, state coverage, manifests, PNG quality, and review-pack completeness.
- Keep `Tools\Verify-PrototypeHudStateContract.ps1` green after Draw, Pending, Invalid Placement, telemetry, or reward HUD changes.
- Use the Play Mode suite to capture Draw Choice, Pending Placement, Invalid Placement, and Wave Combat before manual review.
- Use manual Play Mode for the feel questions automation cannot answer yet: animation timing, placement snap satisfaction, reward comprehension, and pressure readability.
- For each experiment, record the expected metric movement before testing, then mark PASS/FIX/BLOCKED from suite evidence.

## Presentation Upgrade Track
- Keep cue banners short (single action sentence).
- Prioritize spatial feedback near interaction target (grid first, header second).
- Ensure all critical state changes have one visual + one text cue.
- Keep reward and wave-result copy in the same plain vocabulary as card tags.

## Next Sprint Checklist
1. Add draw-card EV, Risk, and Target labels with dummy values first.
2. Add explicit blocked reason strings for occupied, out-of-bounds, invalid-anchor, shape-mismatch, and no-pending.
3. Add the success pop and invalid shake/red flash presets.
4. Add telemetry counters and a simple CSV/console summary for choice, placement, failure, heat, and reward picks.
5. Add one placeholder rest-phase reward choice after wave 1 or wave 3.
6. Validate on vertical mobile aspect ratio with the touch-first script.
7. Keep `Tools\Verify-PrototypeHudStateContract.ps1` green and refresh the suite manifest/contact sheet after HUD changes.
