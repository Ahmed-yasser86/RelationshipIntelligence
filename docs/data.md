# Data

## Tables (31 `DbSet`s, `AppDBContext.cs:23-53`)

People and evidence: `Persons`, `Countries`, `ContactItemRoles`,
`ConnectionChannels`, `ContactChannels` (join), `Notes`, `Interactions`,
`SocialMediaAccounts`, plus lookup joins (`PersonCircles`,
`PersonSocialMediaAccounts`, tag joins). Relationship intelligence:
`RelationshipStates`, `RelationshipStateSnapshots`, `DigestDeliveries`,
`DigestMetrics`, `DigestPreferences`, `RelationshipMemoryEntries`,
`RelationshipEvents`. AI and workflow: `AiProviderSettings`, `Meetings`,
`MeetingPersons`, `MeetingFindings`, `MeetingBriefs`, `OutreachBatches`,
`OutreachBatchMembers`, `CommunicationDrafts`, `IngestionBatches`,
`IngestionFindings`, `RelationshipPreferences`, `PreferenceAuditEntries`,
`GlobalPreferenceDefaults`. Plus ASP.NET Identity tables.

## Isolation

Every user table carries `HasQueryFilter(x => x.ApplicationUserId ==
_currentUserId)` (captured from `ICurrentUserService` in the context
constructor), plus `&& !IsDeleted` on `Persons`. Cross-user reads are
impossible through the context; ownership filters on person/meeting
controllers add a second check. (`AppDBContext.cs:15-21,76-233`)

## Seeds and migrations

- 30 migrations; newest `20260914155802_PreferenceAuditAndGlobalDefaults`
  (audit + global defaults tables), previous `20260914121700_IngestionAndPreferences`.
- Static seeds: 1 user (`testuser@contactsmanager.dev`), 10 countries,
  channels, tags, circles, social accounts, 10 persons, 10 roles/notes/
  interactions (June 2026). The stored password hash is stale — e2e logs
  in with `Test123!` via helpers, not the documented old password.
- Runtime demo seeder (`DemoWorkspaceService`): 11 realistic contacts
  (Proceedit staff, Cairo network) with interactions/memories/events;
  requires auth, refuses when ≥5 persons exist, clears only its own rows.

## Retention

Snapshots: one row per person per day, skipped for `NoHistory`.
Digest deliveries: one row per week (selection snapshot as JSON).
Audit entries: append-only, never updated or deleted by the service.
No automatic pruning besides the 7-day digest-action token expiry.
