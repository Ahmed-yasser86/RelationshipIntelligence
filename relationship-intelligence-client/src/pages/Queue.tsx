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

function readSnoozed(): Record<string, number> {
  try {
    return JSON.parse(localStorage.getItem(SNOOZE_KEY) ?? "{}") as Record<string, number>;
  } catch {
    return {};
  }
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
  const [snoozed, setSnoozed] = useState<Record<string, number>>(() => readSnoozed());
  const [explaining, setExplaining] = useState<RelationshipHealth | null>(null);
  const [evidence, setEvidence] = useState<InteractionResponse[]>([]);

  const load = useCallback(async () => {
    setError(null);
    try {
      const queue = await api.get<RelationshipHealth[]>(
        "/api/Contacts/GetRelationshipQueue?top=7",
      );
      setItems(queue);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load the queue.");
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  function snooze(id: string) {
    setSnoozed((prev) => {
      const next = { ...prev, [id]: Date.now() };
      localStorage.setItem(SNOOZE_KEY, JSON.stringify(next));
      return next;
    });
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
      return Date.now() - since > SNOOZE_DAYS * 86_400_000;
    }) ?? null;

  return (
    <div>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Needs attention</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Ranked by how far each relationship has drifted past its own rhythm —
            not by recency alone. Log, explain, or snooze each one right here;
            snoozed items return in {SNOOZE_DAYS} days.
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
          body="Every relationship is within its natural rhythm — or everything left is snoozed. Log interactions as they happen and drift surfaces here weeks before you would notice it."
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
                    {item.isBridge && <Badge variant="secondary">Bridge</Badge>}
                    {item.isImportant && <Badge variant="secondary">Key</Badge>}
                    {item.evidenceStatus === "Insufficient" && (
                      <Badge variant="outline" title={`Scored from ${item.interactionCount} logged interaction(s) — an early estimate, not a measured rhythm`}>
                        Early estimate
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
