# Methodology — original method record

What the system computes, mapped to implemented code. Nothing here is
aspirational; every section names the code that implements it. Pins use
the repo root (`Core` = `RelationshipIntelligence.Core`). Line numbers
are omitted deliberately — method names are the stable reference.

Scope: this record covers the deterministic scoring core (strength,
rhythm, urgency, bands, evidence, network, events, reminders, AI
boundaries). Pipeline behavior (ingestion review, meeting logging,
outreach drafting, digest selection) lives in
[methodology](methodology.md) and [pipelines](pipelines.md).

## 1. Observed evidence

The only inputs treated as fact are **dated contact events**
(`Interaction`: Call, Email, Meeting, Message — title, optional
description, timestamp; `Core/Domain/Entities/Interaction.cs`) plus
user-authored context (memory entries, events). Shared-context
memberships (organizations, tags, channels) feed the network graph
only (§8) — never scores. Message content, sentiment, and the other
side's behavior are not observed and never enter any calculation.
`TieDecayModel.StrengthAt` takes only `eventTimesUtc` — there is no
parameter through which content could leak in.

## 2. Tie strength

`Core/Services/TieDecayModel.cs` → `StrengthAt`

Each event contributes an equal boost of 1.0 (`EqualBoost`), decayed
exponentially by its age:

$$s = \sum_i 1.0 \cdot e^{-\alpha \cdot \mathrm{age}_i}, \qquad \alpha = \frac{\ln 2}{60}$$

- **Variables**: $s$ = tie strength (unitless); $\mathrm{age}_i$ = days
  since event $i$ (UTC both ends, negative clamped to 0).
- **Parameters**: half-life 60 days (`DefaultHalfLifeDays`); $\alpha$
  derived, not tuned.
- **Verification**: `TieDecayModelTests` (decay math, half-life, equal
  boosts).
- **Interpretation**: recent, frequent contact scores high; old silence
  decays toward zero. Descriptive of the *record*, not a property of the
  person.
- **Non-claims**: not a probability, not a prediction, not causal. No
  per-channel weights, no per-person tuning, no hidden adjustments.

## 3. Cadence reference (rhythm)

`TieDecayModel.CadenceReference(gapsDays, priorDays)`

- With ≥3 observed gaps: `(n·median + 3·prior) / (n + 3)`, clamped to
  3–180 days — median blended with the persona prior as 3
  pseudo-observations.
- With <3 gaps: the persona prior alone (`PersonaPriors.DaysFor`:
  default 30d; recruiting/hiring → 14d; client → 21d), flagged
  estimated.
- The prior describes an expected rhythm; UI labels it "estimated, not
  measured" wherever shown.

## 4. Silence and its quantile

- **Silence** (`TieDecayModel.SilenceDays`): floor of elapsed UTC days
  since last contact; null when no contact, 0 for future timestamps.
  Single source of truth — the frontend mirrors it (`lib/format.ts`
  `daysSince`: floor, UTC both ends) instead of recomputing.
- **Silence quantile** (`SilenceQuantile`): fraction of past gaps ≤
  current silence; null when no gaps. Descriptive only ("longer than
  100% of past gaps").

## 5. Urgency and bands

- **Urgency** (`Urgency`): `U = 100·(1 − s/s_max)`, clamped 0–100,
  where $s_{max}$ is the user's own strongest tie. Urgency is the
  deficit against your strongest relationship — a ranking aid, not a
  probability.
- **Bands** (`BandFor`): Critical > 85, At Risk > 65, Drifting ≥ 40,
  Healthy otherwise. Display thresholds over urgency, not independent
  measurements.
- **Evidence cap** (`RelationshipScoringService.CapBandForEvidence`):
  display band capped at Drifting when evidence is Insufficient;
  urgency untouched — ordering stays correct while display stays
  honest.

## 6. Evidence grades

`EvidenceStatus` (`Core/Domain/Entities/EvidenceStatus.cs`):
`NoHistory` (no interactions — unscored, unranked, urgency 0),
`Insufficient` (<3 gaps — scored on priors, labeled "early estimate"),
`Established` (measured rhythm). Rows without history never enter
ranked surfaces (`RelationshipScoringService.GetQueueAsync` filters
`EvidenceStatus.NoHistory` before ordering).

## 7. User intent as constrained input (not a bypass)

Precedence: explicit per-person cadence (`DesiredCadenceDays`) >
global default (`DefaultCadenceDays`) > system inference. The global
default only fills gaps — it never overrides an explicit cadence
(`RelationshipScoringService.GetQueueAsync`, "Precedence chain"
comment).

Intent feeds display-level inputs only: urgency tie-break
(`PreferencePriority`), the `IsImportant` flag, and
`threshold = min(measured rhythm, user cadence)` for the event signal.
Scores, bands, silence, and primary urgency ordering stay model-owned
(same method, "constrains attention surfacing only" comment). Queue
rows label the source (`CadenceSourceLabel`: "You asked for every N
days" / "Your default" / "Usual rhythm").

## 8. Network terms (used precisely)

`Core/Services/NetworkAnalyzer.cs`

- Edges = **shared context** (same org +1.0, shared tags ≤+1.0,
  shared channel +0.5; attributes shared by more than
  `DefaultMaxSharedAttributeMembers` = 50 people ignored).
- Groups = **disconnected components**, not detected communities — no
  community-detection algorithm exists.
- Flagged nodes = **articulation points** (iterative Tarjan in
  `FindArticulationPoints`), labeled "Bridge" with tooltip. Structural
  flag, not a risk score.
- Deterministic: no random seeds, weights, or layout in the model.

## 9. Events and attention

Events never change scores. A queue row carries an event flag when an
event is ≤7 days away AND the relationship is already drifting
(urgency > 65 or silence past its rhythm). Contextual enrichment, not
a new score. (`RelationshipScoringService.EnrichWithUpcomingEventsAsync`;
test `GetQueueAsync_EventNearAndDrifting_SetsEventSignalWithoutChangingScore`
asserts scores are untouched.)

## 10. Reminders are intent, not evidence

Reminder state (`ReminderState`: Disabled/Idle/Due/Snoozed/Skipped/
Completed) is derived from stored fields, never stored separately
(`RelationshipPreferenceService.GetReminderStateAsync`). Viewing,
snoozing, skipping, or dismissing never creates an interaction;
completion happens only via `InteractionService.LogAsync`, the sole
writer of `LastCompletedAtUtc`.

## 11. AI boundaries

The Copilot interprets, summarizes, extracts, explains, and drafts. It
never computes scores, bands, urgency, cadence, or network flags;
those remain deterministic. AI output that becomes durable always
passes explicit user confirmation with provenance recorded (User /
AiSuggested / AiConfirmed / MeetingDerived / IngestionDerived).
(`CopilotPrompts.cs` rules 2–4: never present inference as fact, never
recompute deterministic intelligence, user-confirmed memory outranks
everything.)
