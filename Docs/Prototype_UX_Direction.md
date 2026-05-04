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
### P0 (Now)
- Instrument loop quality:
  - draw_choice_pick_rate
  - pending_to_place_success_rate
  - blocked_place_reason_count
  - overheat_events_per_wave
- Keep one-screen focus: reduce unnecessary top HUD density during intense placement moments.

### P1 (Next)
- Show tactical expectation on each draw card:
  - expected DPS range
  - heat risk tag
  - targeting profile
- Add tactile result feedback:
  - placement success pop (0.12-0.18s)
  - invalid placement shake + red flash
  - combo-ready pulse language aligned with burst timing

### P2 (Scale)
- 3-wave unlock cadence:
  - wave 1-3: basic lane pressure literacy
  - wave 4-6: heat-risk management decisions
  - wave 7+: advanced shape/target synergies
- Standardize VFX trigger bus:
  - event select
  - valid place / invalid place
  - combo burst / overheat spike

## Presentation Upgrade Track
- Keep cue banners short (single action sentence).
- Prioritize spatial feedback near interaction target (grid first, header second).
- Ensure all critical state changes have one visual + one text cue.

## Next Sprint Checklist
1. Add explicit blocked reason strings (occupied / out-of-bounds / shape mismatch).
2. Add draw-card expectation labels (dummy values first).
3. Add micro-animation presets for place success/fail.
4. Add telemetry counters and simple CSV/console summary.
5. Validate on vertical mobile aspect ratio with touch-first script.
6. Keep `Tools\Verify-PrototypeHudStateContract.ps1` green after Draw/Pending/Invalid Placement HUD changes.
## Agent-Driven Next Step (2026-04-09)
1. Stabilize placement explainability
- Keep `LastPlacementFailReason` and `LastPlacementFailReasonText` as HUD source-of-truth.
- Surface blocked reason consistently in pending hint + checklist.

2. Tighten decision UX around draw choices
- Keep EV + Risk labels on each of 3 draw cards.
- Verify legibility under compact HUD and tall portrait layout.

3. Add lightweight telemetry pass
- Count blocked placement by reason (out-of-bounds / occupied / invalid-anchor / no-pending).
- Track draw pick distribution by EV bucket and risk tag.

4. Production-facing polish pass
- Finalize one animation preset for place-success and one for place-fail.
- Keep all critical state transitions mapped to cue banner + near-target feedback.
