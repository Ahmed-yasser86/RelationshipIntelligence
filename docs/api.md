# API

Base `http://localhost:5156`. Global auth filter: every endpoint
requires JWT except `Account/PostLogin` and `PostRegister`. JSON
bodies required on all POST/PUT (empty `{}` when no payload — global
`Consumes` filter 415s bodiless posts).

| Controller | Key endpoints | Notes |
|---|---|---|
| Account | `PostRegister`, `PostLogin`, `PostLogout` | Returns JWT + refresh token; passwords via Identity |
| Contacts | `GetRelationshipQueue?top=`, `GetInteractionsForContact`, `GetRelationshipMemory`, `QueryContactsByCompositeFilter`, `GetContactsFilteredByBatches`, `GetContactsGrid`, `GetOrganizations`, `PostQuickAddContact`, `PostLogInteraction`, `PostImportInteractions` | Queue is the canonical state feed; composite filter ANDs all fields incl. interaction evidence |
| Copilot | `PostAgentChat`, `PostAsk`, `PostSummarize`, `PostBriefing`, `PostSuggestPlan`, `PostParseOutreachIntent`, `GetAiSettings`, `PutAiSettings` | `PostAgentChat` is the production path; others are legacy helpers retained by clients |
| Digest | `GetWeeklyDigest`, `GetDigestPreference`, `GetDigestPreview`, `PostSendDigest`, `DigestAction?token=&action=` | Action links HMAC-signed, 7-day expiry |
| Ingestion | `PostSubmit`, `PostSubmitForMeeting`, `PostProcess`, `GetBatch(s)`, `GetPending`, `PutReview`, `PostApproveFinding/Person/Batch`, `DeleteBatch` | Idempotent submit; process needs provider (503 when down) |
| Meeting | prep/draft/begin-logging/transcript/process/persons/finding/brief/confirm/delete | Lifecycle states enforced server-side |
| Network | `GetNetworkGraph` | Nodes/edges/cluster count; weights dropped |
| Outreach | `PostBatchFromSignals/Persons/NL`, `PutBatchSettings/Member`, `GenerateDrafts`, `ReviewDraft`, `Approve`, `DiscardBatch` | Approval never contacts anyone |
| Preference | `Get/PutPreference`, `PostReminder/Snooze/Skip/Complete`, `DeleteReminder/DeletePreference`, `GetReminderState/PreferenceHistory/GlobalDefaults`, `PutGlobalDefaults` | `PostComplete` without a logged interaction returns 409 by design |

Error mapping: validation → 400, unknown id → 404, provider down →
503, provider unconfigured → 409, completion-without-interaction → 409
with guidance. Full wire shapes in `relationship-intelligence-client/
src/lib/types.ts` (camelCase) mirrored by controller DTOs.
