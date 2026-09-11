import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
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
import type { InteractionResponse, PersonDetail as Person, RelationshipHealth } from "@/lib/types";
import { InteractionTypes } from "@/lib/types";

function usePerson(id: string | undefined) {
  const [person, setPerson] = useState<Person | null>(null);
  const [error, setError] = useState<string | null>(null);

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
  }, [load]);

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

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await api.post("/api/Contacts/PostLogInteraction", {
        PersonId: personId,
        TimeOfInteraction: new Date(date).toISOString(),
        InteractionType: Number(type),
        InteractionTitle: title.trim(),
        InteractionDescription: description.trim() === "" ? null : description.trim(),
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
              <Label>Type</Label>
              <Select value={type} onValueChange={(v) => setType(v ?? "1")}>
                <SelectTrigger>
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
    (a, b) => +new Date(b.TimeOfInteraction) - +new Date(a.TimeOfInteraction),
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
        <li key={i.InteractionId} className="relative pb-5 last:pb-0">
          <span className="absolute -left-5 top-1.5 h-2 w-2 -translate-x-1/2 rounded-full bg-primary" />
          <div className="flex flex-wrap items-baseline gap-x-2">
            <span className="text-sm font-semibold">{i.InteractionTitle}</span>
            <Badge variant="outline" className="text-[11px]">
              {InteractionTypes[i.InteractionType] ?? i.InteractionType}
            </Badge>
            <span className="text-xs text-muted-foreground">
              {formatDate(i.TimeOfInteraction)} · {timeAgo(i.TimeOfInteraction)}
            </span>
          </div>
          {i.InteractionDescription && (
            <p className="mt-0.5 text-sm text-muted-foreground">{i.InteractionDescription}</p>
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
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    api
      .get<RelationshipHealth[]>("/api/Contacts/GetRelationshipQueue?top=50")
      .then((q) => setState(q.find((x) => x.PersonId === id) ?? null))
      .catch(() => undefined);
  }, [id, person]);

  async function onDelete() {
    if (!id || !window.confirm(`Delete ${person?.Name}? This cannot be undone.`)) return;
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

  const silent = daysSince(
    [...(person.Interactions ?? [])]
      .map((i) => i.TimeOfInteraction)
      .sort()
      .reverse()[0],
  );

  return (
    <div>
      <NavButton to="/people" variant="ghost" size="sm" className="mb-4">
        ← People
      </NavButton>

      <div className="flex flex-wrap items-start gap-4">
        <PersonAvatar name={person.Name} className="h-14 w-14 text-base" />
        <div className="min-w-0 flex-1">
          <h1 className="text-2xl font-semibold tracking-tight">{person.Name}</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {[
              person.ContactItemRoles?.[0]?.Role,
              person.Organizations?.[0]?.Name ?? person.Circles?.[0]?.Name,
              person.CountryName,
            ]
              .filter(Boolean)
              .join(" · ")}
          </p>
          <div className="mt-2 flex flex-wrap items-center gap-1.5">
            {state && (
              <>
                <BandBadge band={state.Band} />
                <UrgencyBar value={state.UrgencyScore} />
                {state.IsBridge && <Badge variant="secondary">Bridge</Badge>}
              </>
            )}
            {(person.SystemStatusTags ?? []).map((t) => (
              <Badge key={t.StatusTagId} variant="secondary">
                {t.Name}
              </Badge>
            ))}
            {(person.UserDefinedTags ?? []).map((t) => (
              <Badge key={t.TagId} variant="outline">
                {t.TagName}
              </Badge>
            ))}
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <LogInteractionDialog personId={person.PersonId} onDone={() => void reload()} />
          <ImportDialog personId={person.PersonId} onDone={() => void reload()} />
          <NavButton to={`/people/${person.PersonId}/edit`} size="sm" variant="outline">
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
            {state.CadenceReferenceDays != null
              ? `${Math.round(state.CadenceReferenceDays)} days`
              : "— (not enough history)"}
            {silent != null ? `, quiet for ${silent}d` : ""}. Strength{" "}
            {state.TieStrength.toFixed(2)} — urgency {Math.round(state.UrgencyScore)}/100.
            {state.IsBridge && " This contact bridges otherwise separate parts of your network."}
          </span>
        </div>
      )}

      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_320px]">
        <section>
          <h2 className="mb-3 text-base font-semibold">History</h2>
          <Timeline interactions={person.Interactions ?? []} />
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
              {person.Address && (
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">Address</dt>
                  <dd className="truncate">{person.Address}</dd>
                </div>
              )}
              {person.DateOfBirth && (
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">Born</dt>
                  <dd>
                    {formatDate(person.DateOfBirth)} ({person.Age})
                  </dd>
                </div>
              )}
              {person.LinkedInProfile && (
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">LinkedIn</dt>
                  <dd className="truncate">
                    <a
                      href={person.LinkedInProfile}
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
          {(person.ContextMemory || person.Origin || person.OtherInformation) && (
            <section>
              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Memory
              </h3>
              {person.ContextMemory && <p className="text-sm">{person.ContextMemory}</p>}
              {person.Origin && (
                <p className="mt-1 text-sm text-muted-foreground">Met: {person.Origin}</p>
              )}
              {person.OtherInformation && (
                <p className="mt-1 text-sm text-muted-foreground">{person.OtherInformation}</p>
              )}
            </section>
          )}
          {(person.Notes ?? []).length > 0 && (
            <section>
              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Notes ({person.Notes.length})
              </h3>
              <ul className="flex flex-col gap-2">
                {person.Notes.map((n) => (
                  <li key={n.NoteId} className="rounded-md border px-3 py-2 text-sm">
                    {n.Content}
                  </li>
                ))}
              </ul>
            </section>
          )}
          {(person.ConnectionChannels ?? []).length > 0 && (
            <section>
              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Channels
              </h3>
              <div className="flex flex-wrap gap-1.5">
                {person.ConnectionChannels.map((c) => (
                  <Badge key={c.ConnectionChannelId} variant="outline">
                    {c.ConnectionChannelName}
                  </Badge>
                ))}
              </div>
            </section>
          )}
          {(person.SocialMediaAccounts ?? []).length > 0 && (
            <section>
              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Elsewhere
              </h3>
              <ul className="space-y-1 text-sm">
                {person.SocialMediaAccounts.map((s) => (
                  <li key={s.SocialMediaAccountId} className="truncate">
                    <a href={s.Url} target="_blank" rel="noreferrer" className="underline">
                      {s.Platform || s.Url}
                    </a>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </aside>
      </div>
      <Separator className="my-6" />
      <p className="text-xs text-muted-foreground">
        Strength and urgency are computed from dated interactions by an exponential
        tie-decay model — equal weight per event, 60-day half-life. Flags and bands
        are display aids, not measurements. See the project methodology document.
      </p>
    </div>
  );
}
