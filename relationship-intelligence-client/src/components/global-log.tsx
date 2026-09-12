import { useState } from "react";
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
import { ApiError, api } from "@/lib/api";
import type { PagedResult, PersonView } from "@/lib/types";
import { InteractionTypes } from "@/lib/types";

export function GlobalLog() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [matches, setMatches] = useState<PersonView[]>([]);
  const [personId, setPersonId] = useState<string | null>(null);
  const [personName, setPersonName] = useState("");
  const [type, setType] = useState("1");
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [done, setDone] = useState(false);

  async function search(q: string) {
    setQuery(q);
    setPersonId(null);
    if (q.trim() === "") {
      setMatches([]);
      return;
    }
    try {
      const params = new URLSearchParams({
        QueryParamter: q.trim(),
        SearchBy: "Name",
        pageNumber: "1",
        pageSize: "8",
      });
      const res = await api.get<PagedResult<PersonView>>(
        `/api/Contacts/GetContactsFilteredByBatches?${params}`,
      );
      setMatches(res.items);
    } catch {
      setMatches([]);
    }
  }

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!personId) {
      setError("Pick a person first.");
      return;
    }
    const data = new FormData(e.currentTarget);
    const formTitle = (data.get("title") ?? "").toString().trim();
    setError(null);
    setBusy(true);
    try {
      await api.post("/api/Contacts/PostLogInteraction", {
        PersonId: personId,
        TimeOfInteraction: new Date().toISOString(),
        InteractionType: Number(type),
        InteractionTitle: formTitle,
        InteractionDescription: null,
      });
      setDone(true);
      setTitle("");
      setQuery("");
      setMatches([]);
      setPersonId(null);
      setPersonName("");
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not log.");
    } finally {
      setBusy(false);
    }
  }

  function reset() {
    setQuery("");
    setMatches([]);
    setPersonId(null);
    setPersonName("");
    setTitle("");
    setError(null);
    setDone(false);
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        setOpen(o);
        if (!o) reset();
      }}
    >
      <DialogTrigger render={<Button size="sm">Log interaction</Button>} />
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Log an interaction</DialogTitle>
        </DialogHeader>
        {done ? (
          <p className="text-sm text-emerald-700">
            Logged. Scores refresh on the next view.
          </p>
        ) : (
          <form onSubmit={submit} className="flex flex-col gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="gl-search">Person</Label>
              <Input
                id="gl-search"
                placeholder="Type a name…"
                value={personName !== "" && personId ? personName : query}
                onChange={(e) => {
                  setPersonName("");
                  void search(e.target.value);
                }}
              />
              {matches.length > 0 && !personId && (
                <ul className="max-h-40 overflow-auto rounded-md border">
                  {matches.map((m) => (
                    <li key={m.personId}>
                      <button
                        type="button"
                        className="w-full px-3 py-1.5 text-left text-sm hover:bg-secondary"
                        onClick={() => {
                          setPersonId(m.personId);
                          setPersonName(m.name ?? "");
                          setMatches([]);
                        }}
                      >
                        {m.name ?? "Unnamed contact"}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
              {personId && (
                <p className="text-xs text-muted-foreground">To: {personName}</p>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="gl-type">Type</Label>
                <Select value={type} onValueChange={(v) => setType(v ?? "1")}>
                  <SelectTrigger id="gl-type">
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
                <Label htmlFor="gl-title">What was it about</Label>
                <Input
                  id="gl-title"
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
              <Button type="submit" disabled={busy || !personId}>
                {busy ? "Saving…" : "Save"}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
