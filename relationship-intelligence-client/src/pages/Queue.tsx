import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { AttentionRow } from "@/components/attention-row";
import { ExplainDrawer } from "@/components/explain-drawer";
import { QuickLog } from "@/components/quick-log";
import { BandBadge, EmptyState, ErrorState, LoadingList, PersonAvatar } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { useCopilot } from "@/lib/copilot";
import type { InteractionResponse, RelationshipHealth } from "@/lib/types";

const SNOOZE_KEY = "ri.snoozed";
const SNOOZE_DAYS = 7;
const SNOOZE_MS = SNOOZE_DAYS * 86_400_000;
const PAGE_TOP = 7;
const FULL_TOP = 50;

function readSnoozed(): Record<string, number> {
  try {
    return JSON.parse(localStorage.getItem(SNOOZE_KEY) ?? "{}") as Record<string, number>;
  } catch {
    return {};
  }
}

function pruneSnoozed(map: Record<string, number>): Record<string, number> {
  const now = Date.now();
  const next: Record<string, number> = {};
  for (const [id, since] of Object.entries(map)) {
    if (now - since <= SNOOZE_MS) next[id] = since;
  }
  return next;
}

function snoozeDaysLeft(since: number): number {
  return Math.max(1, Math.ceil((since + SNOOZE_MS - Date.now()) / 86_400_000));
}

export function Queue() {
  const { openCopilot } = useCopilot();
  const navigate = useNavigate();
  const [items, setItems] = useState<RelationshipHealth[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [top, setTop] = useState(PAGE_TOP);
  const [selected, setSelected] = useState<string[]>([]);
  const [outreachBusy, setOutreachBusy] = useState(false);
  const [snoozed, setSnoozed] = useState<Record<string, number>>(() => pruneSnoozed(readSnoozed()));
  const [explaining, setExplaining] = useState<RelationshipHealth | null>(null);
  const [evidence, setEvidence] = useState<InteractionResponse[]>([]);

  const load = useCallback(
    async (wantedTop: number = top) => {
      setError(null);
      try {
        const queue = await api.get<RelationshipHealth[]>(
          `/api/Contacts/GetRelationshipQueue?top=${wantedTop}`,
        );
        setItems(queue);
        setSnoozed((prev) => {
          const pruned = pruneSnoozed(prev);
          localStorage.setItem(SNOOZE_KEY, JSON.stringify(pruned));
          return pruned;
        });
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Could not load the queue.");
      }
    },
    [top],
  );

  useEffect(() => {
    void load();
  }, [load]);

  function persistSnoozed(next: Record<string, number>) {
    localStorage.setItem(SNOOZE_KEY, JSON.stringify(next));
    setSnoozed(next);
  }

  function snooze(id: string) {
    persistSnoozed({ ...snoozed, [id]: Date.now() });
  }

  function unsnooze(id: string) {
    const next = { ...snoozed };
    delete next[id];
    persistSnoozed(next);
  }

  async function explain(item: RelationshipHealth) {
    setExplaining(item);
    setEvidence([]);
    try {
      const rows = await api.get<InteractionResponse[]>(
        `/api/Contacts/GetInteractionsForContact?id=${item.personId}`,
      );
      setEvidence(rows);
    } catch {
      setEvidence([]);
    }
  }

  const visible =
    items?.filter((item) => {
      const since = snoozed[item.personId] ?? 0;
      return Date.now() - since > SNOOZE_MS;
    }) ?? null;
  const snoozedItems =
    items?.filter((item) => {
      const since = snoozed[item.personId] ?? 0;
      return Date.now() - since <= SNOOZE_MS;
    }) ?? [];
  const canShowMore = items !== null && items.length >= top && top < FULL_TOP;

  function showMore() {
    setTop(FULL_TOP);
  }

  function toggleSelect(id: string) {
    setSelected((s) => (s.includes(id) ? s.filter((x) => x !== id) : [...s, id]));
  }

  async function startOutreach() {
    if (selected.length === 0) return;
    setOutreachBusy(true);
    try {
      const batch = await api.post<{ outreachBatchId: string }>("/api/Outreach/PostBatchFromPersons", {
        PersonIds: selected,
        Intent: "Follow up",
      });
      navigate(`/outreach/${batch.outreachBatchId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not start outreach.");
    } finally {
      setOutreachBusy(false);
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h1 className="font-display text-3xl font-semibold tracking-tight">Needs attention</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Ranked by how far each relationship has drifted past its own rhythm —
            not by recency alone. Log, explain, or snooze each one right here;
            snoozed items return in {SNOOZE_DAYS} days on this device.
          </p>
        </div>
        <div className="flex gap-2">
          {selected.length > 0 && (
            <Button size="sm" disabled={outreachBusy} onClick={() => void startOutreach()}>
              {outreachBusy ? "Starting…" : `Contact selected (${selected.length})`}
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={() => void load()}>
            Refresh
          </Button>
        </div>
      </div>

      {error && <ErrorState message={error} onRetry={() => void load()} />}
      {visible === null && !error && <LoadingList rows={5} />}
      {visible !== null && visible.length === 0 && (
        <EmptyState
          title="Nothing needs attention"
          body={
            snoozedItems.length > 0
              ? "Every relationship is within its natural rhythm — the rest are snoozed below. Log interactions as they happen and drift surfaces here weeks before you would notice it."
              : "Every relationship is within its natural rhythm. Log interactions as they happen and drift surfaces here weeks before you would notice it."
          }
        />
      )}
      {visible !== null && visible.length > 0 && (
        <ol className="flex flex-col gap-3">
          {visible.map((item, i) => (
            <AttentionRow
              key={`${item.personId}-${i}`}
              item={item}
              rank={i + 1}
              selectable={{ selected: selected.includes(item.personId), onToggle: () => toggleSelect(item.personId) }}
              onLogDone={() => void load()}
              onExplain={(entry) => void explain(entry)}
              onAsk={(entry) => openCopilot({ personId: entry.personId, personName: entry.name })}
              onSnooze={snooze}
              logAction={(person, onDone) => <QuickLog person={person} onDone={onDone} />}
            />
          ))}
        </ol>
      )}

      {canShowMore && (
        <div className="mt-3">
          <Button variant="outline" size="sm" onClick={showMore}>
            Show more
          </Button>
        </div>
      )}

      {snoozedItems.length > 0 && (
        <section className="mt-6">
          <h2 className="mb-2 text-sm font-semibold">
            Snoozed ({snoozedItems.length})
          </h2>
          <p className="mb-3 text-xs text-muted-foreground">
            Hidden from the ranking until their snooze lapses. Bring any of them
            back early, or find them anytime under People.
          </p>
          <ul className="flex flex-col gap-2">
            {snoozedItems.map((item) => (
              <li
                key={item.personId}
                className="flex items-center gap-3 rounded-lg border border-dashed px-4 py-2.5"
              >
                <PersonAvatar name={item.name} className="h-7 w-7 text-[10px]" />
                <div className="min-w-0 flex-1">
                  <Link
                    to={`/people/${item.personId}`}
                    className="truncate text-sm font-medium hover:underline"
                  >
                    {item.name ?? "Unnamed contact"}
                  </Link>
                  <p className="text-xs text-muted-foreground">
                    Back in the ranking in ~{snoozeDaysLeft(snoozed[item.personId] ?? Date.now())}d
                    · urgency {Math.round(item.urgencyScore)}
                  </p>
                </div>
                <BandBadge band={item.band} />
                <Button size="sm" variant="ghost" onClick={() => unsnooze(item.personId)}>
                  Unsnooze
                </Button>
              </li>
            ))}
          </ul>
        </section>
      )}

      {explaining && (
        <ExplainDrawer
          health={explaining}
          interactions={evidence}
          open={explaining !== null}
          onOpenChange={(open) => {
            if (!open) setExplaining(null);
          }}
        />
      )}
    </div>
  );
}
