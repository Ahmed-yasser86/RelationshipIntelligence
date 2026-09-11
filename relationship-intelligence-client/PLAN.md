# Relationship Intelligence — Product / UI / UX Implementation Plan

Single source of truth for the frontend. The backend (`RelationshipIntelligence.Api`)
is the system of record; nothing here invents backend behavior.

## 1. Product experience

**What it is.** A relationship-intelligence instrument, not a CRM. It answers one
question continuously: *who in my network is cooling, why should I believe it,
and what exactly should I do this week?*

**Primary user.** A solo professional operator (recruiter, freelancer, consultant)
managing a few hundred relationships alone. Secondary reader: a CSS researcher
inspecting temporal relational behavior.

**Core workflows.** (a) Monday triage: open Attention, act on the top of the
queue; (b) investigate: person → history → state → network context; (c) capture:
log interactions as they happen; (d) maintain: weekly digest by email or in-app;
(e) explore: network topology for structural insight.

**Visual priority.** Urgency-ranked people first; evidence (last contact, rhythm,
history) second; CRUD chrome last. Operational CRUD (add/edit/delete) exists but
never dominates. Analytical surfaces (queue, digest, network, timeline, state
panel) are the product. Temporal intelligence (rhythm vs. silence, decay bands,
history timeline) is what differentiates this from a contact manager.

## 2. Screen map

| Screen | Route | Purpose / goal | Main actions |
|---|---|---|---|
| Sign in | `/login` | authenticate | submit credentials |
| Register | `/register` | create account | submit profile |
| Attention | `/` | triage decaying relationships | refresh, open person |
| People | `/people` | browse/search/filter/sort network | search, filter, sort, paginate, add |
| Person detail | `/people/:id` | investigate one relationship holistically | log interaction, import CSV, edit, delete |
| Person new | `/people/new` | capture a contact (quick or full) | submit quick/full form |
| Person edit | `/people/:id/edit` | correct profile data | submit changes |
| Network | `/network` | see topology, bridges, isolates | select node, open person |
| Digest | `/digest` | review + configure weekly selection | send now, save preferences, one-click action links |

Every screen specifies loading (skeletons), empty (explanatory + next action),
mutation (busy/disabled + inline errors), API-error (message + retry), 401
(global logout), and 404 (missing entity) states.

## 3. Endpoint → screen mapping

Base `api/...` route prefix implied (`api/[controller]/[action]`).

| Endpoint | Screen | Component → action | Request | Response usage |
|---|---|---|---|---|
| POST Account/PostLogin | Login | form submit | `{Email, Password}` | token → session |
| POST Account/PostRegister | Register | form submit | profile + `UserType: 0` | token → session |
| POST Account/PostLogout | Layout | sign-out | — | session cleared |
| GET Contacts/GetRelationshipQueue?top | Attention, Person detail | initial load / state badge | `top` | ranked queue; detail finds own entry |
| GET Contacts/GetContactsGrid | People | initial + pagination | pageNumber/pageSize | rows + total |
| GET Contacts/GetContactsFilteredByBatches | People | search submit | QueryParamter/SearchBy/page | rows + total |
| POST Contacts/QueryContactsByCompositeFilter | People | filter submit | filter body + page params | rows + total |
| GET Contacts/GetSearchSortedPeopleBy | People | sort change | page/size/sortBy | rows + total |
| GET Contacts/GetValidSearchFields | People | field dropdown options | — | dropdown |
| GET Contacts/GetValidSortFields | People | sort dropdown options | — | dropdown |
| GET Contacts/GetContactByContactID?id | Person detail/edit | initial load | id | full profile + history |
| POST Contacts/PostQuickAddContact | Person new | quick form | name/email/orgs/roles/origin | new id → navigate |
| POST Contacts/PostAddPersoneRequest | Person new | full form | full profile | new id → navigate |
| PUT Contacts/PutContactItemUpdateRequest | Person edit | save | PersonUpdateRequest | navigate back |
| POST Contacts/DeletePersoneObject?id | Person detail | delete (confirm) | id | navigate to list |
| GET Contacts/GetInteractionsForContact?id | Person detail | timeline section | id | timeline (also embedded in detail) |
| POST Contacts/PostLogInteraction | Person detail | log dialog | event DTO | reload detail |
| POST Contacts/PostImportInteractions?id | Person detail | CSV dialog | `{CsvText}` | count → reload |
| GET Contacts/GetAllCountries | Person new/edit | country dropdown | — | dropdown |
| GET Contacts/GetSystemStatusTags | — | NOT exposed | — | lookup-only; surfaced as badges, no screen (documented decision) |
| GET Network/GetNetworkGraph | Network | initial load | — | SVG clustered graph |
| GET Digest/GetWeeklyDigest | Digest | initial load | — | entries + health |
| POST Digest/PostSendDigest | Digest | send now | — | result message |
| GET/POST Digest preference | Digest | preferences form | preference DTO | form state |
| GET Digest/DigestAction (anonymous) | Digest, email | one-click link (new tab) | signed token+action | records interaction server-side |

## 4. Information architecture

Primary nav (sidebar): **Attention → People → Network → Digest**, in triage order.
No controller-mirroring routes. Person detail is the hub: profile, state panel,
timeline, memory, channels, notes, and actions co-located; queue/digest/network
all deep-link to it with context preserved (back links return to the list).
Preferences live inside Digest (the only consumer). Global search is the People
screen itself (server-side, all fields). Auth screens stand alone.

## 5. Design direction

Calm research instrument: neutral palette with a deep-teal primary, dense ranked
lists over cards, urgency shown as band label + thin bar (never color alone),
timelines with absolute + relative dates, SVG network grouped by component with
bridge rings. No KPI dashboards, no decorative charts, no fake analytics. Every
score is shown next to its evidence (last contact, rhythm, strength) and the
methodology note is one click away in copy.

## 6. Tech summary

React 19 + Vite + TypeScript + Tailwind v4 + shadcn/ui (Base UI primitives,
installed from the shadcn registry; `cn`/`buttonVariants` shared helpers).
No MCB existed in the repo and the prior bare-Angular client was deleted, so
shadcn/ui was adopted (shadcn MCP server provisioned and verified). API layer
isolated in `lib/api.ts` (token injection, 401 → global logout, typed errors);
auth in `lib/auth.tsx`; routing guards in `App.tsx`; shared states/indicators in
`components/states.tsx`. Backend owns all scoring logic; the client renders
`urgency/band/strength` as received plus purely presentational formatting.

## 7. Validation

`tsc --noEmit` clean, `vite build` succeeds. Manual API-integration check against
local backend recommended (login → queue → person → log → digest → network).
