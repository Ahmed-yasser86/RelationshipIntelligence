# Relationship Intelligence - Methodology & Implementation (Single Source of Truth)

**Status:** living document. Every scientific, statistical, or modeling decision in this
repository MUST be described here before or alongside the code that implements it.
Undocumented methodology changes are not permitted.

**Audience:** written for independent evaluation by a Computational Social Science
researcher. A reader with no access to the authors should be able to reproduce the
reasoning, re-derive the quantities, and distinguish established science from our
engineering choices.

**Scope:** R0 (stabilization) then R1, R2, R3, R4 (temporal write API, decay scoring,
digest habit loop, network topology). Macro/macro-synthesis work (Context Engine,
MacroScope feeds, cause attribution, simulation) is explicitly deferred and NOT
covered here beyond the deferral rationale in Sec.11.

**Notation:** all mathematics in this document uses plain ASCII
(`alpha`, `exp()`, `->`, `>=`, `*`) so the file renders identically everywhere.
No Unicode math symbols are used.

---

## Table of contents

1. Problem statement
2. Architecture (Clean Architecture extension, no CQRS, no new databases)
3. Data model & definitions
4. User-data isolation (security boundary)
5. Scientific methodology
   - 5.1 Literature review (established constructs)
   - 5.2 Rejected approaches (SM-2 / FSRS / invented curves) and why
   - 5.3 Adopted core: tie-decay networks (established model)
   - 5.4 Established covariates (Burt; flags only in v1)
   - 5.5 Our engineering operationalization (explicitly labeled)
   - 5.6 Cold-start priors - assumptions, not findings
   - 5.7 Presentation layer: bands, urgency (not science)
6. Exact specification (variables, parameters, equations)
7. Limitations & unobserved variables
8. Validation strategy (from heuristic to survival analysis)
9. Testing strategy (TDD + isolation)
10. Phased implementation plan (R0 -> R1 -> R2 -> R3 -> R4)
11. Deferred work & non-goals
12. Decision log
13. References

---

## 1. Problem statement

Solo professional operators (recruiters, freelancers, consultants, founders) lose
economically valuable relationships through inattention, not intent. Existing tools
(CRM, LinkedIn, spreadsheets) record *who* is known but never answer *who is
cooling, how urgently, and what to do this week*.

Our product problem, precisely: given **passive observational data** (a log of
past contact events per relationship, plus coarse contact attributes), produce a
**per-relationship, interpretable, personalized risk ranking** that tells the user
which 5-7 relationships need attention this week.

Data constraints (these drive every methodology choice):

- (a) Irregular interaction intervals - no fixed schedule exists per pair.
- (b) Sparse observations - most pairs have 0-5 recorded events.
- (c) No explicit user ratings - nobody grades relationships 0-5.
- (d) Per-pair temporal history only - one ego's view, small-n per dyad.
- (e) Outputs must be interpretable in two lines of UI text.
- (f) The method must be implementable in plain C# + SQL Server with no new
      infrastructure, and must evolve toward rigorous statistical modeling later
      without schema breakage.

---

## 2. Architecture

### 2.1 Foundation: extend the existing Clean Architecture

The repository is layered as:

```
Angular client (future) + external consumers
  -> RelationshipIntelligence.Api (JWT, ContactsController/AccountController)
  -> RelationshipIntelligence.Core (entities, DTOs, service contracts, services)
  -> RelationshipIntelligence.Infrastructure (EF Core + SQL Server repositories, AppDBContext, UnitOfWork)
```

Dependency direction is inward (API -> Core abstractions -> Infrastructure
implements Core contracts). New Relationship Intelligence functionality follows the
same pattern in the same projects:

- New entities: `RelationshipIntelligence.Core/Domain/Entities/`.
- New DTOs: `RelationshipIntelligence.Core/DTOs/` (DataAnnotations validation,
  `ToPerson()`-style converters, matching existing `PersonAddRequest` patterns).
- New contracts: `RelationshipIntelligence.Core/ServiceContracts/` + `Domain/RepositryContracts/`.
- New services: `RelationshipIntelligence.Core/Services/`, one concern per class,
  `ILogger<T>` + `SerilogTimings` + `ValidationHelpers.ValidationFunction`,
  matching `PersonAdderService` / `PersonGetterService`.
- New repositories: `RelationshipIntelligence.Infrastructure/Repositries/`
  (attach/track only; commit exclusively through `IUnitOfWork` - see Sec.2.3).
- DI: `RelationshipIntelligence.Api/Program.cs`.
- Pure computational kernels (decay math, graph analysis) live in Core as
  dependency-free classes so they are unit-testable without EF, HTTP, or time.

Coding rules for all new code: follow the existing style (naming, per-operation
services, DataAnnotations validation, SerilogTimings operation timing, xUnit + Moq
+ AutoFixture + FluentAssertions tests). Additionally: one public type per file -
request/response DTOs live under `Core/DTOs/<Area>DTOs/`, never inside controllers
or service contracts. Extend; do not redesign. No new
architectural style is introduced without a recorded justification in Sec.12.

### 2.2 Explicitly not introduced

No CQRS (read/write asymmetry does not justify it; a materialized
`RelationshipState` read-projection gives the benefit), no graph/document/vector
database (per-user ego graphs are hundreds to thousands of nodes; SQL adjacency +
in-memory analysis suffices), no event bus (direct service calls suffice until a
second event producer exists). Sec.11 records the revisit conditions.

### 2.3 UnitOfWork consistency rule

`IUnitOfWork` exposes exactly one operation: `SaveChangesAsync()`. The rule,
enforced by review and tests:

- Repositories only attach and track entities. They never call SaveChanges.
- Each application-service use case performs exactly one `SaveChangesAsync()`
  call through `IUnitOfWork` when its unit of work is complete.
- A single EF Core SaveChanges call is atomic; no explicit transaction API is
  exposed until a cross-aggregate use case genuinely requires one (recorded in
  Sec.12 if it ever happens).
- `PersonRepositryContract.UpdatePerson` (merge-style) is marked `[Obsolete]`:
  update flows mutate the tracked entity loaded via `GetPersonById` and commit
  through `IUnitOfWork`. The obsolete member is retained for compatibility only.

---

## 3. Data model & definitions

### 3.1 Ownership

`Person.ApplicationUserId (Guid, required, FK -> AspNetUsers.Id, on-delete Restrict)`.
Every row belongs to exactly one user. Reads are additionally constrained by a
global query filter (`ApplicationUserId == current user AND NOT IsDeleted`); writes
stamp the id server-side from `ICurrentUserService` and throw
`UnauthorizedAccessException` without an authenticated user. No backfill was needed:
ownership predates this work.

### 3.2 Interaction - the behavioral fact table

`Interaction { InteractionId, InteractionType (Call|Email|Meeting|Message),
InteractionTitle (required, <=100 chars), InteractionDescription?,
TimeOfInteraction (required), PersonId FK -> Person }` - one row per contact event
per person (1:N). R1 adds the service/repository write path through `IUnitOfWork`,
UTC normalization of `TimeOfInteraction`, no-future-date validation, and CSV import.
`Note` (content record, no timestamp) is deliberately NOT a scoring input in v1:
notes record what was said, not that contact occurred.

### 3.3 RelationshipState (R2) - materialized read projection, not raw data

`RelationshipState { RelationshipStateId, ApplicationUserId, PersonId, TieStrength,
LastContactAtUtc?, CadenceReferenceDays?, SilenceQuantile?, IsBridge,
UrgencyScore, UpdatedAtUtc }`, unique `(ApplicationUserId, PersonId)`. Recomputed
nightly plus instant per-pair recalc on write. Digest and graph endpoints read this
table; they never compute on request.

### 3.4 Digest records (R3)

`DigestDelivery { DigestDeliveryId, ApplicationUserId, WeekStartUtc, PersonIdsJson,
CreatedAtUtc }` (idempotency key `(ApplicationUserId, WeekStartUtc)`);
`DigestMetric { DigestMetricId, DeliveryId, ApplicationUserId, PersonId, Opened,
ActionTaken, ActionType? }` - the labeled outcomes that power Sec.8 validation.
Analytics rows are never mixed with contact data.

### 3.5 Importance and exclusion without new columns (v1)

The v1 schema adds no `IsKey`/`IsExcluded` columns. Instead it reuses observable
user input that already exists:

- Importance: `SystemStatusTag` membership in `HighPriority` or `Urgent`
  (user-assigned, seeded reference data).
- Exclusion from scoring: `UserDefinedTags` membership in a tag named `NoScore`
  (case-insensitive; documented in digest settings copy).

Both are transparent, reversible, and require zero migration. Dedicated columns
are reconsidered only if outcome data shows the convention is misused (Sec.8).

---

## 4. User-data isolation (security boundary)

**Rule: a user must never retrieve, modify, score, or analyze another user's data,
even under a malicious or malformed request. Controllers are never the sole
security boundary.**

Enforcement on this codebase (three layers):

1. `ICurrentUserService.UserId (Guid?)` resolves the principal server-side from the
   JWT `NameIdentifier` claim (which carries the user id; email travels in a
   dedicated `Email` claim consumed by logout). Null means unauthenticated.
2. `AppDBContext` applies a global query filter on `Person`
   (`ApplicationUserId == current user AND NOT IsDeleted`), so all Person reads -
   including lists, searches, and batched grids - are owner-scoped with no
   per-call parameters. Writes stamp `ApplicationUserId` from `ICurrentUserService`
   and throw `UnauthorizedAccessException` without one.
3. `PersonOwnershipFilter` (registered in DI, applied to per-id endpoints)
   returns 401/403 for foreign ids; controllers map missing entities to 404
   instead of leaking existence.

Covered by `RelationshipIntelligence.Tests/OwnerIsolationTests.cs` (JWT claim
contents, attach + stamping, fail-closed service paths),
`PersonOwnershipIsolationTests.cs` (real SQLite-backed EF tests proving the global
filter on get/list/filter/delete plus unauthenticated emptiness), and
`RelationshipIntelligence.ControllerTests/PersonOwnershipFilterTests.cs`
(401/403/passthrough + ValidatedPerson handoff).

---

## 5. Scientific methodology

### 5.1 Literature review - established constructs we operationalize

1. **Tie strength as a construct (Granovetter 1973, AJS).** Strength is "a
   (probably linear) combination of the amount of time, the emotional intensity,
   the intimacy (mutual confiding), and the reciprocal services which characterize
   the tie." *Status:* conceptual definition, deliberately not a formula. We adopt
   the construct; we do not claim Granovetter endorsed any equation herein.
2. **Measuring tie strength (Marsden & Campbell 1984, Social Forces).** From survey
   data on friendship ties: **closeness is the best indicator of strength;
   contact frequency and duration are secondary and partly confounded.**
   *Consequence for us:* frequency/recency are legitimate but imperfect predictors;
   closeness/intimacy is unobserved in our data (Sec.7). Any frequency-based measure
   must be presented as a behavioral proxy, not as tie strength itself.
3. **Predicting tie strength from behavioral data (Gilbert & Karahalios 2009,
   CHI; 2,000+ ties, >85% accuracy).** Frequency, recency, reciprocity,
   multiplexity, and intimacy signals jointly predict self-reported strength.
   *Consequence:* validates our predictor set (frequency, recency, channel
   variety); provides no decay equation and no channel weights - so v1 uses
   equal weights (Sec.6.2).
4. **Tie decay and persistence (Burt 2000 "Decay functions," Social Networks;
   Burt 2002 "Bridge decay").** Two robust findings: (i) **decay probability
   decreases with tie age** (older ties are hardier); (ii) **bridges decay faster
   than embedded ties**. *Consequence:* tie age and bridge status are recorded and
   displayed in v1, but enter NO equation until fitted coefficients exist (Sec.5.4).
5. **Temporal predictors of persistence (Raeder et al. / Hidalgo stream, EPJ Data
   Science 2017, 19-month call data).** Persistence is predicted by intensity,
   embeddedness (topological overlap), and temporal distribution - **burstiness
   predicts decay**. Standard fit: logistic persistence
   `P(persist) = 1 / (1 + exp(-beta.x))`. *Consequence:* this logistic framing is our
   validation target (Sec.8), not our v1 implementation.
6. **Tie-decay networks (Ahmad, Porter, Beguerisse-Diaz 2018-2021; Grindrod et
   al.).** Formalism that **separates interactions (discrete events) from ties
   (continuous strengths): tie strength decays exponentially between interactions
   and is boosted discretely on each interaction.** Peer-reviewed, mathematically
   specified, and exactly our data shape. *Consequence:* this is our adopted v1
   core (Sec.5.3) - the only "established model" claim in this document.
7. **Survival / event-history analysis (de Nooy 2011; Dean, Bauer & Prinstein:
   discrete-time multilevel survival for friendship dissolution; joint
   longitudinal + time-to-event models).** The rigorous framework for whether
   *and when* ties dissolve. Requires longitudinal outcome data we do not yet
   have. *Consequence:* v1 is designed to emit valid inputs for this later stage
   (per-pair histories + labeled outcomes), but survival modeling is not v1.
8. **Relational maintenance (Stafford & Canary 1991; Canary et al.).** Maintenance
   strategies must be enacted continuously; effects diminish over time.
   *Consequence:* behavioral justification for monitoring cadence at all; no
   equation taken.

### 5.2 Rejected approaches

- **SM-2 / FSRS (spaced repetition, see spacedrepitationalgorithm.md).** Require
  explicit difficulty grades and scheduler-controlled intervals. Our setting has
  neither (passive observation, no grades, no control over the counterparty).
  FSRS's Retrievability/Stability distinction inspired separating current state
  from baseline, but the algorithms themselves are inapplicable. Rejected as core;
  not cited as basis.
- **Invented saturating curves** (e.g. any `100*(1-exp(-lambda*(r-1)))` with chosen
  lambda, fixed day constants, persona priors, score bands, key multipliers).
  Rejected in prior review: mathematical elegance and interpretability do not
  constitute academic justification. None ship as science; bands and ranking rules
  in the UI are labeled presentation layer (Sec.5.7).

### 5.3 Adopted core - exponential tie-decay with equal event boosts (established)

For each `(owner, person)` pair with ordered event times `t1 < t2 < ... <= now`
taken from `TimeOfInteraction`:

```
s(t)    = s(t_i+) * exp(-alpha * (t - t_i)),   for t_i < t <= t_{i+1}
s(t_i+) = s(t_i-) + 1                           (equal boost on each event)
```

- *Basis:* Ahmad et al. tie-decay formalism (Sec.5.1.6). Established; we claim only
  the dynamic, not any parameterization.
- *Why it fits (Sec.1 constraints):* handles irregular intervals natively (gaps are
  continuous); works with any n >= 1 (no fitting per pair); purely observational
  (no grades); dependency-free computation; outputs feed survival models later.
- *Conservative v1 restriction (binding):* one decay rate `alpha`, one equal boost
  `b = 1.0` for every `InteractionType`. No per-channel weights, no tie-age term,
  no bridge term enter `s(t)`. Rationale: Gilbert validates the predictor set but
  no weights; Burt gives directions but no magnitudes fittable to our data. Any
  differentiation waits for Sec.8 evidence.

### 5.4 Established covariates (Burt; flags only in v1)

- **Tie age:** older ties decay slower (Burt 2000). V1 records tie age (first
  observed `TimeOfInteraction`) and *displays* it as context alongside the score.
  It does NOT enter the decay dynamic until fitted coefficients exist (Sec.8).
- **Embeddedness / bridges:** bridges are higher-risk (Burt 2002). V1 computes
  bridge status (Sec.6.5) and surfaces it as a *flag* ("connects two parts of
  your network"), with no numeric effect on scores until validated.

### 5.5 Our engineering operationalization (explicitly NOT established science)

The following are our choices, necessitated by sparse passive data, and are labeled
`[OPERATIONALIZATION]` wherever they appear in code/docs:

- Median-gap cadence *reference* (descriptive context only, never a fitted model).
- Cold-start priors and minimum-observation rules.
- Ego-graph edge rules (shared Circle / shared tag / shared channel / co-occurrence).
- Urgency ranking composition (deficit + observable flags + tie-breaks).
- Health bands, digest selection rules, `NoScore` tag convention.
- Each is documented with: what it is, why the data forces it, what literature
  (if any) constrains it, and how it will be validated or replaced (Sec.8).

### 5.6 Cold-start priors - assumptions, not findings

No literature prescribes per-role contact cadences, and our schema has no
sector/role columns. V1 priors key on observable text with a single default:

- `ContactItemRole.Role` containing "recruit"/"hiring" (case-insensitive): 14 days.
- `ContactItemRole.Role` containing "client": 21 days.
- Default (anything else, or fewer than 3 observed gaps): 30 days.

These are transparent placeholders in one table (`PersonaPriors`), used only until
personal history exists, recalibrated from observed gaps once volume permits, and
presented only as "estimated rhythm," never as fact.

### 5.7 Presentation layer - bands, urgency (not science)

Health bands (Healthy/Drifting/At Risk/Critical) and urgency ordering are
ranking/display conventions for triage, tuned by future outcome data - not
constructs with literature status. Code and UI copy must never imply otherwise.

---

## 6. Exact specification

### 6.1 Time and inputs

- All timestamps UTC. `TimeOfInteraction` values are normalized to UTC on write;
  future dates (beyond now + 1 day tolerance) are rejected. "Now" is
  `DateTime.UtcNow` at computation time.
- Per-pair input: ordered `TimeOfInteraction` values + `InteractionType` values
  (types do not affect v1 scoring; they are stored for display and for future
  fitted models) + optional tie-start (first observed event).
- Cold-start table `PersonaPriors` (Sec.5.6).

### 6.2 Tie-decay parameters (engineering choices, sensitivity-tested)

| Parameter | V1 value | Definition | Why this value |
|---|---|---|---|
| `alpha` (decay rate) | `ln(2)/60 = 0.01155 per day` | Exponential rate; half-life 60 days | Central assumption: a professional tie with zero contact halves in about 2 months. Chosen as a round, reviewable middle of a 30/60/90-day sensitivity grid (tests assert ordering across all three; product ships 60). NOT a literature constant. |
| `b` (event boost) | `1.0` for every event | Additive strength on each logged interaction | Equal weights because no weights are established for our data (Gilbert supports the predictor set, not magnitudes). Any differentiation requires Sec.8 evidence. |
| `TieStrength` scale | open-ended, >= 0 | `s(now)` per Sec.5.3 | Comparable within a user, not across users. Displayed per-user normalized (min-max over the user's pairs), labeled as a within-network aid. |

*Worked example:* events 40d and 10d ago, alpha as above:
`s = exp(-0.01155*40) + exp(-0.01155*10) = 0.63 + 0.89 = 1.52`.
Thirty silent more days: `1.52 * exp(-0.01155*30) = 1.07`.

### 6.3 Tie age (displayed context; no score effect in v1)

Tie age = now minus first observed `TimeOfInteraction`, shown next to the score
("in your network 2.3 years; quiet for 49 days"). Directional prior from Burt
2000 is acknowledged in copy ("older ties usually weather silence better") but no
number enters any equation. A dampening coefficient may be proposed only after
Sec.8 fitting produces one from our own outcome data.

### 6.4 Bridge status (displayed flag; no score effect in v1)

`IsBridge` per Sec.6.5. Effect: flagged in queue, digest, and graph ("bridge -
connects two parts of your network; bridges are lost faster (Burt 2002)"). No
urgency points are added in v1. A bounded adjustment may be proposed only after
Sec.8 evidence.

### 6.5 Ego-graph construction (SNA-lite; standard measures only)

- Nodes: the user's contacts (`Person` rows visible through the query filter).
- Edges (all same-user, undirected, `[OPERATIONALIZATION]`):
  shared `Circle` (weight 1.0), shared `UserDefinedTags` entry (0.5 each, cap 1.0),
  shared `ConnectionChannel` (0.5), co-occurrence of two persons in interactions
  with equal timestamps where supported (1.0; reserved until multi-attendee
  events exist - v1 passes no co-occurrence groups).
- Measures (standard): **degree centrality** and **articulation points** via
  Tarjan's algorithm; **components** shown as clusters (structural holes described,
  no constraint/effective-size statistics claimed until validated).
- Betweenness reserved for later (cost + interpretability); degree + articulation
  cover the v1 questions ("load-bearing?", "what connects clusters?").

### 6.6 Cadence reference (descriptive context only)

- With >= 3 observed gaps: reference = median(gaps), shrunk once toward prior:
  `ref = (n*median + 3*prior) / (n+3)`, clamped to [3, 180] days. Shrinkage
  constant 3 = standard weak-prior pseudo-count, disclosed as choice.
- With < 3 gaps: prior from Sec.5.6. **Assumptions, not findings.**
- Displayed as "roughly every X days (estimated)" plus silence context and the
  silence quantile (fraction of own past gaps at or below the current silence).
  Never an input to the decay dynamic - the dynamic uses raw event times
  (Sec.5.3), so the reference cannot corrupt scoring; it only contextualizes.

### 6.7 Urgency ranking (presentation layer, explicitly not science)

```
urgency = 100 * (1 - s(now) / sMaxUser)     # deficit vs user's strongest tie
```

- `sMaxUser` = max strength over the user's scored pairs (fallback 1.0).
- Ordering: urgency descending; ties broken by HighPriority/Urgent tag membership,
  then by longest current silence. Contacts tagged `NoScore` are skipped; in digest
  context, contacts touched within 7 days are suppressed.
- Bands on urgency: <40 Healthy, 40-65 Drifting, 66-85 At Risk, >85 Critical -
  UI thresholds for triage, recalibrated from outcome data (Sec.8).

---

## 7. Limitations & unobserved variables

1. Closeness/intimacy (Marsden & Campbell's strongest indicator) is unobserved -
   our strength is a behavioral proxy.
2. One-sided observation: counterparty-side events (their outreach elsewhere,
   job changes) are invisible until enrichment arrives.
3. No sentiment/content: a dispute and a deal-closing look identical as events.
   `InteractionTitle`/`InteractionDescription` are stored for human context and
   future models, never scored in v1.
4. Small-n per pair: all per-pair quantities are high-variance; shrinkage and
   priors mitigate but do not remove this.
5. Bursty professional rhythms (project crunches) distort raw gaps; the digest
   suppression window and per-user recalibration (Sec.8) are the mitigations.
6. `InteractionType` plays no scoring role in v1 by design (equal boosts); any
   future differentiation must survive the Sec.8 falsification test.
7. No causal claims: macro-vs-personal attribution is deferred (Sec.11); v1 never
   states *why* a tie cooled.

---

## 8. Validation strategy (heuristic -> evidence)

- **V1 (now):** unit tests pin the dynamic's mathematical properties
  (monotonic decay, boost additivity, half-life identity `s(t_half) = s0/2`,
  quantile behavior); sensitivity tests assert ranking stability across
  alpha in {30, 60, 90}-day half-lives; dogfood check - "did it surface a
  genuinely forgotten contact?"
- **V2 (with digest outcome data):** fit Raeder-style logistic persistence
  `P(persist) = 1 / (1 + exp(-beta.x))` on features {strength, recency,
  frequency, burstiness, embeddedness, tie age}; report calibration + AUC vs.
  recency-only and frequency-only baselines. Recalibrate bands, priors, and -
  only if justified - channel weights and covariate coefficients from fitted
  values. This is the first empirical claim we will be entitled to make.
- **V3 (longitudinal):** discrete-time multilevel survival (Dean/de Nooy framing)
  for time-to-dissolution; joint models only if content signals arrive.
- **Falsification:** if fitted models do not beat recency-only baselines, the
  scoring layer is simplified, not defended. If channel weights do not improve
  fit, equal boosts stay permanently.

---

## 9. Testing strategy (TDD + isolation)

Tests precede or accompany code, mirroring existing infrastructure
(xUnit + Moq + AutoFixture + FluentAssertions; service/contract mocking as in
`RelationshipIntelligence.Tests/PersonServicesTest.cs`; controller/filter tests in
`RelationshipIntelligence.ControllerTests`; real-EF tests on SQLite in-memory):

- Pure-kernel tests (no I/O): decay monotonicity, boost additivity, half-life
  identity, quantile behavior, graph fixtures (two clusters + bridge -> flagged;
  isolates listed; empty/invalid tags -> no edges, no crash).
- Service tests: validation, ownership stamping, fail-closed cross-user paths.
- Repository tests (SQLite): two-owner seeded data; isolation on get/list/filter/
  delete; unauthenticated emptiness.
- Filter tests: 401/403/passthrough + `ValidatedPerson` handoff.
- Integration tests (when the harness exists): second principal -> 404 on foreign
  ids; digest signed-URL valid/expired/tampered cases; job idempotency
  (double-run -> same rows, one email).
- Sensitivity tests: alpha-grid ranking stability.

---

## 10. Phased implementation plan

### R0 - Stabilization & security (prerequisite; substantially complete)

JWT `NameIdentifier` carries the user id (email in `Email` claim); QuickAdd and
CountryAdd attach through repositories; `PersonOwnershipFilter` registered and
applied to per-id endpoints with 404 mapping; `IUnitOfWork` contract documented
(`IUnitOfWorkcs.cs` renamed to `IUnitOfWork.cs`; dead merge-update path marked
`[Obsolete]`); isolation suites (mock service-level, SQLite repository-level,
filter-level); CI repaired; methodology + README aligned. Remaining R0 close-out:
this document revision, README path fixes, `dotnet format`, full suite, commit.

### R1 - Temporal write API

`InteractionRepositoryContract` + `InteractionRepository` (attach-only, UoW commit
via service), `IInteractionService` (log/list/CSV import, max 500 rows with
line-numbered errors; UTC normalization; no-future-date rule; ownership verified
by scoped person fetch), controller endpoints on `ContactsController`
(`GetInteractionsForContact`, `PostLogInteraction`, `PostImportInteractions`;
`ArgumentException` maps to 404, validation failures to 400; per-id reads carry
`PersonOwnershipFilter`), DI registration. Reuses the existing
`InteractionResponse`/`ConvertToDto` (extended with `PersonId`) instead of a
parallel DTO.
*Depends on:* R0 ownership green. *Done when:* log->timeline round-trip, CSV
import, isolation on new paths, old suite green.

### R2 - Decay scoring + queue

Pure `TieDecayModel` (Sec.5.3/Sec.6.2) + `IRelationshipScoringService` writing
`RelationshipState` (nightly `IHostedService` + instant per-pair recalc on log) +
queue endpoint + bridge flags (display-only) + band filter. *Depends on:* R1 event
streams. *Done when:* dogfood surprise check, queue live, math-property +
sensitivity + idempotency tests green, single EF migration applied.

### R3 - Digest habit loop

`IDigestService` top-N selector (urgency order, `NoScore` exclusion, 7-day
suppression, never pad), `IEmailSender` (file/log default; SMTP is deployment
config), HMAC-signed one-click actions (7-day expiry; login-free `Interaction`
creation), digest settings + `DigestDelivery`/`DigestMetric` tables. *Depends on:*
R2 scores. *Done when:* email + JSON payload from one build, one-click creates an
interaction, metrics recorded. *Gate (>60% open) before any macro work.*

### R4 - SNA-lite topology

`INetworkAnalyzer` (edge rules Sec.6.5, degree + Tarjan articulation, components),
cached per user, `GET /api/network/graph` JSON (nodes, edges, bridges, clusters,
isolates). *Depends on:* R2 states (R3 optional). *Done when:* fixture graphs
correct, perf guard (<500 nodes, server <2s), endpoint isolated.

---

## 11. Deferred work & non-goals

MacroScope feeds (GDELT/EWS/CPCI/country risk), Context Engine (cause attribution,
timing predictor, correlation learning), agent simulation, Career/Lifecycle/
Flight-Risk/Account-Temperature engines, Gmail/Calendar sync (enum values reserved
only in a future `InteractionSource` column), LinkedIn/WhatsApp, team model, public
API versioning beyond current setup, CQRS, new databases, event bus. Revisit only
on: retention gate passed (macro), 100k+ users or real-time graph load
(CQRS/new stores), second event producer proven (events).

---

## 12. Decision log

- Sec.12.1 (unification): work branch is `relationship-intelligence-main`, based on
  `origin/ay/frontend-login-test` plus master's docs. Stale-base WIP on
  `registration-implementation` is reference only and is never merged; its
  decay/digest/SNA pieces are re-applied onto this base in R1-R4.
- Sec.12.2 (Sec.A correction): prior invented formula and constants withdrawn in full;
  tie-decay dynamic adopted as only established-model claim.
- Sec.12.3 (R0 fixes): JWT claim mismatch, QuickAdd and CountryAdd no-op creates,
  ownership filter wiring with 404 mapping, updater-test/UoW mismatch, CI project
  references, committed build artifacts, corrupt UTF-16 `.gitignore` - all corrected
  with covering tests. Controller/Program wiring was once lost in the folder-rename
  shuffle and is re-applied and verified in R0 close-out.
- Sec.12.4 (UoW consistency): repositories attach/track only; services commit once
  via `IUnitOfWork`; single SaveChanges is the atomicity boundary; dead
  merge-update path marked `[Obsolete]`; contract file renamed to `IUnitOfWork.cs`.
- Sec.12.5 (conservative v1): equal boosts for all `InteractionType`s; tie age and
  bridge status displayed but unscored; urgency is pure deficit with observable
  tie-breaks; importance/exclusion reuse `SystemStatusTag`/`UserDefinedTags`
  instead of new columns. Any numeric differentiation requires Sec.8 evidence.
- Sec.12.6 (email transport): `IEmailSender` ships with a file/log implementation;
  SMTP credentials are deployment config, never code.
- Sec.12.7 (N3/N4 scope notes): v1 `Interaction` rows carry a single `PersonId`, so
  the co-occurrence edge rule in Sec.6.5 is implemented in the pure `NetworkAnalyzer`
  kernel and covered by fixtures, but the service passes no co-occurrence groups
  until multi-attendee events exist. The digest "remind" action records an intent
  metric; standalone reminder scheduling is deferred. Neither affects scoring.
- Sec.12.8 (migrations): new tables (`RelationshipState`, digest records) ship via
  a single EF migration generated at the end of R2 (`20260911144337_RelationshipState`
  covers the states table); operator applies at deploy time.
- Sec.12.9 (R1/R2 implementation notes): R1 reuses the existing
  `InteractionResponse`/`ConvertToDto` (extended with `PersonId`) and maps
  `ArgumentException` to 404 / validation failures to 400 on new endpoints. R2's
  nightly worker composes repositories and the scoring service manually per owner
  (a `FixedUserService` carrying the enumerated owner id) because there is no
  HTTP request principal in background scope; `RecomputeForOwnerAsync` is the
  internal privileged path and is never called from controllers. `Note` stays
  out of v1 scoring (content record, no timestamp).
- Sec.12.10 (R3/R4 implementation notes): anonymous digest links authenticate by
  HMAC, so `HandleActionAsync` reads the person through
  `GetPersonByIdIgnoringFilters` and then enforces `person.ApplicationUserId ==
  token ownerId` explicitly - the only sanctioned filter bypass, documented on
  the contract. Digest and graph endpoints follow the action-based routing of
  `ContactsController` (`api/Digest/DigestAction`, `api/Network/GetNetworkGraph`).
  `NetworkAnalyzer` accepts co-occurrence groups but R4 passes none (single-person
  events); tag edges adapt the `UserDefinedTags` string list to JSON at the
  service boundary. `IsBridge` persists to `RelationshipState` so the queue can
  display it without recomputing the graph.
- Sec.12.11 (file organization): one public type per file for all new code;
  request/response DTOs live under `Core/DTOs/<Area>DTOs/`, never inside
  controllers or service contracts. Supporting records (graph inputs, CSV rows,
  kernel enums) each get their own file. Pre-existing multi-type files
  (`RelatedEntityResponse.cs` and similar) are left untouched.
- Sec.12.12 (history): commit messages never reference internal plan-step labels;
  history on this branch uses feature-based messages only.
  `InteractionResponse`/`ConvertToDto` (extended with `PersonId`) and maps
  `ArgumentException` to 404 / validation failures to 400 on new endpoints. R2's
  nightly worker composes repositories and the scoring service manually per owner
  (a `FixedUserService` carrying the enumerated owner id) because there is no
  HTTP request principal in background scope; `RecomputeForOwnerAsync` is the
  internal privileged path and is never called from controllers. `Note` stays
  out of v1 scoring (content record, no timestamp).

---

## 13. References

- Granovetter, M. (1973). The strength of weak ties. *AJS*, 78(6).
- Marsden, P. & Campbell, K. (1984). Measuring tie strength. *Social Forces*, 63(2).
- Gilbert, E. & Karahalios, K. (2009). Predicting tie strength with social media. *CHI*.
- Burt, R. (2000). Decay functions. *Social Networks*, 22(1).
- Burt, R. (2002). Bridge decay. *Social Networks*, 24(4).
- Raeder, T. et al. / Hidalgo, C. stream (2017). Temporal patterns behind the
  strength of persistent ties. *EPJ Data Science*.
- Ahmad, W., Porter, M., Beguerisse-Diaz, M. (2018-2021). Tie-decay networks in
  continuous time and eigenvector-based centralities (+ follow-ups: opinion
  dynamics on tie-decay networks; continuous-time tie-decay models).
- de Nooy, W. (2011); Dean, D., Bauer, D. & Prinstein, M. - multilevel/event-history
  models for friendship dissolution.
- Stafford, L. & Canary, D. (1991). Maintenance strategies and romantic
  relationship type... *JSPR*, 8. Plus Canary-Stafford program (1992-2011).
- Stafford, L. (2011). New typology of maintenance strategies.
