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
import { EvidenceChip } from "@/components/evidence-chip";
import { ApiError, api } from "@/lib/api";
import { MemoryKinds } from "@/lib/types";
import type { MemoryEntryResponse } from "@/lib/types";

const STATUS_LABELS = ["Active", "Done", "Dropped"] as const;

function ProvenanceBadge({ provenance }: { provenance: number }) {
  if (provenance === 1) return <EvidenceChip kind="suggested" />;
  if (provenance === 2) return <EvidenceChip kind="user" label="Confirmed" title="Assistant proposal you confirmed — now canonical" />;
  if (provenance === 3) return <EvidenceChip kind="meeting" />;
  return <EvidenceChip kind="user" label="Your note" />;
}

export function MemorySection({ personId }: { personId: string }) {
  const [entries, setEntries] = useState<MemoryEntryResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [kind, setKind] = useState("5");
  const [title, setTitle] = useState("");
  const [detail, setDetail] = useState("");
  const [busy, setBusy] = useState(false);
  const [editing, setEditing] = useState<string | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [editDetail, setEditDetail] = useState("");
  const [editStatus, setEditStatus] = useState("0");

  const load = useCallback(async () => {
    setError(null);
    try {
      setEntries(await api.get<MemoryEntryResponse[]>(`/api/Contacts/GetRelationshipMemory?personId=${personId}`));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load relationship context.");
    }
  }, [personId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function create(e: React.FormEvent) {
    e.preventDefault();
    if (title.trim() === "") return;
    setBusy(true);
    try {
      await api.post("/api/Contacts/PostMemoryEntry", {
        PersonId: personId,
        Kind: Number(kind),
        Title: title.trim(),
        Detail: detail.trim() === "" ? null : detail.trim(),
      });
      setTitle("");
      setDetail("");
      setAdding(false);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save.");
    } finally {
      setBusy(false);
    }
  }

  async function saveEdit(entry: MemoryEntryResponse) {
    if (editTitle.trim() === "") return;
    setBusy(true);
    try {
      await api.put("/api/Contacts/PutMemoryEntry", {
        MemoryEntryId: entry.memoryEntryId,
        Kind: entry.kind,
        Title: editTitle.trim(),
        Detail: editDetail.trim() === "" ? null : editDetail.trim(),
        Status: Number(editStatus),
      });
      setEditing(null);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save.");
    } finally {
      setBusy(false);
    }
  }

  async function remove(entry: MemoryEntryResponse) {
    if (!window.confirm(`Delete "${entry.title}" permanently?`)) return;
    try {
      await api.del(`/api/Contacts/DeleteMemoryEntry?id=${entry.memoryEntryId}`);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not delete.");
    }
  }

  async function accept(entry: MemoryEntryResponse) {
    try {
      await api.post(`/api/Contacts/PostAcceptMemorySuggestion?id=${entry.memoryEntryId}`);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not accept.");
    }
  }

  async function reject(entry: MemoryEntryResponse) {
    try {
      await api.post(`/api/Contacts/PostRejectMemorySuggestion?id=${entry.memoryEntryId}`);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not reject.");
    }
  }

  return (
    <section>
      <div className="mb-1.5 flex items-center justify-between">
        <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          Relationship context
        </h3>
        {!adding && (
          <Button size="sm" variant="ghost" onClick={() => setAdding(true)}>
            Add
          </Button>
        )}
      </div>
      <p className="mb-2 text-xs text-muted-foreground">
        Your understanding of this relationship — what it is, what matters, what was promised.
        Observed interaction data and system scores stay separate.
      </p>

      {adding && (
        <form onSubmit={create} className="mb-3 flex flex-col gap-2 rounded-lg border p-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`mem-kind-${personId}`}>Kind</Label>
            <Select value={kind} onValueChange={(v) => setKind(v ?? "5")}>
              <SelectTrigger id={`mem-kind-${personId}`}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {MemoryKinds.map((k, i) => (
                  <SelectItem key={k} value={String(i)}>
                    {k}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`mem-title-${personId}`}>Title</Label>
            <Input
              id={`mem-title-${personId}`}
              value={title}
              maxLength={200}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. Promised the phase-2 proposal by month end"
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`mem-detail-${personId}`}>Detail (optional)</Label>
            <Textarea
              id={`mem-detail-${personId}`}
              rows={2}
              value={detail}
              maxLength={2000}
              onChange={(e) => setDetail(e.target.value)}
            />
          </div>
          <div className="flex gap-2">
            <Button type="submit" size="sm" disabled={busy || title.trim() === ""}>
              {busy ? "Saving…" : "Save"}
            </Button>
            <Button type="button" size="sm" variant="ghost" onClick={() => setAdding(false)}>
              Cancel
            </Button>
          </div>
        </form>
      )}

      {error && <p className="mb-2 text-xs text-destructive">{error}</p>}
      {entries === null && !error && <p className="text-xs text-muted-foreground">Loading…</p>}
      {entries !== null && entries.length === 0 && !adding && (
        <p className="text-xs text-muted-foreground">
          Nothing recorded yet — add what this relationship is and what matters in it.
        </p>
      )}
      {entries !== null && entries.length > 0 && (
        <ul className="flex flex-col gap-2">
          {entries.map((entry) => (
            <li key={entry.memoryEntryId} className="rounded-lg border px-3 py-2">
              {editing === entry.memoryEntryId ? (
                <div className="flex flex-col gap-2">
                  <Input
                    aria-label="Memory title"
                    value={editTitle}
                    maxLength={200}
                    onChange={(e) => setEditTitle(e.target.value)}
                  />
                  <Textarea
                    aria-label="Memory detail"
                    rows={2}
                    value={editDetail}
                    maxLength={2000}
                    onChange={(e) => setEditDetail(e.target.value)}
                  />
                  <div className="flex items-center gap-2">
                    <Select value={editStatus} onValueChange={(v) => setEditStatus(v ?? "0")}>
                      <SelectTrigger aria-label="Memory status" className="w-32">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {STATUS_LABELS.map((s, i) => (
                          <SelectItem key={s} value={String(i)}>
                            {s}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Button size="sm" disabled={busy} onClick={() => void saveEdit(entry)}>
                      Save
                    </Button>
                    <Button size="sm" variant="ghost" onClick={() => setEditing(null)}>
                      Cancel
                    </Button>
                  </div>
                </div>
              ) : (
                <>
                  <div className="flex flex-wrap items-center gap-1.5">
                    <Badge variant="secondary">{MemoryKinds[entry.kind] ?? entry.kind}</Badge>
                    <ProvenanceBadge provenance={entry.provenance} />
                    {entry.status !== 0 && (
                      <Badge variant="outline">{STATUS_LABELS[entry.status] ?? entry.status}</Badge>
                    )}
                  </div>
                  <p className="mt-1 text-sm font-medium">{entry.title}</p>
                  {entry.detail && <p className="mt-0.5 text-sm text-muted-foreground">{entry.detail}</p>}
                  {entry.sourceExcerpt && (
                    <p className="mt-1 border-l-2 pl-2 text-xs italic text-muted-foreground">
                      “{entry.sourceExcerpt}”
                      {entry.sourceMeetingDeleted ? " (source meeting removed)" : ""}
                    </p>
                  )}
                  <div className="mt-1.5 flex flex-wrap gap-1.5">
                    {entry.provenance === 1 ? (
                      <>
                        <Button size="sm" variant="ghost" onClick={() => void accept(entry)}>
                          Accept
                        </Button>
                        <Button size="sm" variant="ghost" onClick={() => void reject(entry)}>
                          Reject
                        </Button>
                      </>
                    ) : (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {
                          setEditing(entry.memoryEntryId);
                          setEditTitle(entry.title);
                          setEditDetail(entry.detail ?? "");
                          setEditStatus(String(entry.status));
                        }}
                      >
                        Edit
                      </Button>
                    )}
                    <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => void remove(entry)}>
                      Delete
                    </Button>
                  </div>
                </>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
