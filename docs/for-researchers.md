# For researchers (Cambridge / Yale standard)

What this system is and is not as a research object, stated so a
reviewer can decide what to cite.

## Citable constructs (implemented, tested, pinned)

- **Tie strength**: exponential decay, half-life 60 days, equal unit
  boosts — `TieDecayModel.StrengthAt`, 18 tests. Cite the equation in
  [methodology](methodology.md) §2 with its code pin.
- **Cadence reference**: median–prior blend with 3 pseudo-observations,
  clamped 3–180 days; persona priors 30/21/14 days. Cite §3.
- **Urgency**: max-normalized deficit, 0–100. A *ranking aid over one
  user's history* — cite as a heuristic, never as a validated measure.
- **Evidence grades** (`NoHistory`/`Insufficient`/`Established`) and the
  display band cap: built-in uncertainty labeling, citable as a design
  pattern for honest scoring UX.
- **Network construction**: shared-context edges with stated weights,
  ubiquity cutoff (>50), Tarjan articulation points, connected
  components. Deterministic and re-derivable by hand.
- **Separation architecture**: intention/state/interaction isolation
  with the exact influence matrix (which preference fields touch which
  outputs) — citable as a worked example of non-corrupting personalization.

## What cannot be cited as validated

- No ground truth, no inter-rater study, no predictive evaluation
  exists. Urgency has no demonstrated correlation with any external
  outcome. Bands are display conventions, not validated risk strata.
- Persona priors (14/21/30 days) are engineering judgments, not
  estimated parameters. The 60-day half-life is a chosen constant, not
  a fitted one.
- Copilot answers are LLM-generated over scoped data: citable as
  system behavior, not as evidence about the world.

## Using the system as infrastructure

Snapshots (per person per day), audit trails, and evidence-graded
memories are timestamped, versioned corpora suitable for secondary
analysis of tie decay, attention behavior, and recruiter workflows —
with per-user isolation preserved. See [reproducibility](reproducibility.md)
for reconstruction. Suitable for methods sections; insufficient alone
for substantive claims without independent validation.
