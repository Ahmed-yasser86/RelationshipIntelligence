# Testing strategy: invariants, not counts

272 tests in `RelationshipIntelligence.Tests`, 24 in
`RelationshipIntelligence.ControllerTests`, green on every push
(`dotnet test RelationshipIntelligence.sln`). The suite is hermetic:
xUnit + Moq + EF-mocked repositories, stubbed extractors/providers,
zero network. A test that needs the network is a bug in the test —
nondeterministic externals (LLM wording, provider states) sit behind
interfaces precisely so the suite can pin the behavior without them.

## Coverage by area (what each suite pins)

**Scoring math** — `TieDecayModelTests` (18: decay sums, half-life,
equal boosts, cadence clamp/blend, silence floor, quantile, urgency,
bands); `RelationshipScoringServiceTests` (recompute upsert/snapshot/
history, NoHistory exclusion, Insufficient band cap, queue ordering).

**Preferences & reminders** — `RelationshipPreferenceServiceTests`
(validation, ownership, snooze/skip lifecycle, due computation,
audit rows with previous/new, global defaults, completion-requires-
interaction); `PreferenceScheduleParserTests` (natural-language
schedules incl. "twice a month" → 15, unsupported → null);
`InteractionServiceTests` (log validation, reminder-clear on real log,
scoring call).

**Ingestion & review** — `IngestionServiceTests` (submit idempotency/
limits, slot mapping, conflict analysis, approve gating);
`MeetingServiceTests` + `MeetingPipelineIntegrationTests`
(begin/process/confirm guards, brief-only confirm, extractor→rows).

**Copilot & search** — `RelationshipQueryPluginTests` (unknown-id
safety, org-member completeness, filter passthrough, interaction/
event/meeting/memory filters); `PersonSearcherServiceTests`
(name/company composite search, evidence filters);
`PersonNameExtractorTests` (similarity thresholds);
`AgentSessionStoreTests` (session create/isolate per owner).

**Outreach, digest, memory, events, network, orgs** —
`OutreachServiceTests` (signal→member build, per-person grounding,
approval-contacts-nobody); `DigestServiceTests` (threshold/count
selection, stale-state recompute, exclusion); `RelationshipMemoryServiceTests`
(CRUD, AI→User provenance flip); `EventServiceTests` (recurrence incl.
Feb 29); `NetworkAnalyzerTests` (shared-context edges, components,
bridges); `OrganizationServiceTests` (member ops).

**Ownership & API contracts** — `OwnerIsolationTests`,
`PersonOwnershipIsolationTests`, controller tests (HTTP mappings:
404/400/409/503 incl. completion-without-interaction → 409);
`ArchitectureTests` (Core↔AI boundaries, scoring type isolation).

**People & data** — `PersonServicesTest`, `PersonPaginationTest`
(paging clamps, mapping), `CountryServiceTest`, `DemoWorkspaceServiceTests`
(seed guards), `AiProviderSettingsServiceTests` (key never returned),
`RelationshipStateRepositoryTests` (field persistence).

## Conventions

- Mocks at service boundaries; fakes where behavior matters
  (in-memory extraction results, stubbed providers).
- Thresholds are pinned and require re-measurement to change (60-day
  half-life, band edges 85/65/40, cadence clamp 3–180).
- Regression discipline: every live bug fix ships with the test that
  would have caught it (e.g. `ReviewAsync_ApproveThenEditedTitle_
  StaysReviewed`, completion-409 tests).

## Frontend

No unit-test project; CI enforces `oxlint` + `tsc -b && vite build`.
Contract safety comes from shared `types.ts` shapes and controller
tests pinning the payloads the UI consumes.

## Known caveats, documented not hidden

- E2E fixtures (`E2E Probe`, `E2E OrgPick` contacts) live in the same
  dev database as real contacts and appear in counts — filter by name
  when verifying totals.
- Full-suite timing flakes on strained machines (MSBuild node pressure);
  single-node (`-m:1`) runs are the reference.
