import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ExplainDrawer } from "@/components/explain-drawer";
import { BandBadge, EmptyState, ErrorState, LoadingList, NavButton, PersonAvatar, UrgencyBar } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { daysSince, timeAgo } from "@/lib/format";
import type { InteractionResponse, RelationshipHealth } from "@/lib/types";
import { InteractionTypes } from "@/lib/types";

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

function cadenceLine(item: RelationshipHealth): string {
  const silent = daysSince(item.lastContactAtUtc);
  const rhythm =
    item.cadenceReferenceDays != null
      ? `roughly every ${Math.round(item.cadenceReferenceDays)} days`
      : "no rhythm established yet";
  const quiet = silent == null ? "no contact recorded" : `quiet for ${silent}d`;
  return `Your rhythm: ${rhythm} — ${quiet}. Last contact ${timeAgo(item.lastContactAtUtc)}.`;
}

function QuickLog({
  person,
  onDone,
}: {
  person: RelationshipHealth;
  onDone: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [type, setType] = useState("1");
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const data = new FormData(e.currentTarget);
    const formTitle = (data.get("title") ?? "").toString().trim();
    setError(null);
    setBusy(true);
    try {
      await api.post("/api/Contacts/PostLogInteraction", {
        PersonId: person.personId,
        TimeOfInteraction: new Date().toISOString(),
        InteractionType: Number(type),
        InteractionTitle: formTitle,
        InteractionDescription: null,
      });
      setOpen(false);
      setTitle("");
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not log.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger
        render={<Button size="sm" variant="outline">Log</Button>}
      />
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Log contact with {person.name ?? "this contact"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={submit} className="flex flex-col gap-3">
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor={`ql-type-${person.personId}`}>Type</Label>
              <Select value={type} onValueChange={(v) => setType(v ?? "1")}>
                <SelectTrigger id={`ql-type-${person.personId}`}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {InteractionTypes.map((t, i) => (
                    <SelectItem key={t} value={String(i)}>
                      {t}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor={`ql-title-${person.personId}`}>What was it about</Label>
              <Input
                id={`ql-title-${person.personId}`}
                name="title"
                required
                maxLength={100}
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </div>
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
          <DialogFooter>
            <Button type="submit" disabled={busy}>
              {busy ? "Saving…" : "Save"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export function Queue() {
  const [items, setItems] = useState<RelationshipHealth[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [top, setTop] = useState(PAGE_TOP);
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

  return (
    <div>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Needs attention</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Ranked by how far each relationship has drifted past its own rhythm —
            not by recency alone. Log, explain, or snooze each one right here;
            snoozed items return in {SNOOZE_DAYS} days on this device.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => void load()}>
          Refresh
        </Button>
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
            <li key={`${item.personId}-${i}`} className="rounded-lg border px-4 py-3">
              <div className="flex items-center gap-3">
                <span className="w-5 shrink-0 text-sm tabular-nums text-muted-foreground">
                  {i + 1}
                </span>
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
                <QuickLog person={item} onDone={() => void load()} />
                <Button size="sm" variant="ghost" onClick={() => void explain(item)}>
                  Explain
                </Button>
                <Button size="sm" variant="ghost" onClick={() => snooze(item.personId)}>
                  Snooze
                </Button>
                <NavButton to={`/people/${item.personId}`} size="sm" variant="ghost">
                  Open
                </NavButton>
              </div>
            </li>
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
