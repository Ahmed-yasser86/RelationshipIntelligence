import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { ApiError, api } from "@/lib/api";
import { EventTypes } from "@/lib/types";
import type { PersonEvent } from "@/lib/types";

function formatOccurs(e: PersonEvent): string {
  const d = new Date(e.occursOn.length <= 10 ? `${e.occursOn}T00:00:00` : e.occursOn);
  const label = Number.isNaN(+d)
    ? e.occursOn
    : d.toLocaleDateString(undefined, { month: "short", day: "numeric", year: e.repeatsYearly ? undefined : "numeric" });
  return e.repeatsYearly ? `Every year · ${label}` : label;
}

export function EventsSection({ personId }: { personId: string }) {
  const [events, setEvents] = useState<PersonEvent[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [type, setType] = useState("0");
  const [title, setTitle] = useState("");
  const [date, setDate] = useState("");
  const [yearly, setYearly] = useState(true);
  const [importance, setImportance] = useState("2");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [editing, setEditing] = useState<PersonEvent | null>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      setEvents(await api.get<PersonEvent[]>(`/api/Contacts/GetPersonEvents?personId=${personId}`));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load events.");
    }
  }, [personId]);

  useEffect(() => {
    void load();
  }, [load]);

  function reset() {
    setType("0");
    setTitle("");
    setDate("");
    setYearly(true);
    setImportance("2");
    setNotes("");
    setAdding(false);
    setEditing(null);
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (title.trim() === "" || date === "") return;
    setBusy(true);
    try {
      const payload = {
        Type: Number(editing ? editing.type : type),
        Title: title.trim(),
        OccursOn: date,
        RepeatsYearly: yearly,
        Importance: Number(importance),
        Notes: notes.trim() === "" ? null : notes.trim(),
      };
      if (editing) {
        await api.put("/api/Contacts/PutEvent", { ...payload, EventId: editing.eventId });
      } else {
        await api.post("/api/Contacts/PostEvent", { ...payload, PersonId: personId });
      }
      reset();
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save.");
    } finally {
      setBusy(false);
    }
  }

  function startEdit(ev: PersonEvent) {
    setEditing(ev);
    setAdding(true);
    setType(String(ev.type));
    setTitle(ev.title);
    setDate(ev.occursOn.slice(0, 10));
    setYearly(ev.repeatsYearly);
    setImportance(String(ev.importance));
    setNotes(ev.notes ?? "");
  }

  async function remove(ev: PersonEvent) {
    if (!window.confirm(`Delete "${ev.title}"?`)) return;
    try {
      await api.del(`/api/Contacts/DeleteEvent?id=${ev.eventId}`);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not delete.");
    }
  }

  return (
    <section>
      <div className="mb-1.5 flex items-center justify-between">
        <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          Important events
        </h3>
        {!adding && (
          <Button size="sm" variant="ghost" onClick={() => { reset(); setAdding(true); }}>
            Add
          </Button>
        )}
      </div>

      {adding && (
        <form onSubmit={submit} className="mb-3 grid grid-cols-2 gap-2 rounded-lg border p-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`ev-type-${personId}`}>Type</Label>
            <Select value={type} onValueChange={(v) => setType(v ?? "0")}>
              <SelectTrigger id={`ev-type-${personId}`}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {EventTypes.map((t, i) => (
                  <SelectItem key={t} value={String(i)}>
                    {t}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`ev-title-${personId}`}>Title</Label>
            <Input
              id={`ev-title-${personId}`}
              value={title}
              maxLength={200}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. Birthday"
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`ev-date-${personId}`}>Date</Label>
            <Input id={`ev-date-${personId}`} type="date" value={date} onChange={(e) => setDate(e.target.value)} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`ev-imp-${personId}`}>Importance</Label>
            <Select value={importance} onValueChange={(v) => setImportance(v ?? "2")}>
              <SelectTrigger id={`ev-imp-${personId}`}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="1">Nice to know</SelectItem>
                <SelectItem value="2">Important</SelectItem>
                <SelectItem value="3">Must not miss</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <label className="col-span-2 flex items-center gap-2 text-sm">
            <input type="checkbox" checked={yearly} onChange={(e) => setYearly(e.target.checked)} />
            Repeats every year
          </label>
          <div className="col-span-2 flex flex-col gap-1.5">
            <Label htmlFor={`ev-notes-${personId}`}>Notes (optional)</Label>
            <Textarea id={`ev-notes-${personId}`} rows={2} value={notes} maxLength={1000} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <div className="col-span-2 flex gap-2">
            <Button type="submit" size="sm" disabled={busy || title.trim() === "" || date === ""}>
              {busy ? "Saving…" : editing ? "Save changes" : "Add event"}
            </Button>
            <Button type="button" size="sm" variant="ghost" onClick={reset}>
              Cancel
            </Button>
          </div>
        </form>
      )}

      {error && <p className="mb-2 text-xs text-destructive">{error}</p>}
      {events === null && !error && <p className="text-xs text-muted-foreground">Loading…</p>}
      {events !== null && events.length === 0 && !adding && (
        <p className="text-xs text-muted-foreground">No dates recorded — birthdays and milestones surface here before they matter.</p>
      )}
      {events !== null && events.length > 0 && (
        <ul className="flex flex-col gap-2">
          {events.map((ev) => (
            <li key={ev.eventId} className="rounded-lg border px-3 py-2">
              <div className="flex flex-wrap items-center gap-1.5">
                <span className="text-sm font-medium">{ev.title}</span>
                <Badge variant="outline">{EventTypes[ev.type] ?? ev.type}</Badge>
                {ev.importance === 3 && (
                  <Badge variant="outline" className="border-red-300 bg-red-50 text-red-800">
                    Must not miss
                  </Badge>
                )}
              </div>
              <p className="mt-0.5 text-xs tabular-nums text-muted-foreground">{formatOccurs(ev)}</p>
              {ev.notes && <p className="mt-0.5 text-xs text-muted-foreground">{ev.notes}</p>}
              <div className="mt-1 flex gap-1.5">
                <Button size="sm" variant="ghost" onClick={() => startEdit(ev)}>
                  Edit
                </Button>
                <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => void remove(ev)}>
                  Delete
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
