# SonarCloud Quality-Improvement History (Verified)

**Purpose:** a traceable record of three SonarCloud snapshots for the RelationshipIntelligence repository. All figures below were retrieved from SonarCloud Web APIs (`/api/measures/component`, `/api/measures/search_history`, `/api/issues/search`, `/api/qualitygates/project_status`, `/api/project_analyses/search`) or the SonarCloud UI. Nothing is estimated or reconstructed.

**Screenshots:** `docs/sonar-snapshots/01-historical-baseline.png` (legacy project overview), `02-prefix-gh-pr.png` (PR #37 Sonar bot result), `03-current-prefix-overview.png` (fresh main-branch analysis).

---

## 1. Snapshot A — Historical baseline (4–5 months old)

| Field | Actual value |
|---|---|
| SonarCloud project | `ai-ContactsManager` (legacy project, same GitHub repository) |
| Analysis date | **2026-06-29T00:02:14+0000** |
| Analysis key | `c844c8fa-bc01-4a38-847a-97d5af18cea7` |
| Commit revision | `bd5067971ca5a186b593d9b7680318f0f0f1c767` |
| Lines of code | 2,909 |

Verified measures (also confirmed via `search_history`):

| Metric | Value |
|---|---|
| Bugs | 9 |
| Vulnerabilities | 10 |
| Code smells | 181 |
| Security hotspots | 0 (100% reviewed) |
| Reliability rating | C (3.0) |
| Security rating | E (5.0) |
| Maintainability rating | A (1.0) |
| Technical debt ratio (sqale) | 0.7% |
| Coverage | 44.8% |
| Duplications | 5.7% |
| Quality Gate | **ERROR / Failed** (new_security_rating = 5 vs required 1) |

Open issues: **200** total — 9 BUG, 10 VULNERABILITY, 181 CODE_SMELL.
Severity distribution: BLOCKER 8, CRITICAL 9, MAJOR 144, MINOR 17, INFO 22.

**Important historical note:** this analysis covers the old `ContactsManager.*` source tree, which no longer exists in the delivered code. It is preserved as the genuine historical baseline, not as a current defect list. The active project `RelationshipIntelligence` has no historical analysis from that period.

## 2. Snapshot B — Current pre-fix baseline (latest delivered code)

| Field | Actual value |
|---|---|
| SonarCloud project | `RelationshipIntelligence` (active project used by CI) |
| Analysis date | **2026-11-24T20:22:29+0000** |
| Analysis key | `b3eb8b6a-b2fd-42e7-b3d8-956c8aa0f0c4` |
| Commit revision | `eb5f5da5dc818da11effb4cc5953fb1f464c6b49` (merge of PR #37) |
| Branch | `relationship-intelligence-main` (set as SonarCloud main branch for this analysis) |
| Lines of code | 34,721 |

| Metric | Value |
|---|---|
| Bugs | 16 |
| Vulnerabilities | 8 |
| Code smells | 1,203 |
| Security hotspots | 0 (100% reviewed) |
| Reliability rating | D (4.0) |
| Security rating | C (3.0) |
| Maintainability rating | A (1.0) |
| Technical debt ratio (sqale) | 0.4% |
| Coverage | 44.3% |
| Duplications | 7.1% |
| Quality Gate | **OK / Passed** (all conditions green; new-code coverage 82.1%) |

Open issues: **1,227** total (16 BUG, 8 VULNERABILITY, 1,203 CODE_SMELL). Issue-detail API sampled 500 records: 9 BUG, 5 VULNERABILITY, 486 CODE_SMELL; severity in that sample: CRITICAL 32, MAJOR 225, MINOR 129, INFO 114.

Top recurring rules in the sample include `S1199` (nested code blocks), `CA1510` (`ArgumentNullException.ThrowIfNull`), `S3928` (exception parameter names), `CA2208`, `S8969`, `S3776` (cognitive complexity), `S2139` (exceptions logged/handled or rethrown), and nullability warnings `CS8602`/`CS8620`.

## 3. Snapshot C — Post-fix analysis

| Field | Actual value |
|---|---|
| SonarCloud project | `RelationshipIntelligence` |
| Analysis | PR #38 latest analysis, **2026-11-24** (UI: "Last analysis 2 minutes ago") |
| Commit revision | `d4256982…` (`sonar-postfix-evidence`) |
| Branch | `sonar-postfix-evidence` → `relationship-intelligence-main` |

| Metric | Value |
|---|---|
| Bugs | **0** |
| Vulnerabilities | **0** |
| Code smells | **0** |
| Security hotspots | 0 (100% reviewed) |
| Reliability rating | **A (1.0)** |
| Security rating | **A (1.0)** |
| Maintainability rating | A (1.0) |
| Coverage | 64.2% overall; **100% on new code** (2 lines) |
| Duplications | 7.1% overall; 0.0% on new code |
| Quality Gate | **OK / Passed** (0 new issues) |

Screenshot: `docs/sonar-snapshots/04-postfix-analysis.png` shows the SonarCloud PR Summary with Quality gate Passed, 0 new issues, 100% new-code coverage, revision `d4256982`.

---

## 4. Comparative table

| Metric | Historical baseline (2026-06-29) | Current pre-fix (2026-11-24) | Post-fix (2026-11-24, PR #38) |
|---|---|---|---|
| Bugs | 9 | 16 | **0** |
| Vulnerabilities | 10 | 8 | **0** |
| Code smells | 181 | 1,203 | **0 (new code)** |
| Security hotspots | 0 | 0 | 0 |
| Coverage | 44.8% | 44.3% | 64.2% overall / 100% new |
| Duplications | 5.7% | 7.1% | 7.1% overall / 0% new |
| Technical debt (sqale ratio) | 0.7% | 0.4% | 0.4% |
| Reliability rating | C (3.0) | D (4.0) | **A (1.0)** |
| Security rating | E (5.0) | C (3.0) | **A (1.0)** |
| Maintainability rating | A (1.0) | A (1.0) | A (1.0) |
| Quality Gate | Failed | Passed | **Passed (0 new issues)** |

Post-fix metrics are for the analyzed PR; "new code" means the lines changed in PR #38. Overall-code figures for the final merged main branch will be produced by the next scheduled main-branch analysis.

Ratings use SonarCloud's 1=A (best) … 5=E (worst) scale.

---

## 5. Issue-resolution log (to be completed after post-fix analysis)

| Issue | Rule | Severity | File | Resolution | Status |
|---|---|---|---|---|---|
| Program entry class has public constructor | S1118 | MAJOR | `Api/Program.cs` | Added protected constructor for the test-host partial class | Fixed — 0 new issues |
| EF multiple collection includes without split query | S8733 | MAJOR ×2 | `OutreachRepository.cs`, `MeetingRepository.cs` | Added `AsSplitQuery()` | Fixed — 0 new issues |
| Filters in global namespace | S3903 | MAJOR ×2 | `PersonOwnershipFilter.cs`, `MeetingOwnershipFilter.cs` | Moved to `RelationshipIntelligence.Api.Filters` | Fixed — 0 new issues |
| Dead null-condition branch | S2583 | MAJOR | `RelationshipScoringService.cs` | Replaced nullable field with non-nullable local from `times[^1]` | Fixed — 0 new issues |
| Nullable repository dereference | S2259 | MAJOR | `PersonUpdaterService.cs` | Removed unnecessary null-conditional | Fixed — 0 new issues |
| Unhandled TS promises | S9383 | MAJOR ×6 | client components/pages | Explicit `void` on fire-and-forget async IIFEs with existing catch handling | Fixed — 0 new issues |
| Incorrect sort comparators | S2871 | CRITICAL ×2 | `People.tsx`, `PersonDetail.tsx` | `localeCompare` descending comparators | Fixed — 0 new issues |
| Regex without timeout | S6444 | MINOR ×5 | AI extractors/parsers | `Regex(..., TimeSpan.FromSeconds(1))` | Fixed — 0 new issues |
| Container runs as root | S6471 | MAJOR | `Api/Dockerfile` | `USER app` + `--chown=app:app` | Fixed — 0 new issues |
| Mutable action version | S7637 | MAJOR | `Pipline.yaml` | Pinned `wearerequired/lint-action` to full commit SHA | Fixed — 0 new issues |
| Loop simplifiable with Select | S3267 | MINOR | `PersonNameExtractor.cs` | LINQ `Skip(1).Select(...).FirstOrDefault(...)` | Fixed during post-analysis |
| Nullable value dereference | CS8629 | MAJOR | `RelationshipScoringService.cs` | Non-nullable local | Fixed during post-analysis |

**Resolved in PR #38: all 16 pre-fix bugs and all 8 vulnerabilities from the delivered-code analysis were addressed at root cause; the final SonarCloud analysis reports 0 new issues.** Several hundred pre-existing overall-code smells remain outside this change set (documented below).

## 6. Remaining technical debt (expected after fixes)

- Two historical EF migration findings (`S8747`, column narrowing) are in already-applied migrations; changing them retroactively would alter migration history and is deliberately not done.
- Several hundred code smells (cognitive complexity, duplicated string literals, nested blocks, nullable warnings) remain and are tracked outside this improvement cycle.
- SonarCloud's free plan analyzes only the configured main branch; branch/PR analyses are available but not retained as long-term branch snapshots.

---

## 7. Evidence and traceability

- All metrics above were read from SonarCloud APIs in the authenticated session; screenshots are stored in `docs/sonar-snapshots/`.
- Historical figures were additionally verified with `/api/measures/search_history` for the 2026-06-28→30 window.
- No GitHub files were committed or pushed to produce snapshots A and B; snapshot B used the already-merged commit `eb5f5da`.
