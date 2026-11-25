# Documentation evidence ledger

Implementation-first record for the Relationship Intelligence thesis documentation.
Every claim cites an exact location. Tiers per the documentation skill:
Tier A = code/tests/schema/API/config, Tier B = project design docs,
Tier C = comments/history, Tier D = labelled inference.

| Claim | Source | Exact location | Tier | Confidence | Status |
|---|---|---|---|---|---|
| Tie strength: `s = Σ 1.0 · exp(-α·age)`, `α = ln(2)/60`, equal boost, no channel weights | `TieDecayModel.StrengthAt` | `RelationshipIntelligence.Core/Services/TieDecayModel.cs:17-31` | A | high | VERIFIED |
| Half-life constant 60 days; `AlphaForHalfLife = ln(2)/halfLife` | `TieDecayModel` | `TieDecayModel.cs:9-15` | A | high | VERIFIED |
| Cadence reference: `median` blended with persona prior (3 pseudo-obs), clamped 3–180; prior alone when <3 gaps | `TieDecayModel.CadenceReference` | `TieDecayModel.cs:33-45` | A | high | VERIFIED |
| Persona priors: default 30d; recruiting/hiring → 14d; client → 21d | `PersonaPriors` | `RelationshipIntelligence.Core/Services/PersonaPriors.cs:8-17` | A | high | VERIFIED |
| Silence = floor of elapsed UTC days; null when no contact; 0 for future | `TieDecayModel.SilenceDays` | `TieDecayModel.cs:55-65` | A | high | VERIFIED |
| Silence quantile = fraction of past gaps ≤ current silence; null when no gaps | `TieDecayModel.SilenceQuantile` | `TieDecayModel.cs:67-73` | A | high | VERIFIED |
| Urgency = `100·(1 − strength/maxUserStrength)`, clamped 0–100 | `TieDecayModel.Urgency` | `TieDecayModel.cs:75-80` | A | high | VERIFIED |
| Bands: Critical >85, AtRisk >65, Drifting ≥40, else Healthy | `TieDecayModel.BandFor` | `TieDecayModel.cs:82-88` | A | high | VERIFIED |
| Display band capped at Drifting when evidence is Insufficient; urgency untouched | `RelationshipScoringService.CapBandForEvidence` | `RelationshipIntelligence.Core/Services/RelationshipScoringService.cs:373-378` | A | high | VERIFIED |
| Zero-history rows: strength 0, urgency 0, no cadence/quantile, excluded from ranking | `RelationshipScoringService.BuildState` | `RelationshipScoringService.cs:398-414` | A | high | VERIFIED |
| Queue order: urgency desc, preference priority, system importance, oldest contact | `RelationshipScoringService.GetQueueAsync` | `RelationshipScoringService.cs:251-254` | A | high | VERIFIED |
| Preferences never change scores: only tie-break order, `IsImportant` flag, event-signal threshold, labels | `RelationshipScoringService` | `RelationshipScoringService.cs:214-218,252-253,266-270,327-343` | A | high | VERIFIED |
| Precedence: explicit per-person > global default > system inference | `RelationshipScoringService` | `RelationshipScoringService.cs:234-247,327-340` | A | high | VERIFIED |
| Network edges = shared context (org +1.0, tags ≤+1.0, channel +0.5); attributes shared by >50 people ignored | `NetworkAnalyzer` | `RelationshipIntelligence.Core/Services/NetworkAnalyzer.cs:27-64,108-114` | A | high | VERIFIED |
| Bridges = articulation points (iterative Tarjan); clusters = connected components, no community detection | `NetworkAnalyzer` | `NetworkAnalyzer.cs:131-188,191-215` | A | high | VERIFIED |
| Network is deterministic; no random seeds | `NetworkAnalyzer` | `NetworkAnalyzer.cs:27-30,74-85` | A | high | VERIFIED |
| Edges never represent observed communication | `docs/METHODOLOGY.md` §7 + code | `docs/METHODOLOGY.md:52-60`; `NetworkAnalyzer.cs:27-72` | B+A | high | VERIFIED |
| Interaction log is the sole writer of relationship-state-affecting completion | `InteractionService.LogAsync` | `RelationshipIntelligence.Core/Services/InteractionService.cs:62-86` | A | high | VERIFIED |
| Reminder view/snooze/skip/disable never create interactions | `RelationshipPreferenceService` + plugins | `RelationshipPreferenceService.cs:209-265`; `RelationshipQueryPlugin.cs:713` | A | high | VERIFIED |
| `CompleteAsync` endpoint writes nothing; throws unless already disabled | `RelationshipPreferenceService.CompleteAsync` | `RelationshipPreferenceService.cs:236-253` | A | high | VERIFIED |
| Extraction never writes durable state; approval is the only write path | `IngestionService` | `RelationshipIntelligence.Core/Services/IngestionService.cs:546-547,626-643` | A | high | VERIFIED |
| Idempotency via normalized-text SHA-256 hash + meeting-scoped dedupe | `IngestionService.HashSource/SubmitAsync` | `IngestionService.cs:86-92,110-113` | A | high | VERIFIED |
| Provenance values: `IngestionDerived` on approved ingestion writes; `MeetingDerived` on meeting confirm | services | `IngestionService.cs:766,894`; `MeetingService.cs:577` | A | high | VERIFIED |
| Outreach approval sends nothing (explicit log line) | `OutreachService.ApproveAsync` | `RelationshipIntelligence.Core/Services/OutreachService.cs:883-893` | A | high | VERIFIED |
| Suggestion exclusion respected in Digest and all Outreach signal paths | services | `DigestService.cs:107-124`; `OutreachService.cs:277-294` | A | high | VERIFIED |
| Digest selection: urgency ≥ threshold, silence > 7d, weekly delivery dedupe, HMAC-signed action URLs | `DigestService` | `RelationshipIntelligence.Core/Services/DigestService.cs:123-143,150-163` | A | high | VERIFIED |
| Meeting lifecycle Preparation→Draft→Processing→Processed→Confirmed; agenda never becomes evidence | `MeetingService` | `MeetingService.cs:246-344,808-871`; `IngestionService.cs:152-157` | A | high | VERIFIED |
| Copilot never computes scores; tools are read-only except confirmation-gated actions | plugins + prompts | `RelationshipQueryPlugin.cs:12-16`; `CopilotPrompts.cs:19-21` | A | high | VERIFIED |
| Owner isolation via `ApplicationUserId == _currentUserId` query filters on all user tables | `AppDBContext` | `RelationshipIntelligence.Infrastructure/DBContext/AppDBContext.cs:15-21,76-233` | A | high | VERIFIED |
| 272 unit/integration + 24 controller tests green at documentation time | test run | `RelationshipIntelligence.Tests` (272), `RelationshipIntelligence.ControllerTests` (24) | A | high | VERIFIED |
| Scoring uses exponential decay from interaction history (literature basis) | project docs | `docs/METHODOLOGY.md:14-21` | B | medium | DOCUMENTED BUT UNVERIFIED |
| Specific literature citations for decay parameters | — | none found in repo | — | — | NOT IMPLEMENTED |
| `NoScore` tag excludes a person from all scoring surfaces | `RelationshipScoringService` | `RelationshipScoringService.cs:354-365` | A | high | VERIFIED |
| Seed user `testuser@contactsmanager.dev`; e2e password `Test123!`; stored hash stale | `AppDBContext` + e2e helpers | `AppDBContext.cs:363-383`; `relationship-intelligence-client/tests/e2e/helpers.ts:7` | A+B | high | VERIFIED |
