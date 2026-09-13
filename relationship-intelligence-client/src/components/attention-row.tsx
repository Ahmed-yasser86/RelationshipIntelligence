import { useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { BandBadge, NavButton, PersonAvatar, UrgencyBar } from "@/components/states";
import { RhythmBar } from "@/components/explain-drawer";
import { api } from "@/lib/api";
import { formatDate, silenceDays, timeAgo } from "@/lib/format";
import type { InteractionResponse, MemoryEntryResponse, RelationshipHealth } from "@/lib/types";

export function cadenceLine(item: RelationshipHealth): string {
  const silent = silenceDays(item.silenceDays, item.lastContactAtUtc);
  const rhythm =
    item.cadenceReferenceDays != null
      ? `roughly every ${Math.round(item.cadenceReferenceDays)} days`
      : "no rhythm established yet";
  const quiet = silent == null ? "no contact recorded" : `quiet for ${silent}d`;
  return `Your rhythm: ${rhythm} — ${quiet}. Last contact ${timeAgo(item.lastContactAtUtc)}.`;
}

export function AttentionRow({
  item,
  rank,
  selectable,
  onLogDone,
  onExplain,
  onAsk,
  onSnooze,
  logAction,
}: {
  item: RelationshipHealth;
  rank?: number;
  selectable?: { selected: boolean; onToggle: () => void };
  onLogDone: () => void;
  onExplain: (item: RelationshipHealth) => void;
  onAsk: (item: RelationshipHealth) => void;
  onSnooze?: (id: string) => void;
  logAction: (person: RelationshipHealth, onDone: () => void) => React.ReactNode;
}) {
  const [expanded, setExpanded] = useState(false);
  const [evidence, setEvidence] = useState<InteractionResponse[] | null>(null);
  const [memory, setMemory] = useState<MemoryEntryResponse[] | null>(null);
  const [loadingEvidence, setLoadingEvidence] = useState(false);

  async function toggle() {
    const next = !expanded;
    setExpanded(next);
    if (next && evidence === null && !loadingEvidence) {
      setLoadingEvidence(true);
      try {
        const [interactions, entries] = await Promise.all([
          api.get<InteractionResponse[]>(`/api/Contacts/GetInteractionsForContact?id=${item.personId}`),
          api.get<MemoryEntryResponse[]>(`/api/Contacts/GetRelationshipMemory?personId=${item.personId}`),
        ]);
        setEvidence(interactions);
        setMemory(entries);
      } catch {
        setEvidence([]);
        setMemory([]);
      } finally {
        setLoadingEvidence(false);
      }
    }
  }

  const silent = silenceDays(item.silenceDays, item.lastContactAtUtc);
  const recent = [...(evidence ?? [])]
    .sort((a, b) => +new Date(b.timeOfInteraction) - +new Date(a.timeOfInteraction))
    .slice(0, 3);
  const openItems = (memory ?? []).filter((m) => m.status === 0 && (m.kind === 5 || m.kind === 6));

  return (
    <li className="rounded-xl border bg-card px-4 py-3 transition-colors duration-150 hover:border-foreground/20">
      <div className="flex items-center gap-3">
        {selectable && (
          <input
            type="checkbox"
            aria-label={`Select ${item.name ?? "contact"} for outreach`}
            checked={selectable.selected}
            onChange={selectable.onToggle}
          />
        )}
        {rank != null && (
          <span className="w-5 shrink-0 text-sm tabular-nums text-muted-foreground">
            {rank}
          </span>
        )}
        <PersonAvatar name={item.name} />
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <Link
              to={`/people/${item.personId}`}
              className="truncate text-sm font-semibold hover:underline"
            >
              {item.name ?? "Unnamed contact"}
            </Link>
            <BandBadge band={item.band} />
            {item.isBridge && (
              <Badge
                variant="secondary"
                title="Articulation point (bridge) — this contact connects otherwise separate parts of your network. Structural flag, not a risk score."
              >
                Bridge
              </Badge>
            )}
            {item.isImportant && (
              <Badge
                variant="secondary"
                title="Key — this contact carries a High Priority or Urgent status tag"
              >
                Key
              </Badge>
            )}
            {item.evidenceStatus === "Insufficient" && (
              <Badge variant="outline" title={`Scored from ${item.interactionCount} logged interaction(s) — an early estimate, not a measured rhythm`}>
                Early estimate
              </Badge>
            )}
            {(item.upcomingEvents ?? []).length > 0 && (
              <Badge
                variant="outline"
                className="border-amber-300 bg-amber-50 text-amber-800"
                title={(item.upcomingEvents ?? [])
                  .map((e) => `${e.title} in ${e.inDays}d`)
                  .join(" · ")}
              >
                {item.upcomingEvents[0].title}{" "}
                {item.upcomingEvents[0].inDays === 0
                  ? "today"
                  : `in ${item.upcomingEvents[0].inDays}d`}
                {item.upcomingEvents.length > 1 ? ` +${item.upcomingEvents.length - 1}` : ""}
              </Badge>
            )}
          </div>
          <p className="mt-0.5 truncate text-xs text-muted-foreground">
            {cadenceLine(item)}
          </p>
        </div>
        <UrgencyBar value={item.urgencyScore} />
      </div>
      <div className="mt-2 flex flex-wrap gap-1.5 pl-8">
        {logAction(item, onLogDone)}
        <Button size="sm" variant="ghost" onClick={() => onExplain(item)}>
          Explain
        </Button>
        <Button size="sm" variant="ghost" onClick={() => onAsk(item)}>
          Ask
        </Button>
        {onSnooze && (
          <Button size="sm" variant="ghost" onClick={() => onSnooze(item.personId)}>
            Snooze
          </Button>
        )}
        <NavButton to={`/people/${item.personId}`} size="sm" variant="ghost">
          Open
        </NavButton>
        <Button
          size="sm"
          variant="ghost"
          aria-expanded={expanded}
          onClick={() => void toggle()}
        >
          {expanded ? "Hide details" : "Details"}
        </Button>
      </div>
      {expanded && (
        <div className="mt-3 rounded-lg border bg-background px-3 py-2.5">
          {loadingEvidence && <p className="text-xs text-muted-foreground">Loading evidence…</p>}
          {!loadingEvidence && (
            <div className="flex flex-col gap-2.5">
              <div>
                <p className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                  Silence against rhythm
                </p>
                <RhythmBar silence={silent} rhythm={item.cadenceReferenceDays} band={item.band} />
              </div>
              <div>
                <p className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                  Recent evidence
                </p>
                {recent.length === 0 ? (
                  <p className="text-xs text-muted-foreground">No logged interactions yet.</p>
                ) : (
                  <ul className="space-y-0.5">
                    {recent.map((r) => (
                      <li key={r.interactionId} className="text-xs tabular-nums">
                        {r.interactionTitle}{" "}
                        <span className="text-muted-foreground">· {formatDate(r.timeOfInteraction)}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              {openItems.length > 0 && (
                <div>
                  <p className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                    Open commitments &amp; goals
                  </p>
                  <ul className="space-y-0.5">
                    {openItems.map((m) => (
                      <li key={m.memoryEntryId} className="text-xs">
                        {m.title}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              <div>
                <Link to={`/people/${item.personId}`} className="text-xs underline">
                  Open full relationship story →
                </Link>
              </div>
            </div>
          )}
        </div>
      )}
    </li>
  );
}
