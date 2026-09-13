import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { EmptyState, ErrorState, LoadingList, NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { MeetingStatuses } from "@/lib/types";
import type { MeetingResponse, PagedResult, PersonView } from "@/lib/types";

// Participant picker: search existing people and add their canonical
// names. The user chooses — no guessing, no duplicate people from spelling
// variants. Free typing remains for genuinely new names.
function ParticipantPicker({ onAdd }: { onAdd: (name: string) => void }) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<PersonView[]>([]);
  const [open, setOpen] = useState(false);

  async function search(q: string) {
    setQuery(q);
    if (q.trim().length < 2) {
      setResults([]);
      setOpen(false);
      return;
    }
    try {
      const res = await api.get<PagedResult<PersonView>>(
        `/api/Contacts/GetContactsFilteredByBatches?QueryParamter=${encodeURIComponent(q.trim())}&SearchBy=Name&pageNumber=1&pageSize=8`,
      );
      setResults(res.items);
      setOpen(true);
    } catch {
      setResults([]);
    }
  }

  return (
    <div className="relative">
      <Input
        aria-label="Search existing contacts to add"
        value={query}
        onChange={(e) => void search(e.target.value)}
        onFocus={() => {
          if (results.length > 0) setOpen(true);
        }}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
        placeholder="Search contacts to add…"
      />
      {open && results.length > 0 && (
        <ul className="absolute z-10 mt-1 max-h-48 w-full overflow-auto rounded-md border bg-background shadow-lg">
          {results.map((p) => (
            <li key={p.personId}>
              <button
                type="button"
                className="w-full px-3 py-1.5 text-left text-sm hover:bg-secondary"
                onMouseDown={() => {
                  if (p.name) onAdd(p.name);
                  setQuery("");
                  setResults([]);
                  setOpen(false);
                }}
              >
                <span className="font-medium">{p.name ?? "Unnamed contact"}</span>
                {(p.circles?.[0]?.name || p.contactItemRoles?.[0]?.role) && (
                  <span className="block truncate text-xs text-muted-foreground">
                    {[p.contactItemRoles?.[0]?.role, p.circles?.[0]?.name].filter(Boolean).join(" · ")}
                  </span>
                )}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function StatusBadge({ status }: { status: number }) {
  const cls =
    status === 4
      ? "border-emerald-300 bg-emerald-50 text-emerald-800"
      : status === 3
        ? "border-sky-300 bg-sky-50 text-sky-800"
        : status === 5
          ? "border-border text-muted-foreground"
          : "border-amber-300 bg-amber-50 text-amber-800";
  return (
    <Badge variant="outline" className={cls}>
      {MeetingStatuses[status] ?? status}
    </Badge>
  );
}

export function Meetings() {
  const [meetings, setMeetings] = useState<MeetingResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState<"draft" | "prep" | null>(null);
  const [title, setTitle] = useState("");
  const [date, setDate] = useState("");
  const [description, setDescription] = useState("");
  const [agenda, setAgenda] = useState("");
  const [participants, setParticipants] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setError(null);
    try {
      setMeetings(await api.get<MeetingResponse[]>("/api/Meeting/GetMeetings"));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load meetings.");
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function create(e: React.FormEvent) {
    e.preventDefault();
    if (title.trim() === "" || creating === null) return;
    setBusy(true);
    try {
      const endpoint = creating === "prep" ? "/api/Meeting/PostMeetingPrep" : "/api/Meeting/PostMeetingDraft";
      await api.post(endpoint, {
        Title: title.trim(),
        OccurredAtUtc: date === "" ? new Date().toISOString() : new Date(`${date}T12:00:00`).toISOString(),
        Description: description.trim() === "" ? null : description.trim(),
        Agenda: agenda.trim() === "" ? null : agenda.trim(),
        ParticipantNames:
          participants.split(",").map((s) => s.trim()).filter(Boolean).length > 0
            ? participants.split(",").map((s) => s.trim()).filter(Boolean)
            : null,
      });
      setTitle("");
      setDate("");
      setDescription("");
      setAgenda("");
      setParticipants("");
      setCreating(null);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not create meeting.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h1 className="font-display text-3xl font-semibold tracking-tight">Meetings</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Raw meetings become structured evidence — transcript in, reviewed findings out,
            confirmed interactions into your relationships. Nothing is logged without your mapping and confirmation.
          </p>
        </div>
        {!creating && (
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={() => setCreating("prep")}>
              Plan a meeting
            </Button>
            <Button size="sm" onClick={() => setCreating("draft")}>
              Log a meeting
            </Button>
          </div>
        )}
      </div>

      {creating && (
        <form onSubmit={create} className="mb-6 flex max-w-xl flex-col gap-3 rounded-lg border p-4">
          <h2 className="text-base font-semibold">{creating === "prep" ? "Plan a meeting" : "Log a meeting"}</h2>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="mtg-title">Meeting title</Label>
            <Input id="mtg-title" value={title} maxLength={200} onChange={(e) => setTitle(e.target.value)} placeholder="e.g. Partnership discussion with Ahmed" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="mtg-date">Date</Label>
            <Input id="mtg-date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="mtg-desc">Description (optional context)</Label>
            <Textarea id="mtg-desc" rows={2} value={description} maxLength={2000} onChange={(e) => setDescription(e.target.value)} placeholder="What was this meeting about? First meeting? Follow-up?" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="mtg-agenda">Agenda {creating === "prep" ? "" : "(optional)"}</Label>
            <Textarea id="mtg-agenda" rows={2} value={agenda} maxLength={2000} onChange={(e) => setAgenda(e.target.value)} placeholder="What should be covered?" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="mtg-part">Participants</Label>
            <ParticipantPicker
              onAdd={(name) =>
                setParticipants((prev) =>
                  prev.trim() === "" ? name : `${prev.replace(/,\s*$/, "")}, ${name}`,
                )
              }
            />
            <Input id="mtg-part" value={participants} onChange={(e) => setParticipants(e.target.value)} placeholder="e.g. Salma El-Sayed, Karim Naguib" />
            <p className="text-xs text-muted-foreground">
              Search above to add existing contacts (uses canonical names), or type names separated by commas.
            </p>
          </div>
          <div className="flex gap-2">
            <Button type="submit" size="sm" disabled={busy || title.trim() === ""}>
              {busy ? "Creating…" : creating === "prep" ? "Create preparation" : "Create draft"}
            </Button>
            <Button type="button" size="sm" variant="ghost" onClick={() => setCreating(null)}>
              Cancel
            </Button>
          </div>
        </form>
      )}

      {error && <ErrorState message={error} onRetry={() => void load()} />}
      {meetings === null && !error && <LoadingList rows={4} />}
      {meetings !== null && meetings.length === 0 && (
        <EmptyState
          title="No meetings yet"
          body="Paste a transcript or a few notes and the co-pilot turns them into topics, commitments, and people — you map, review, and confirm."
        />
      )}
      {meetings !== null && meetings.length > 0 && (
        <ul className="flex flex-col gap-2">
          {meetings.map((m) => (
            <li key={m.meetingId} className="flex items-center gap-3 rounded-lg border px-4 py-3">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Link to={`/meetings/${m.meetingId}`} className="truncate text-sm font-semibold hover:underline">
                    {m.title}
                  </Link>
                  <StatusBadge status={m.status} />
                </div>
                <p className="mt-0.5 text-xs tabular-nums text-muted-foreground">
                  {formatDate(m.actualOccurredAtUtc ?? m.occurredAtUtc)}
                  {m.actualOccurredAtUtc ? " (actual)" : " (planned)"} · {m.people.length} {m.people.length === 1 ? "person" : "people"} · {m.findings.length} {m.findings.length === 1 ? "finding" : "findings"}
                </p>
              </div>
              <NavButton to={`/meetings/${m.meetingId}`} size="sm" variant="ghost">
                Open
              </NavButton>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
