import { useCallback, useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
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
import { Separator } from "@/components/ui/separator";
import { Textarea } from "@/components/ui/textarea";
import { ExplainDrawer } from "@/components/explain-drawer";
import {
  BandBadge,
  EmptyState,
  ErrorState,
  LoadingList,
  NavButton,
  PersonAvatar,
  UrgencyBar,
} from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { formatDate, timeAgo, daysSince } from "@/lib/format";
import type { InteractionResponse, NetworkGraph, PersonDetail as Person, RelationshipHealth } from "@/lib/types";

interface HistoryPoint {
  takenAtUtc: string;
  tieStrength: number;
  urgencyScore: number;
  band: string;
}

function Trajectory({ points }: { points: HistoryPoint[] }) {
  if (points.length < 2) {
    return (
      <p className="text-sm text-muted-foreground">
        Trajectory appears after a few days of scored history — snapshots accumulate
        nightly and on every logged interaction.
      </p>
    );
  }
  const w = 280;
  const h = 64;
  const pad = 6;
  const max = Math.max(...points.map((p) => p.urgencyScore), 100);
  const step = points.length > 1 ? (w - pad * 2) / (points.length - 1) : 0;
  const path = points
    .map(
      (p, i) =>
        `${i === 0 ? "M" : "L"}${(pad + i * step).toFixed(1)},${(h - pad - (p.urgencyScore / max) * (h - pad * 2)).toFixed(1)}`,
    )
    .join(" ");
  const first = points[0].urgencyScore;
  const last = points[points.length - 1].urgencyScore;
  const delta = Math.round(last - first);
  const trend =
    delta > 5 ? "cooling" : delta < -5 ? "warming" : "steady";
  return (
    <div>
      <svg viewBox={`0 0 ${w} ${h}`} className="w-full" role="img" aria-label="Urgency trajectory">
        <path d={path} fill="none" stroke="currentColor" strokeWidth={2} />
        {points.map((p, i) => (
          <circle
            key={i}
            cx={pad + i * step}
            cy={h - pad - (p.urgencyScore / max) * (h - pad * 2)}
            r={2.5}
            className="fill-primary"
          >
            <title>{`${formatDate(p.takenAtUtc)} — urgency ${Math.round(p.urgencyScore)} (${p.band})`}</title>
          </circle>
        ))}
      </svg>
      <p className="mt-1 text-xs text-muted-foreground">
        {trend === "steady"
          ? `Steady around ${Math.round(last)} over ${points.length} snapshots.`
          : `${trend === "cooling" ? "Cooling" : "Warming"}: urgency ${delta > 0 ? "+" : ""}${delta} points across ${points.length} snapshots.`}{" "}
        Each point is a scored daily snapshot, not a prediction.
      </p>
    </div>
  );
}
import { InteractionTypes } from "@/lib/types";

function usePerson(id: string | undefined) {
  const [person, setPerson] = useState<Person | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { key } = useLocation();

  const load = useCallback(async () => {
    if (!id) return;
    setError(null);
    try {
      const p = await api.get<Person>(`/api/Contacts/GetContactByContactID?id=${id}`);
      if (!p) throw new ApiError(404, "Person not found.");
      setPerson(p);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load person.");
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load, key]);

  return { person, error, reload: load };
}

function LogInteractionDialog({
  personId,
  onDone,
}: {
  personId: string;
  onDone: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [type, setType] = useState("1");
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const data = new FormData(e.currentTarget);
      const title = (data.get("title") ?? "").toString().trim();
      const description = (data.get("description") ?? "").toString().trim();
      await api.post("/api/Contacts/PostLogInteraction", {
        PersonId: personId,
        TimeOfInteraction: new Date(date).toISOString(),
        InteractionType: Number(type),
        InteractionTitle: title,
        InteractionDescription: description === "" ? null : description,
      });
      setOpen(false);
      setTitle("");
      setDescription("");
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not log interaction.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger render={<Button size="sm">Log interaction</Button>} />
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Log an interaction</DialogTitle>
        </DialogHeader>
        <form onSubmit={submit} className="flex flex-col gap-3">
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="log-type">Type</Label>
              <Select value={type} onValueChange={(v) => setType(v ?? "1")}>
                <SelectTrigger id="log-type">
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
              <Label htmlFor="log-date">Date</Label>
              <Input
                id="log-date"
                type="date"
                required
                value={date}
                max={new Date().toISOString().slice(0, 10)}
                onChange={(e) => setDate(e.target.value)}
              />
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="log-title">What was it about</Label>
            <Input
              id="log-title"
              name="title"
              required
              maxLength={100}
              placeholder="e.g. Q3 hiring plans"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="log-desc">Notes (optional)</Label>
            <Textarea
              id="log-desc"
              name="description"
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
          <DialogFooter>
            <Button type="submit" disabled={busy}>
              {busy ? "Saving…" : "Save interaction"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function ImportDialog({ personId, onDone }: { personId: string; onDone: () => void }) {
  const [open, setOpen] = useState(false);
  const [csv, setCsv] = useState("");
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setResult(null);
    setBusy(true);
    try {
      const count = await api.post<number>(
        `/api/Contacts/PostImportInteractions?id=${personId}`,
        { CsvText: csv },
      );
      setResult(`Imported ${count} interactions.`);
      setCsv("");
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Import failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger
        render={
          <Button size="sm" variant="outline">
            Import CSV
          </Button>
        }
      />
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Import past interactions</DialogTitle>
        </DialogHeader>
        <p className="text-xs text-muted-foreground">
          One per line: <code>date, type, title, description</code> — e.g.
          <code> 2026-07-01, Email, Kickoff, Discussed scope</code>. Types: Call,
          Email, Meeting, Message. Max 500 rows.
        </p>
        <form onSubmit={submit} className="flex flex-col gap-3">
          <Textarea
            rows={6}
            required
            placeholder={"2026-07-01, Email, Kickoff, Discussed scope"}
            value={csv}
            onChange={(e) => setCsv(e.target.value)}
          />
          {error && <p className="text-sm text-destructive">{error}</p>}
          {result && <p className="text-sm text-emerald-700">{result}</p>}
          <DialogFooter>
            <Button type="submit" disabled={busy}>
              {busy ? "Importing…" : "Import"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function Timeline({ interactions }: { interactions: InteractionResponse[] }) {
  const sorted = [...(interactions ?? [])].sort(
    (a, b) => +new Date(b.timeOfInteraction) - +new Date(a.timeOfInteraction),
  );
  if (sorted.length === 0) {
    return (
      <EmptyState
        title="No interactions recorded"
        body="Log the first interaction to start this relationship's history. The decay model needs at least a few dated events before it can say anything useful."
      />
    );
  }
  return (
    <ol className="relative ml-1.5 flex flex-col gap-0 border-l pl-5">
      {sorted.map((i) => (
        <li key={i.interactionId} className="relative pb-5 last:pb-0">
          <span className="absolute -left-5 top-1.5 h-2 w-2 -translate-x-1/2 rounded-full bg-primary" />
          <div className="flex flex-wrap items-baseline gap-x-2">
            <span className="text-sm font-semibold">{i.interactionTitle}</span>
            <Badge variant="outline" className="text-[11px]">
              {InteractionTypes[i.interactionType] ?? i.interactionType}
            </Badge>
            <span className="text-xs text-muted-foreground">
              {formatDate(i.timeOfInteraction)} · {timeAgo(i.timeOfInteraction)}
            </span>
          </div>
          {i.interactionDescription && (
            <p className="mt-0.5 text-sm text-muted-foreground">{i.interactionDescription}</p>
          )}
        </li>
      ))}
    </ol>
  );
}

export function PersonDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { person, error, reload } = usePerson(id);
  const [state, setState] = useState<RelationshipHealth | null>(null);
  const [network, setNetwork] = useState<NetworkGraph | null>(null);
  const [history, setHistory] = useState<HistoryPoint[]>([]);
  const [explaining, setExplaining] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    api
      .get<RelationshipHealth[]>("/api/Contacts/GetRelationshipQueue?top=50")
      .then((q) => setState(q.find((x) => x.personId === id) ?? null))
      .catch(() => undefined);
    api
      .get<NetworkGraph>("/api/Network/GetNetworkGraph")
      .then(setNetwork)
      .catch(() => undefined);
    if (id) {
      api
        .get<{ personId: string; points: HistoryPoint[] }>(
          `/api/Contacts/GetStateHistory?id=${id}`,
        )
        .then((h) => setHistory(h.points ?? []))
        .catch(() => undefined);
    }
  }, [id, person]);

  async function onDelete() {
    if (!id || !window.confirm(`Delete ${person?.name}? This cannot be undone.`)) return;
    setDeleting(true);
    try {
      await api.post(`/api/Contacts/DeletePersoneObject?id=${id}`);
      navigate("/people");
    } catch {
      setDeleting(false);
    }
  }

  if (error)
    return (
      <div>
        <NavButton to="/people" variant="ghost" size="sm" className="mb-4">
          ← People
        </NavButton>
        <ErrorState message={error} onRetry={() => void reload()} />
      </div>
    );
  if (!person) return <LoadingList rows={6} />;

  const neighbors = (network?.edges ?? [])
    .filter((e) => e.from === person.personId || e.to === person.personId)
    .slice(0, 8);
  const neighborName = (pid: string) =>
    network?.nodes.find((n) => n.personId === pid)?.name ?? "Unnamed contact";
  const nodeInfo = network?.nodes.find((n) => n.personId === person.personId);

  const silent = daysSince(
    [...(person.interactions ?? [])]
      .map((i) => i.timeOfInteraction)
      .sort()
      .reverse()[0],
  );

  return (
    <div>
      <NavButton to="/people" variant="ghost" size="sm" className="mb-4">
        ← People
      </NavButton>

      <div className="flex flex-wrap items-start gap-4">
        <PersonAvatar name={person.name} className="h-14 w-14 text-base" />
        <div className="min-w-0 flex-1">
          <h1 className="text-2xl font-semibold tracking-tight">{person.name}</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {[
              person.contactItemRoles?.[0]?.role,
              person.organizations?.[0]?.name ?? person.circles?.[0]?.name,
              person.countryName,
            ]
              .filter(Boolean)
              .join(" · ")}
          </p>
          <div className="mt-2 flex flex-wrap items-center gap-1.5">
            {state && (
              <>
                <BandBadge band={state.band} />
                <UrgencyBar value={state.urgencyScore} />
                {state.isBridge && <Badge variant="secondary">Bridge</Badge>}
              </>
            )}
            {(person.systemStatusTags ?? []).map((t) => (
              <Badge key={t.statusTagId} variant="secondary">
                {t.name}
              </Badge>
            ))}
            {(person.userDefinedTags ?? []).map((t) => (
              <Badge key={t.tagId} variant="outline">
                {t.tagName}
              </Badge>
            ))}
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <LogInteractionDialog personId={person.personId} onDone={() => void reload()} />
          <ImportDialog personId={person.personId} onDone={() => void reload()} />
          {state && (
            <Button size="sm" variant="outline" onClick={() => setExplaining(true)}>
              Explain score
            </Button>
          )}
          <NavButton to={`/people/${person.personId}/edit`} size="sm" variant="outline">
            Edit
          </NavButton>
          <Button size="sm" variant="destructive" disabled={deleting} onClick={() => void onDelete()}>
            {deleting ? "Deleting…" : "Delete"}
          </Button>
        </div>
      </div>

      {state && (
        <div className="mt-4 rounded-lg border bg-card px-4 py-3 text-sm">
          <span className="font-semibold">Relationship state. </span>
          <span className="text-muted-foreground">
            Rhythm roughly every{" "}
            {state.cadenceReferenceDays != null
              ? `${Math.round(state.cadenceReferenceDays)} days`
              : "— (not enough history)"}
            {silent != null ? `, quiet for ${silent}d` : ""}. Strength{" "}
            {state.tieStrength.toFixed(2)} — urgency {Math.round(state.urgencyScore)}/100.
            {state.isBridge && " This contact bridges otherwise separate parts of your network."}
          </span>
        </div>
      )}

      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_320px]">
        <section>
          <h2 className="mb-3 text-base font-semibold">Trajectory</h2>
          <Trajectory points={history} />
        </section>
      </div>
      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_320px]">
        <section>
          <h2 className="mb-3 text-base font-semibold">History</h2>
          <Timeline interactions={person.interactions ?? []} />
        </section>
        <aside className="flex min-w-0 flex-col gap-5">
          <section>
            <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Contact
            </h3>
            <dl className="space-y-1 text-sm">
              {person.email && (
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">Email</dt>
                  <dd className="truncate">{person.email}</dd>
                </div>
              )}
              {person.phone && (
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">Phone</dt>
                  <dd>{person.phone}</dd>
                </div>
              )}
              {person.address && (
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">Address</dt>
                  <dd className="truncate">{person.address}</dd>
                </div>
              )}
              {person.dateOfBirth && (
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">Born</dt>
                  <dd>
                    {formatDate(person.dateOfBirth)} ({person.age})
                  </dd>
                </div>
              )}
              {person.linkedInProfile && (
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">LinkedIn</dt>
                  <dd className="truncate">
                    <a
                      href={person.linkedInProfile}
                      target="_blank"
                      rel="noreferrer"
                      className="underline"
                    >
                      profile
                    </a>
                  </dd>
                </div>
              )}
            </dl>
          </section>
          {(person.contextMemory || person.origin || person.otherInformation) && (
            <section>
              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Memory
              </h3>
              {person.contextMemory && <p className="text-sm">{person.contextMemory}</p>}
              {person.origin && (
                <p className="mt-1 text-sm text-muted-foreground">Met: {person.origin}</p>
              )}
              {person.otherInformation && (
                <p className="mt-1 text-sm text-muted-foreground">{person.otherInformation}</p>
              )}
            </section>
          )}
          {(person.notes ?? []).length > 0 && (
            <section>
              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Notes ({person.notes.length})
              </h3>
              <ul className="flex flex-col gap-2">
                {person.notes.map((n) => (
                  <li key={n.noteId} className="rounded-md border px-3 py-2 text-sm">
                    {n.content}
                  </li>
                ))}
              </ul>
            </section>
          )}
          {(person.connectionChannels ?? []).length > 0 && (
            <section>
              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Channels
              </h3>
              <div className="flex flex-wrap gap-1.5">
                {person.connectionChannels.map((c) => (
                  <Badge key={c.connectionChannelId} variant="outline">
                    {c.connectionChannelName}
                  </Badge>
                ))}
              </div>
            </section>
          )}
          <section>
            <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Network position
            </h3>
            {nodeInfo == null && neighbors.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                No shared organizations, tags, or channels — this contact stands
                apart from the rest of your network.
              </p>
            ) : (
              <div className="flex flex-col gap-1.5">
                {nodeInfo != null && (
                  <p className="text-sm text-muted-foreground">
                    {nodeInfo.degree} direct connection{nodeInfo.degree === 1 ? "" : "s"}
                    {nodeInfo.isBridge ? " · a bridge between network regions" : ""} ·
                    urgency {Math.round(nodeInfo.urgencyScore)}.
                  </p>
                )}
                {neighbors.length > 0 && (
                  <ul className="flex flex-col gap-1">
                    {neighbors.map((e, i) => {
                      const other = e.from === person.personId ? e.to : e.from;
                      return (
                        <li key={i} className="text-sm">
                          <Link
                            to={`/people/${other}`}
                            className="font-medium hover:underline"
                          >
                            {neighborName(other)}
                          </Link>
                          <span className="text-muted-foreground"> — {e.reason}</span>
                        </li>
                      );
                    })}
                  </ul>
                )}
                <Link to={`/network?person=${person.personId}`} className="text-sm underline">
                  Open in network map →
                </Link>
              </div>
            )}
          </section>
          {(person.socialMediaAccounts ?? []).length > 0 && (
            <section>
              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Elsewhere
              </h3>
              <ul className="space-y-1 text-sm">
                {person.socialMediaAccounts.map((s) => (
                  <li key={s.socialMediaAccountId} className="truncate">
                    <a href={s.url} target="_blank" rel="noreferrer" className="underline">
                      {s.platform || s.url}
                    </a>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </aside>
      </div>
      <Separator className="my-6" />
      {state && (
        <ExplainDrawer
          health={state}
          interactions={person.interactions ?? []}
          open={explaining}
          onOpenChange={setExplaining}
        />
      )}
      <p className="text-xs text-muted-foreground">
        Strength and urgency are computed from dated interactions by an exponential
        tie-decay model — equal weight per event, 60-day half-life. Flags and bands
        are display aids, not measurements. See the project methodology document.
      </p>
    </div>
  );
}
