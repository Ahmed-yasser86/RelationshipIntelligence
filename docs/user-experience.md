# User experience: every screen, and the AI inside it

This is the UX tour: what each screen is for, what the user does there,
and exactly what the AI automates on that screen — and what it is
forbidden from doing there. Written for product owners first; engineers
find the code pins, researchers find the honesty boundaries.

The golden rule across all screens: **the system watches, organizes, and
proposes; the user decides.** Nothing on any screen writes trusted state,
contacts anyone, or records contact without an explicit user action.

## Today (`/` — Overview)

The morning screen. Three questions answered at a glance: who needs me,
what is coming, what did I ask to be reminded of.

- **Needs attention first**: top 3 of the deterministic queue, each with
  its reason ("quiet 120d vs ~30d rhythm"). Counts are computed over the
  full network (`top=200`), display slices — never a capped subset
  masquerading as a total.
- **Reminders**: user-configured schedules, labeled "You configured this
  reminder", visually separate from system-derived urgency. Both can name
  the same person; they never merge into one signal.
- **Coming up**: events ≤21 days out with silence context and a "needs
  attention too" flag only when the relationship is already drifting.
- **AI here**: the briefing block (`BriefingBlock`, `PostCopilot/PostBriefing`)
  writes a 2–3 sentence plain-language summary from queue + events +
  commitments. It narrates; it never scores. If the provider is down, a
  deterministic fallback sentence renders instead of an error.

## Attention (`/attention` — Queue)

The full ranked queue. Expand any row for the evidence drawer: rhythm,
silence, quantile ("longer than 100% of past gaps"), trajectory
snapshots, upcoming events — Observed vs Derived, always labeled.
Actions per row: log interaction, explain, ask Copilot, open the person.
A 7-day local snooze (`ri.snoozed`) hides a row on this device only;
server state is untouched, and the snooze visibly counts down.

## Things I Found (`/found`)

The review queue for AI proposals from pasted text, conversations, and
meeting notes. Hierarchy: batch → person/entity → finding. Each finding
shows confidence, source excerpt, existing-vs-proposed value,
conflict/change type, and entity-resolution candidates. Impact preview
before approval ("This will update…"). Approve at finding, person, or
batch level — unresolved items always stay pending. Nothing here affects
urgency, cadence, scores, or reminders until approved.

## People (`/people`) and Person detail (`/people/:id`)

- Directory with full composite filtering (name, email, phone,
  organization exact-match, role, tags) plus interaction-evidence
  filters (type + contacted-since). Organization links (`?org=`) show an
  explicit scope banner with the true member total.
- Detail page: trajectory chart from daily snapshots, interaction
  timeline, memory, events, channels, network position, plus the
  **preferences card** ("How often do you want to stay in touch?",
  importance, priority, intentional contact, suggestion exclusion, with
  "Why these settings?" audit history) and the **reminder card**
  (interval presets + custom, strict/flexible, snooze/skip/turn-off, with
  the explicit note that snoozing never logs contact).
- **AI here**: "Update from text" ingests free text into the review
  queue; Copilot answers person questions from tool results with
  citations.

## Add person (`/people/new`)

Three tabs: quick add, full profile, **From text** — paste a LinkedIn
profile, signature, or bio; the extractor maps it to supported fields
only and routes to `/found` for review. If the provider is down, the
batch is still saved and the user is routed to retry from review.

## Organizations (`/organizations`)

The shared directory behind every contact. Member counts link to the
scoped People view. Rename freely (members follow); deletion is refused
while contacts belong. No AI here — this is ground truth the AI reads,
never writes.

## Meetings (`/meetings`, `/meetings/:id`)

Planned → completed lifecycle with the core invariant visible in the UI:
agenda/preparation labeled PLANNED, transcript/notes labeled ACTUAL
EVIDENCE, and the agenda never auto-becomes evidence. Detail page walks
through transcript → process (extract people/findings) → map → review →
confirm, which writes real `Meeting`-type interactions plus derived
memory with provenance. "Send actual notes to review" routes transcript
+ notes (only) into the unified ingestion pipeline.

## Outreach (`/outreach`, `/outreach/:id`)

- Build from plain language ("everyone I should reconnect with") or
  from signals directly (attention queue, outside rhythm, neglected,
  recent meetings, events, commitments, company members). Cap: 12
  members; every member carries its reason; remove anyone.
- Pick one channel for the batch (individuals can override), prepare
  grounded drafts per person (or flagged thin-context), edit/regenerate,
  approve individually or as a batch.
- **AI here**: intent parsing, per-person draft generation grounded in
  that person's context with a `CONTEXT-USED` trailer, call-prep briefs.
  **Approval never contacts anyone** — it only marks reviewed. The user
  logs the real interaction afterward.

## Network (`/network`)

Shared-context graph (same org/tag/channel — never observed contact),
disconnected components (not "communities"), articulation-point bridges
with tooltips. Pure visualization of the deterministic analysis; the AI
reads it via tools but never edits it.

## Digest (`/digest`)

Weekly email: same queue selection by urgency threshold, suggestion
opt-outs respected, delivery deduplicated per week, preview-before-send,
HMAC-signed one-click action links ("I reached out" logs a real
interaction; metrics recorded either way). Plus the global-defaults
card: the fallback rhythm used only where no per-person rhythm is set.

## Copilot drawer (everywhere)

One agent, ~40 read tools + confirmation-gated writes, over owner-scoped
services — it can only ever see the caller's own data. It answers with
citations and evidence, proposes before mutating, accepts spelling
variants before claiming absence, and reports every member for org
questions. Provider settings (Gemini/OpenAI-compatible) live in setup;
secrets stay server-side, never returned by the API.
