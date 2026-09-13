import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { EvidenceChip } from "@/components/evidence-chip";
import { api } from "@/lib/api";
import { daysSince, formatDate } from "@/lib/format";
import { EventTypes } from "@/lib/types";
import type {
  InteractionResponse,
  MemoryEntryResponse,
  NetworkEdge,
  NetworkNode,
  PersonDetail,
  PersonEvent,
  RelationshipHealth,
} from "@/lib/types";

function nextOccurrenceLabel(ev: PersonEvent): string | null {
  const src = ev.occursOn.slice(0, 10);
  const [y, m, d] = src.split("-").map(Number);
  if (!y || !m || !d) return null;
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const at = (year: number) => {
    const dim = new Date(year, m, 0).getDate();
    return new Date(year, m - 1, Math.min(d, dim));
  };
  let cand = ev.repeatsYearly ? at(today.getFullYear()) : new Date(y, m - 1, d);
  if (cand.getTime() < today.getTime()) {
    if (!ev.repeatsYearly) return null;
    cand = at(today.getFullYear() + 1);
  }
  const inDays = Math.round((cand.getTime() - today.getTime()) / 86_400_000);
  return inDays === 0 ? "today" : `in ${inDays}d`;
}

export function StoryPanel({
  person,
  state,
  neighbors,
  nodeInfo,
}: {
  person: PersonDetail;
  state: RelationshipHealth | null;
  neighbors: NetworkEdge[];
  nodeInfo: NetworkNode | null;
}) {
  const [memory, setMemory] = useState<MemoryEntryResponse[] | null>(null);
  const [events, setEvents] = useState<PersonEvent[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [m, e] = await Promise.all([
          api.get<MemoryEntryResponse[]>(`/api/Contacts/GetRelationshipMemory?personId=${person.personId}`),
          api.get<PersonEvent[]>(`/api/Contacts/GetPersonEvents?personId=${person.personId}`),
        ]);
        if (!cancelled) {
          setMemory(m);
          setEvents(e);
        }
      } catch {
        if (!cancelled) {
          setMemory([]);
          setEvents([]);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [person.personId]);

  const interactions = [...(person.interactions ?? [])].sort(
    (a, b) => +new Date(b.timeOfInteraction) - +new Date(a.timeOfInteraction),
  );
  const first = (person.interactions ?? []).length > 0
    ? [...(person.interactions ?? [])].sort(
        (a: InteractionResponse, b: InteractionResponse) =>
          +new Date(a.timeOfInteraction) - +new Date(b.timeOfInteraction),
      )[0]
    : null;

  const relationshipType = memory?.find((e) => e.kind === 1 && e.status === 0);
  const commitments = (memory ?? []).filter((e) => e.kind === 5 && e.status === 0);
  const goals = (memory ?? []).filter((e) => e.kind === 6 && e.status === 0);
  const upcoming = (events ?? [])
    .map((e) => ({ e, label: nextOccurrenceLabel(e) }))
    .filter((x) => x.label !== null)
    .slice(0, 3);

  const silent = daysSince(state?.lastContactAtUtc ?? person.interactions?.[0]?.timeOfInteraction);
  const pastRhythm =
    state?.cadenceReferenceDays != null && silent != null && silent > state.cadenceReferenceDays;

  const nextStep = upcoming.length > 0
    ? `Something is coming up (${upcoming[0].e.title} ${upcoming[0].label}) — review the context above, then reach out.`
    : commitments.length > 0
      ? `An open commitment needs you: ${commitments[0].title}.`
      : pastRhythm
        ? `Quiet for ${silent}d against a ~${Math.round(state?.cadenceReferenceDays ?? 0)}d rhythm — a check-in would fit.`
        : state != null
          ? "Within its natural rhythm — nothing is asking for action right now."
          : "Log the first interaction to start the observed history.";

  return (
    <section aria-label="Relationship story" className="mt-4 rounded-lg border bg-card px-4 py-3">
      <h2 className="mb-1 font-display text-xl font-semibold">The story so far</h2>
      <div className="mb-2 flex flex-wrap gap-1.5" aria-label="How to read this story">
        <EvidenceChip kind="user" />
        <EvidenceChip kind="observed" />
        <EvidenceChip kind="derived" />
        <EvidenceChip kind="unknown" />
      </div>
      <dl className="space-y-1.5 text-sm">
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">Who</dt>
          <dd>
            {[person.contactItemRoles?.[0]?.role, person.organizations?.[0]?.name, person.countryName].filter(Boolean).join(" · ") || "No role or organization recorded yet."}
          </dd>
        </div>
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">Relationship</dt>
          <dd>{relationshipType ? relationshipType.title : "Not described yet — add what this relationship is below."}</dd>
        </div>
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">How it started</dt>
          <dd>
            {[
              person.origin ? `Met: ${person.origin}` : null,
              first ? `first logged contact ${formatDate(first.timeOfInteraction)}` : "no logged contact yet",
            ]
              .filter(Boolean)
              .join(" · ")}
          </dd>
        </div>
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">Recently</dt>
          <dd>
            {interactions.length === 0
              ? "Nothing logged yet."
              : interactions
                  .slice(0, 2)
                  .map((i) => `${i.interactionTitle} (${formatDate(i.timeOfInteraction)})`)
                  .join(" · ")}
          </dd>
        </div>
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">State</dt>
          <dd>
            {state
              ? `${state.band} — urgency ${Math.round(state.urgencyScore)}/100, ${silent == null ? "no contact recorded" : silent === 0 ? "in touch today" : `quiet for ${silent}d`}${state.cadenceReferenceDays != null ? ` against a ~${Math.round(state.cadenceReferenceDays)}d rhythm` : ""}.`
              : "Unscored — no rhythm measured yet."}
          </dd>
        </div>
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">Coming up</dt>
          <dd>
            {upcoming.length === 0
              ? "No recorded dates approaching."
              : upcoming.map((x) => `${x.e.title} (${EventTypes[x.e.type] ?? x.e.type}) ${x.label}`).join(" · ")}
          </dd>
        </div>
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">Open items</dt>
          <dd>
            {commitments.length + goals.length === 0
              ? "No open commitments or goals."
              : [...commitments.map((c) => `Promised: ${c.title}`), ...goals.map((g) => `Goal: ${g.title}`)].join(" · ")}
          </dd>
        </div>
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">Network fit</dt>
          <dd>
            {nodeInfo == null && neighbors.length === 0
              ? "Stands apart — no shared organizations, tags, or channels."
              : `${nodeInfo?.degree ?? neighbors.length} shared-context connection(s)${nodeInfo?.isBridge ? ", an articulation point between network regions" : ""}. Shared context only — not observed contact.`}
          </dd>
        </div>
        <div className="flex gap-2">
          <dt className="w-28 shrink-0 text-muted-foreground">Consider next</dt>
          <dd>
            {nextStep}{" "}
            <Link to="/attention" className="underline">
              Open the attention queue
            </Link>
          </dd>
        </div>
      </dl>
    </section>
  );
}
