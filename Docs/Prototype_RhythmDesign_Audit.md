# Prototype Rhythm Design Audit

Last updated: 2026-05-17 01:18 KST

## Verdict
The prototype has rhythm ingredients, but rhythm is not yet a first-class design
rule. The current loop already has timed waves, rest windows, Heat pressure,
events, unlock beats, combo timing, recipe durations, and payoff cues. Those are
good raw materials. The missing piece is an explicit cadence map that says when
the player should feel tension, variation, release, and renewed commitment.

Design decision: treat rhythm as the central difficulty and fun lens for the next
loop updates.

## Existing Rhythm Sources
| Source | Current behavior | Rhythm value |
| --- | --- | --- |
| Core loop | Draw 3 -> choose -> rotate/place -> survive wave -> read outcome | Gives a repeatable beat |
| Wave timer | Combat wave lasts 20 seconds | Creates a clear pressure pulse |
| Rest timer | Rest phase lasts 10 seconds after boss spikes | Gives release, but only on specific beats |
| Combo window | Combo window lasts 8 seconds | Creates short-term tempo pressure |
| Vent cooldown | Vent cooldown lasts 7 seconds | Makes Heat management rhythmic instead of constant |
| Heat bands | Warning at 70, overheat at 85 | Creates tension ramp and danger peaks |
| Events | Event every 3 waves | Adds variation/offbeat choices |
| Weather | Weather rotates every 4 waves | Adds rule variation |
| Boss/rest | Boss pressure every 5 waves with rest | Adds spike and relief |
| Unlocks | Wave 4 and 7 unlock targeting/shapes | Adds learning and tempo shifts |
| Recipes | Durations and payoff summaries | Adds delayed reward rhythm |
| Payoff cue | Wave result and recipe result summaries | Closes the beat with feedback |

## Weak Points
- The cadence exists as scattered constants and conditions, not as a visible beat
  map that guides tuning.
- Events, weather, boss pressure, and unlocks are periodic, but their collisions
  are not intentionally composed. A wave can become busy because schedules overlap,
  not because the rhythm asks for that spike.
- The player-facing state rhythm is still hard to read until Play Mode evidence
  confirms Draw Choice, Pending Placement, Invalid Placement, and Wave Combat.
- The game does not yet define a "breath" standard: how long the player gets to
  choose, commit, watch consequences, and recover.
- Music and SFX are mentioned as future resources, but there is no beat contract
  for combat pulse, Heat warning, overheat, combo, event resolve, or payoff.
- Metrics track useful events, but there is no explicit rhythm score such as time
  in each state, spike spacing, recovery spacing, or repeated high-tension overlap.

## 2026-05-17 Review Update
Sub-agent review confirms the original verdict. The current code has enough rhythm
material to build on, but Play Mode evidence is still too partial to validate the
felt rhythm. Do not add broad content until Draw Choice, Pending Placement, and
Invalid Placement are captured and judged.

Implementation priority after evidence closure:
1. `Wave Cadence Composer` source guard
2. `Payoff-to-Read Panel`
3. `Rhythm Beat HUD/Telemetry`
4. `Pressure Ramp Tuning`
5. `Rest-Phase Reward`

The first two are preferred because they use existing systems instead of adding
new surface area. They make the current loop easier to read and tune before
larger reward/content systems are introduced.

2026-05-17 source update: `Wave Cadence Composer` now exists in
`FoodTruckRunModel` as an inspectable `WaveCadencePlan`. It does not pre-roll
weather or event randomness; it only labels scheduled beats and planned spikes
for wave transitions. Play Mode still needs to confirm whether those planned
spikes are felt clearly.

2026-05-17 payoff update: `Payoff-to-Read Panel` now exists as a compact `Next`
chip beside the persistent wave payoff chip. It is not a second result banner;
it translates leaks, HP loss, Heat spikes, damage payoff, combo, or recovery into
a short next-decision hint for the following Read beat.

2026-05-17 rhythm beat update: `Rhythm Beat HUD/Telemetry` now has a first
source guard. The model resolves `Read`, `Commit`, `Pressure`, `Payoff`, and
`Release`; the HUD status line and UX telemetry reuse the same label. Telemetry
also tracks current beat duration and transition count.

2026-05-17 pressure ramp update: `Pressure Ramp Profile` now has a first source
guard. The model labels combat wave pressure as `Build`, `Climb`, or `Peak` and
exports an intensity value for HUD/telemetry review. It does not yet change spawn
or Heat balance.

2026-05-18 rest reward update: `Rest-Phase Reward` now has a first source guard.
Boss/rest release windows grant one small automatic reward from `Repair`,
`Cooling`, or `Stock` based on the completed wave's leaks, HP, Heat spike, KOs,
and combo actions. HUD and telemetry expose the reward so the Release beat can be
reviewed without asking the player for another choice while evidence capture is
still partial.

## Rhythm Pillars
| Pillar | Design rule |
| --- | --- |
| Pulse | The player should feel a steady loop: read, choose, commit, pressure, payoff. |
| Tension Envelope | Heat, threat, lane leaks, and wave timer should climb toward a readable peak. |
| Variation | Every few beats, a new rule or forced choice should bend the loop without hiding it. |
| Release | After a spike, the game should give a visible payoff or relief window. |
| Syncopation | Events, recipes, combo bursts, and overheat should feel like offbeats, not random noise. |
| Readability | A rhythm beat only counts if the player can see what changed and why. |

## Target Beat Map
| Beat | Target feel | Current support | Needed update |
| --- | --- | --- | --- |
| 1. Read | Compare three options and current pressure | Draw cards show Fit/Heat/Role | Verify readability and keep one safe/one greedy/one utility choice |
| 2. Commit | Place or recover from a mistake | Pending and invalid placement have guidance | Verify placement is fast enough and failure does not stall the loop |
| 3. Pressure | Watch the truck, zombies, Heat, and attacks | 20s wave, attack trails, combat labels | Tune spawn/Heat ramp so pressure rises instead of feeling flat |
| 4. Payoff | Understand why the last decision mattered | Wave payoff, recipe payoff, and Next hint cues | Keep payoff visible long enough to guide next choice |
| 5. Release/Variation | Breathe, then get a twist | Rest, events, weather, unlocks | Compose overlaps so spikes and relief are intentional |

## Wave Cadence Target
| Arc | Intent | Composer label |
| --- | --- | --- |
| Wave 1 | Establish the base tempo: simple lanes, low Heat, clear placement. | Steady combat |
| Wave 2 | Add pressure: a lane threat or Heat tradeoff becomes visible. | Steady combat |
| Wave 3 | Offbeat: event choice changes the next decision. | Event |
| Wave 4 | Tempo shift: targeting/shape unlock adds a new decision layer. | Unlock, Weather planned spike |
| Wave 5 | Spike and release: boss pressure, then rest/payoff. | Boss, Rest planned spike |
| Wave 6 | Recovery test: player uses the new tool under moderate pressure. | Event |
| Wave 7 | Second tempo shift: broader targeting/shape/risk variation. | Unlock |

## Update Backlog
### P0 Rhythm Criteria
- Add rhythm acceptance checks to Play Mode review: pressure ramp, variation, payoff,
  and release must be judged alongside readability.
- During focused retakes, judge whether Draw/Pending/Wave states feel like connected
  beats rather than separate screens.
- Add rhythm language to the next-step playbook so future feature work states which
  beat it improves.

### P1 System Updates
- Extend the wave cadence helper from source guard to tuning tool: use the
  planned-spike flag to decide whether an overlap should be emphasized, softened,
  or moved.
- Extend the Payoff-to-Read compact chip after Play Mode review if the current
  `Next` hint is too small, too crowded, or not visible during Draw/Pending.
- Use the rhythm beat duration/transition telemetry after visual review to tune
  whether `Read`, `Commit`, `Pressure`, `Payoff`, and `Release` are lingering or
  cutting off too quickly.
- Use the pressure ramp profile after visual review to decide whether spawn/Heat
  should follow the observed `Build -> Climb -> Peak` curve.
- Use the rest reward profile after visual review to decide whether `Repair`,
  `Cooling`, or `Stock` makes the Release beat feel earned without over-rewarding
  boss waves.
- Add rhythm telemetry: state duration, draw-to-place time, peak Heat timing,
  spike overlap count, and payoff-visible time.
- Add a review-pack rhythm section so screenshots and manual notes carry the same
  beat vocabulary.

### P2 Audio/Feel Updates
- Define SFX pulse roles: combat tick, Heat warning, overheat spike, combo window,
  recipe activation, event resolve, and payoff.
- Use music intensity or percussion layers to match Heat/threat bands.
- Keep audio variation tied to state changes, not just button presses.

## Acceptance
- The player can describe the current beat: choosing, committing, surviving,
  reading payoff, or recovering.
- At least one tension source rises before a spike, and at least one release follows
  after the spike.
- Variation appears every 2-3 waves, but not every variation source fires at once
  unless the wave is intentionally a spike.
- Payoff appears before the next choice and helps choose the next card or placement.
- The player can connect the `Next` hint to a concrete cause: leak, Heat spike,
  damage payoff, combo, or recovery.
- Heat/Threat/Combo/Recipe signals feel like layered rhythm, not unrelated meters.
- A `FIX_FEEDBACK` result can identify which beat failed: read, commit, pressure,
  payoff, or release.
