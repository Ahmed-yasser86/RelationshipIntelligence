# Relationship Intelligence  -  Methodology & Implementation (Single Source of Truth)

**Status:** living document. Every scientific, statistical, or modeling decision in this
repository MUST be described here before or alongside the code that implements it.
Undocumented methodology changes are not permitted.

**Audience:** written for independent evaluation by a Computational Social Science
researcher. A reader with no access to the authors should be able to reproduce the
reasoning, re-derive the quantities, and distinguish established science from our
engineering choices.

**Scope:** Phases 0, N1, N2, N3, N4 (foundation -> temporal data -> decay scoring ->
digest habit loop -> network topology). Macro/macro-synthesis work (Context Engine,
MacroScope feeds, cause attribution, simulation) is explicitly deferred and NOT
covered here beyond the deferral rationale in Sec.11.

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
   - 5.4 Established covariates (Burt: tie age, embeddedness/bridges)
   - 5.5 Our engineering operationalization (explicitly labeled)
   - 5.6 Cold-start priors (assumptions, not findings)
   - 5.7 Presentation layer: bands, urgency ranking (not science)
6. Exact specification (variables, parameters, equations)
7. Limitations & unobserved variables
8. Validation strategy (from heuristic to survival analysis)
9. Testing strategy (TDD + isolation)
10. Phased implementation plan (0 -> N1 -> N2 -> N3 -> N4)
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
which 5ΓÇô7 relationships need attention this week.

Data constraints (these drive every methodology choice):

- (a) Irregular interaction intervals  -  no fixed schedule exists per pair.
- (b) Sparse observations  -  most pairs have 0ΓÇô5 recorded events.
- (c) No explicit user ratings  -  nobody grades relationships 0ΓÇô5.
- (d) Per-pair temporal history only  -  one ego's view, small-n per dyad.
- (e) Outputs must be interpretable in two lines of UI text.
- (f) The method must be implementable in plain C# + SQL Server with no new
      infrastructure, and must evolve toward rigorous statistical modeling later
      without schema breakage.

---

## 2. Architecture

### 2.1 Foundation: extend the existing Clean Architecture

The repository is layered as:

```
Angular client (contacts-manager-client) + external consumers
  -> RelationshipIntelligence.Api (JWT, ContactsController/AccountController)
  -> RelationshipIntelligence.Core (entities, DTOs, service contracts, services)
  -> RelationshipIntelligence.Infrastructure (EF Core + SQL Server repositories, AppDBContext, UnitOfWork)
```

Ownership on this base is `Person.ApplicationUserId` (FK -> AspNetUsers.Id), enforced
by a global EF query filter plus an explicit `PersonOwnershipFilter` on per-id
endpoints. Temporal data uses the existing `Interaction` entity (per-person FK).
Write paths go through `IUnitOfWork.SaveChangesAsync` (repositories attach only).

Dependency direction is inward (UI/API -> Core abstractions -> Infrastructure
implements Core contracts). New Relationship Intelligence functionality follows the
same pattern in the same projects:

- New entities: `RelationshipIntelligence.Core/Domain/Entities/`.
- New DTOs: `RelationshipIntelligence.Core/DTOs/` (DataAnnotations validation, `ToPerson()`-style
  converters, matching existing `PersonAddRequest` / `PersonRespones` patterns).
- New contracts: `RelationshipIntelligence.Core/ServiceContracts/` + `Domain/RepositryContracts/`.
- New services: `RelationshipIntelligence.Core/Services/`, one concern per class, `ILogger<T>`
  + `SerilogTimings` + `ValidationHelpers.ValidationFunction`, matching
  `PersonAdderService` / `PersonGetterService`.
- New repositories: `RelationshipIntelligence.Infrastructure/Repositries/`.
- DI: `RelationshipIntelligence.Api/Program.cs`.
- Pure computational kernels (decay math, graph analysis) live in Core as
  dependency-free classes so they are unit-testable without EF, HTTP, or time.

### 2.2 Explicitly not introduced

No CQRS (read/write asymmetry does not justify it; a materialized
`RelationshipState` read-projection gives the benefit), no graph/document/vector
database (per-user ego graphs are hundreds-thousands of nodes; SQL adjacency +
in-memory analysis suffices), no event bus (direct service calls suffice until
Gmail sync arrives). Sec.11 records the revisit conditions.

---

## 3. Data model & definitions

### 3.1 Ownership

`Person.ApplicationUserId (Guid, required, FK -> AspNetUsers.Id, on-delete Restrict)`.
Every row belongs to exactly one user. Reads are additionally constrained by a
global query filter (`ApplicationUserId == current user && !IsDeleted`); writes
stamp the id server-side from `ICurrentUserService`. No backfill was needed:
ownership predates this work.

### 3.2 Interaction - the behavioral fact table (exists; write API added in R1)

`Interaction { InteractionId, InteractionType (Call|Email|Meeting|Message),
InteractionTitle (required, <=100), InteractionDescription?,
TimeOfInteraction (required), PersonId FK -> Person }` - one row per contact event
per person (1:N). R1 adds the service/repository write path through `IUnitOfWork`,
UTC normalization of `TimeOfInteraction`, no-future-date validation, and CSV import.
`Channel` weights in Sec.6.2 map onto `InteractionType` (Meeting 1.25, else 1.0).

### 3.3 RelationshipState (Phase N2)  -  materialized read projection, not raw data

`RelationshipState { RelationshipStateId, ApplicationUserId, PersonId, TieStrength,
LastContactAt?, CadenceReferenceDays?, SilenceQuantile?, IsBridge, UrgencyScore,
UpdatedAtUtc }`, unique `(ApplicationUserId, PersonId)`. Recomputed nightly +
instant per-pair recalc on write. Digest and graph endpoints read this table;
they never compute on request.

### 3.4 Digest records (Phase N3)

`DigestDelivery { OwnerId, WeekStart, ContactIdsJson, CreatedAtUtc }` (idempotency
key `(OwnerId, WeekStart)`); `DigestMetrics { DeliveryId, PersonId, Opened,
ActionTaken, ActionType? }`  -  the labeled outcomes that power Sec.8 validation.
Analytics rows are never mixed with contact data.

---

## 4. User-data isolation (security boundary)

**Rule: a user must never retrieve, modify, score, or analyze another user's data,
even under a malicious or malformed request. Controllers are never the sole
security boundary.**

Enforcement on this base (three layers):

1. `ICurrentUserService.UserId (Guid?)` resolves the principal server-side from the
   JWT `NameIdentifier` claim (fixed in R0 to carry the user id; email moved to a
   dedicated `Email` claim consumed by logout). Null means unauthenticated.
2. `AppDBContext` applies a global query filter on `Person`
   (`ApplicationUserId == current user && !IsDeleted`), so all Person reads -
   including lists, searches, and batched grids - are owner-scoped with no
   per-call parameters. Writes stamp `ApplicationUserId` from `ICurrentUserService`
   and throw `UnauthorizedAccessException` without one.
3. `PersonOwnershipFilter` (registered in DI, applied to per-id endpoints)
   returns 401/403 for foreign ids; controllers map missing entities to 404
   instead of leaking existence.

Covered by `Tests/OwnerIsolationTests.cs`: JWT claim contents, QuickAdd attach +
stamping, and fail-closed reads/updates/deletes for foreign ids.

---

## 5. Scientific methodology

### 5.1 Literature review  -  established constructs we operationalize

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
   variety); provides no decay equation.
4. **Tie decay and persistence (Burt 2000 "Decay functions," Social Networks;
   Burt 2002 "Bridge decay").** Two robust findings: (i) **decay probability
   decreases with tie age** (older ties are hardier  -  a power function of time);
   (ii) **bridges decay faster than embedded ties**, converging only after
   several years. *Consequence:* tie age and embeddedness/bridge status are the
   only covariates we admit in v1, and only where data exists.
5. **Temporal predictors of persistence (Raeder et al. / Hidalgo stream, EPJ Data
   Science 2017, 19-month call data).** Persistence is predicted by intensity,
   embeddedness (topological overlap), and temporal distribution  -  **burstiness
   predicts decay**. Standard fit: logistic persistence
   `╬á(persist) = 1 / (1 + exp(ΓêÆ╬▓┬╖x))`. *Consequence:* this logistic framing is our
   validation target (Sec.8), not our v1 implementation.
6. **Tie-decay networks (Ahmad, Porter, Beguerisse-D├¡az 2018ΓÇô2021; Grindrod et
   al.).** Formalism that **separates interactions (discrete events) from ties
   (continuous strengths): tie strength decays exponentially between interactions
   and is boosted discretely on each interaction.** Peer-reviewed, mathematically
   specified, and exactly our data shape. *Consequence:* this is our adopted v1
   core (Sec.5.3)  -  the only "established model" claim in this document.
7. **Survival / event-history analysis (de Nooy 2011; Dean, Bauer & Prinstein:
   discrete-time multilevel survival for friendship dissolution; joint
   longitudinal + time-to-event models).** The rigorous framework for whether
   *and when* ties dissolve. Requires longitudinal outcome data we do not yet
   have. *Consequence:* v1 is designed to emit valid inputs for this later stage
   (per-pair histories + labeled outcomes), but survival modeling is not v1.
8. **Relational maintenance (Stafford & Canary 1991; Canary et al.).** Maintenance
   strategies (positivity, openness, assurances, networks, shared tasks) must be
   enacted continuously; effects diminish over time. *Consequence:* behavioral
   justification for monitoring cadence at all; no equation taken.

### 5.2 Rejected approaches

- **SM-2 / FSRS (spaced repetition).** Require explicit difficulty grades and
  scheduler-controlled intervals. Our setting has neither (passive observation,
  no grades, no control over the counterparty). FSRS's Retrievability/Stability
  distinction inspired separating current state from baseline, but the algorithms
  themselves are inapplicable. Rejected as core; not cited as basis.
- **Invented saturating curves** (e.g. any `100┬╖(1ΓêÆexp(ΓêÆ╬╗(rΓêÆ1)))` with chosen ╬╗,
  fixed day constants, 14/21/30-day priors, 50/65/85 bands, 1.5├ù multipliers).
  Rejected in the prior review: mathematical elegance and interpretability do not
  constitute academic justification. None ship as science; bands/multipliers that
  appear in UI are labeled presentation layer (Sec.5.7).

### 5.3 Adopted core  -  exponential tie-decay with event boosts (established)

For each `(owner, person)` pair with ordered event times `tΓéü < tΓéé < ... Γëñ now`:

```
s(t) = s(tß╡óΓü║) ┬╖ exp(ΓêÆ╬▒┬╖(t ΓêÆ tß╡ó)),   tß╡ó < t Γëñ tß╡óΓéèΓéü        (decay between events)
s(tß╡óΓü║) = s(tß╡óΓü╗) + b(channelß╡ó)                            (boost on each event)
```

- *Basis:* Ahmad et al. tie-decay formalism (Sec.5.1.6). Established; we claim only
  the dynamic, not any parameterization.
- *Why it fits (Sec.1 constraints):* handles irregular intervals natively (╬öt is
  continuous); works with any n ΓëÑ 1 (no fitting per pair); purely observational
  (no grades); dependency-free computation; outputs feed survival models later.
- *Parameters* `╬▒`, `b(┬╖)`: engineering choices, Sec.6.2; disclosed, sensitivity-tested.

### 5.4 Established covariates (Burt; conservative use)

- **Tie age:** older ties decay slower (Burt 2000). Operationalized as a bounded
  age dampener on effective decay (Sec.6.3). Bounded so it can never dominate the
  event-driven signal; omitted when first-contact date is unknown.
- **Embeddedness / bridges:** bridges are higher-risk (Burt 2002). Operationalized
  as a displayed flag + bounded urgency adjustment (Sec.6.4), computed from
  shared Company/Sector/Tags + co-occurrence edges (Sec.6.5). No imputation when data
  is absent.

### 5.5 Our engineering operationalization (explicitly NOT established science)

The following are our choices, necessitated by sparse passive data, and are labeled
`[OPERATIONALIZATION]` wherever they appear in code/docs:

- Median-gap cadence *reference* (descriptive context only, never a fitted model).
- Cold-start priors, minimum-observation rules, channel weights, bridge heuristic
  edge rules, urgency ranking composition, health bands, digest selection rules.
- Each is documented with: what it is, why the data forces it, what literature
  (if any) constrains it, and how it will be validated or replaced (Sec.8).

### 5.6 Cold-start priors  -  assumptions, not findings

No literature prescribes per-persona contact cadences. V1 priors
(`HiringManager 14d, Client 21d, Default 30d`, Sec.6.6) are transparent placeholders
so the system functions at n < 3. They are constants in one table, used only until
personal history exists, and scheduled for empirical recalibration from observed
gaps once volume permits. Presented in UI only as "estimated rhythm," never as fact.

### 5.7 Presentation layer  -  bands, urgency (not science)

Health bands (Healthy/Drifting/At Risk/Critical) and the urgency composition
(`score ├ù key-weight`, exclusions) are ranking/display conventions for triage,
tuned by future outcome data  -  not constructs with literature status. Code and UI
copy must never imply otherwise.

---

## 6. Exact specification

### 6.1 Time and inputs

- All timestamps UTC (`OccurredAtUtc`). "Now" = `DateTime.UtcNow` at computation.
- Per-pair input: ordered event times + channels + optional tie-start
  (`Person.CreatedAtUtc` or first event, whichever earlier and known).
- Cold-start table `PersonaPriors { Sector?, DefaultDays }` (Sec.6.6).

### 6.2 Tie-decay parameters (engineering choices, sensitivity-tested)

| Parameter | V1 value | Definition | Why this value |
|---|---|---|---|
| `╬▒` (decay rate) | `ln(2)/60 Γëê 0.01155 dayΓü╗┬╣` | Exponential rate; half-life 60 days | Central assumption: a professional tie with zero contact halves in ~2 months. Chosen as a round, reviewable middle of 30/60/90d sensitivity grid (tests assert ordering across all three; product ships 60d). NOT a literature constant. |
| `b(channel)` boost | 1.0 all channels (Meeting 1.25) | Additive strength on each event | Equal weights = no unjustified claims (Gilbert supports multiplexity but not our weights). Meeting 1.25 reflects synchronous high-cost interaction; flagged `[OPERATIONALIZATION]`, validated later. |
| `TieStrength` scale | open-ended ΓëÑ 0 | `s(now)` per Sec.5.3 | Comparability within user, not across users. Displayed normalized per-user (min-max over user's pairs) so 0ΓÇô100 is a *within-network percentile aid*, labeled as such. |

*Worked example:* events 40d and 10d ago, ╬▒ as above: s = e^(ΓêÆ╬▒┬╖40) + e^(ΓêÆ╬▒┬╖10)
Γëê 0.63 + 0.89 = 1.52. Thirty silent more days -> 1.52┬╖e^(ΓêÆ╬▒┬╖30) Γëê 1.07.

### 6.3 Tie-age dampener (Burt 2000; bounded)

`effective ╬▒' = ╬▒ / (1 + min(tieAgeYears, 5) ┬╖ 0.06)`. A 5-year tie decays at most
~23% slower. Bounded, subordinate to event history, omitted when tie start is
unknown. Labeled `[OPERATIONALIZATION of Burt 2000 direction, not magnitude]`  - 
Burt establishes the direction; no paper gives our 0.06.

### 6.4 Bridge flag (Burt 2002; display + bounded ranking input)

`IsBridge` per Sec.6.5. Effect: flagged in UI ("connects two parts of your network  - 
bridges are lost faster, Burt 2002") and adds at most +10 urgency points (Sec.6.7).
Magnitude is ours; direction is Burt's.

### 6.5 Ego-graph construction (SNA-lite; standard measures only)

- Nodes: user's contacts. Edges (all same-user, undirected): shared Company (weight
  1.0), shared Sector (0.5), shared tag (0.5/tag, cap 1.0), co-occurrence in one
  interaction's attendee set (1.0). Rules are `[OPERATIONALIZATION]`; the measures
  are standard: **degree centrality** and **articulation points (bridges)** via
  Tarjan's algorithm; **structural holes** shown descriptively as disconnected
  clusters, with no constraint/effective-size statistics claimed until validated.
- Betweenness reserved for later (cost + interpretability); degree + articulation
  cover the v1 questions ("load-bearing?", "what connects clusters?").

### 6.6 Cadence reference & cold-start priors (descriptive context only)

- With ΓëÑ3 observed gaps: reference = median(gaps), shrunk once toward prior:
  `ref = (n┬╖median + 3┬╖prior)/(n+3)`, clamped [3, 180]d. Shrinkage constant 3 =
  standard weak-prior pseudo-count, disclosed as choice.
- With <3 gaps: prior table  -  HiringManager 14, Client 21, Candidate 21, Investor
  30, Default 30 days. **Assumptions, not findings** (Sec.5.6).
- UI copy: "roughly every X days (estimated)" + silence context. Never an input to
  the decay dynamic  -  the dynamic uses raw event times (Sec.5.3), so the reference
  cannot corrupt scoring; it only contextualizes.

### 6.7 Urgency ranking (presentation layer, explicitly not science)

```
urgency = 100 ┬╖ (1 ΓêÆ s(now)/sMaxUser)        # deficit vs user's strongest tie
urgency += 10  if IsBridge                    # Sec.6.4, bounded
urgency  ├ù= 1.5 if IsKey                      # user assertion, labeled as such
skip if IsExcluded or contacted within 7d (digest context)
```

`sMaxUser` = max strength over the user's pairs (fallback 1.0). Bands on urgency:
<40 Healthy, 40ΓÇô65 Drifting, 66ΓÇô85 At Risk, >85 Critical  -  UI thresholds for
triage, to be recalibrated from outcome data (Sec.8). `IsKey ├ù1.5` encodes the user's
own assertion of importance, not a scientific weight.

---

## 7. Limitations & unobserved variables

1. Closeness/intimacy (Marsden & Campbell's strongest indicator) is unobserved  - 
   our strength is a behavioral proxy.
2. One-sided observation: counterparty-side events (their outreach elsewhere,
   job changes) are invisible until enrichment arrives.
3. No sentiment/content: a dispute and a deal-closing look identical as events.
4. Small-n per pair: all per-pair quantities are high-variance; shrinkage and
   priors mitigate but do not remove this.
5. Bursty professional rhythms (project crunches) will distort raw gaps; the
   digest suppression window and per-user recalibration (Sec.8) are the mitigations.
6. No causal claims: macro-vs-personal attribution is deferred (Sec.11); v1 never
   states *why* a tie cooled.

---

## 8. Validation strategy (heuristic -> evidence)

- **V1 (now):** unit tests pin the dynamic's mathematical properties
  (monotonic decay, boost ordering, half-life behavior, age-dampener bounds);
  sensitivity tests assert ranking stability across ╬▒ Γêê {30,60,90d half-lives};
  dogfood check  -  "did it surface a genuinely forgotten contact?"
- **V2 (with digest outcome data):** fit Raeder-style logistic persistence
  `╬á(persist)=1/(1+exp(ΓêÆ╬▓┬╖x))` on features {strength, recency, frequency,
  burstiness, embeddedness, tie age}; report calibration + AUC vs. recency-only
  and frequency-only baselines. Recalibrate bands, priors, and weights from fitted
  coefficients. This is the first empirical claim we will be entitled to make.
- **V3 (longitudinal):** discrete-time multilevel survival (Dean/de Nooy framing)
  for time-to-dissolution; joint models only if content signals arrive.
- **Falsification:** if fitted models do not beat recency-only baselines, the
  scoring layer is simplified, not defended.

---

## 9. Testing strategy (TDD + isolation)

Per phase, tests precede or accompany code, mirroring existing infrastructure
(xUnit + Moq + AutoFixture + FluentAssertions; service/contract mocking as in
`Tests/PersonServicesTest.cs`; controller tests as in
`ContactsManager.ControllersTest`; HTTP tests via `CustomWebApplicationFactory`):

- Pure-kernel tests (no I/O): decay monotonicity, boost additivity, half-life
  identity `s(t┬╜)=sΓéÇ/2`, age-dampener bounds, quantile behavior, graph fixtures
  (two clusters + bridge -> flagged; isolates listed).
- Service tests: validation, ownership forwarding, cross-user null/false.
- Repository tests: two-owner seeded EF data; assert isolation on every method.
- Integration tests: second principal -> 404 on foreign ids; digest signed-URL
  valid/expired/tampered cases; job idempotency (double-run -> same rows, one email).
- Sensitivity tests: alpha-grid ranking-stability.

---

## 10. Phased implementation plan

### Phase R0 - Stabilization & security (prerequisite, done first)
Fix JWT `NameIdentifier` to carry the user id (email moved to an `Email` claim);
fix QuickAdd to attach the entity through the repository; apply
`PersonOwnershipFilter` to per-id endpoints with 404 mapping; add
`Tests/OwnerIsolationTests.cs`; repair CI to build/test existing projects;
untrack bin/obj/.vs. Done: real-token auth round-trip, cross-user suite green,
CI green.

### Phase N1  -  Temporal foundation
Objective: the behavioral fact table. `Interaction` + `InteractionRepository` +
`IInteractionService` (log/list/import CSV, max 500 rows) + UTC normalization.
Endpoints: per-contact timeline + `POST /api/contacts/{id}/interactions`. Done:
log->timeline round-trip, CSV import, isolation on new paths, old tests green.

### Phase N2  -  Decay scoring + queue
Objective: first CSS output. Pure `TieDecayModel` (Sec.5.3/Sec.6.2) + age dampener
(Sec.6.3) + `IRelationshipScoringService` writing `RelationshipState` (nightly
`IHostedService` + instant per-pair recalc on log) + queue endpoint/view + list
signal dot + band filter. Done: dogfood surprise check, queue live, math-property
+ sensitivity + idempotency tests green.

### Phase N3  -  Digest habit loop
Objective: retention + labeled outcomes. `IDigestService` top-5 selector (urgency
order, exclusions, 7-day suppression, never pad), `IEmailSender` (SMTP later;
in-code default logs/saves), HMAC-signed one-click actions (7-day expiry,
`SourceType=Digest`), settings, `DigestDelivery/Metrics`. Done: email + in-app
panel from one payload, one-click creates interaction login-free, metrics recorded.
Gate (>60% open) before any macro work.

### Phase N4  -  SNA-lite topology
Objective: structural insight without new infra. `INetworkAnalyzer` (edge rules
Sec.6.5, degree + Tarjan articulation, cluster view), cached per user, D3-class force
view + bridge ring + isolate ring + dim-not-remove filters. Done: fixture graphs
correct, perf guard (<500 nodes, server <2s), isolation on graph endpoint.

---

## 11. Deferred work & non-goals

MacroScope feeds (GDELT/EWS/CPCI/country risk), Context Engine (cause attribution,
timing predictor, correlation learning), agent simulation, Career/Lifecycle/
Flight-Risk/Account-Temperature engines, Gmail/Calendar sync (reserved enum values
only), LinkedIn/WhatsApp, team model, public API, CQRS, new databases, event bus.
Revisit only on: retention gate passed (macro), 100k+ users or real-time graph
load (CQRS/new stores), second OAuth provider proven (events).

---

## 12. Decision log

- Sec.12.1 (unification): work branch is `relationship-intelligence-main`, based on
  `origin/ay/frontend-login-test` plus master's docs. Stale-base WIP on
  `registration-implementation` is reference only and is never merged; its
  decay/digest/SNA pieces are re-applied onto this base in R1-R4.
- Sec.12.2 (Sec.A correction): prior invented formula and constants withdrawn in full;
  tie-decay dynamic adopted as only established-model claim.
- Sec.12.3 (R0 fixes): JWT claim mismatch, QuickAdd no-op create, ownership filter
  wiring, updater-test/UoW mismatch, CI project references, committed build
  artifacts - all corrected on the new base with covering tests.
- Sec.12.4 (email transport): `IEmailSender` ships with a file/log implementation;
  SMTP credentials are deployment config, never code.
- Sec.12.5 (N3/N4 scope notes): v1 `Interaction` rows carry a single `PersonId`, so
  the co-occurrence edge rule in Sec.6.5 is implemented in the pure `NetworkAnalyzer`
  kernel and covered by fixtures, but the service passes no co-occurrence groups
  until multi-attendee events exist. Likewise the digest "remind" action records
  an intent metric; standalone reminder scheduling is deferred to the
  integration phase. Neither affects scoring correctness.
- Sec.12.6 (migration): new tables (`RelationshipState`, digest records) ship via a
  single EF migration generated at the end of R2; operator applies at deploy time.

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
- de Nooy, W. (2011); Dean, D., Bauer, D. & Prinstein, M.  -  multilevel/event-history
  models for friendship dissolution.
- Stafford, L. & Canary, D. (1991). Maintenance strategies and romantic
  relationship type... *JSPR*, 8. Plus Canary-Stafford program (1992-2011).
- Stafford, L. (2011). New typology of maintenance strategies.
