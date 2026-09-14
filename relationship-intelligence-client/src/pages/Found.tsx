import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { EmptyState, ErrorState, LoadingList, NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { sourceLabel } from "@/lib/ingestion";
import type {
  IngestionApplyResult,
  IngestionBatch,
  IngestionFinding,
  ResolutionCandidate,
} from "@/lib/types";

// IngestionFindingStatus: Pending 0, Approved 1, Rejected 2, Edited 3, Unresolved 4.
const FindingStatuses = ["Pending", "Approved", "Rejected", "Edited", "Unresolved"] as const;

function ConfidenceBadge({ value }: { value: string }) {
  const tone =
    value === "High"
      ? "border-emerald-300 bg-emerald-50 text-emerald-800"
      : value === "Medium"
        ? "border-sky-300 bg-sky-50 text-sky-800"
        : value === "Low"
          ? "border-amber-300 bg-amber-50 text-amber-800"
          : "border-red-300 bg-red-50 text-red-800";
  return (
    <span className={`rounded-full border px-2 py-0.5 text-xs font-medium ${tone}`}>
      {value} confidence
    </span>
  );
}

function groupKey(f: IngestionFinding): string {
  if (f.subjectPersonId) return `person:${f.subjectPersonId}`;
  const name = (f.subjectName ?? "unknown").trim().toLowerCase();
  return f.subjectIsNew ? `new:${name}` : `unresolved:${name}`;
}

function groupTitle(findings: IngestionFinding[]): string {
  const first = findings[0];
  if (first.subjectPersonId) return first.subjectPersonName ?? first.subjectName ?? "Contact";
  if (first.subjectIsNew) return `New person: ${first.subjectName ?? "unnamed"}`;
  return `Needs a person: ${first.subjectName ?? "unnamed"}`;
}

function FindingCard({
  finding,
  onChanged,
}: {
  finding: IngestionFinding;
  onChanged: () => void;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState(false);
  const [title, setTitle] = useState(finding.title);
  const [detail, setDetail] = useState(finding.detail ?? "");
  const [email, setEmail] = useState("");
  const [eventDate, setEventDate] = useState("");
  const [personQuery, setPersonQuery] = useState("");
  const [personOptions, setPersonOptions] = useState<ResolutionCandidate[]>(finding.candidates ?? []);
  const [searching, setSearching] = useState(false);

  async function review(body: Record<string, unknown>) {
    setError(null);
    setBusy(true);
    try {
      await api.put("/api/Ingestion/PutReview", { FindingId: finding.ingestionFindingId, ...body });
      onChanged();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Review failed.");
    } finally {
      setBusy(false);
    }
  }

  async function searchPeople(q: string) {
    setPersonQuery(q);
    if (q.trim().length < 2) {
      setPersonOptions(finding.candidates ?? []);
      return;
    }
    setSearching(true);
    try {
      const res = await api.get<{ items: Array<{ personId: string; name: string | null }> }>(
        `/api/Contacts/GetContactsFilteredByBatches?QueryParamter=${encodeURIComponent(q.trim())}&SearchBy=Name&pageNumber=1&pageSize=8`,
      );
      setPersonOptions(
        (res.items ?? []).map((p) => ({
          personId: p.personId,
          name: p.name ?? "Unnamed contact",
          organization: null,
          role: null,
          evidence: "Name search",
          isNew: false,
        })),
      );
    } catch {
      /* keep server-suggested candidates */
    } finally {
      setSearching(false);
    }
  }

  const needsPerson = !finding.subjectPersonId && !finding.subjectIsNew;
  const needsEmail = finding.subjectIsNew && !finding.subjectPersonId;
  const needsDate = finding.eventKind != null && finding.eventKind !== "";

  return (
    <li className="rounded-lg border px-3 py-2.5">
      <div className="flex flex-wrap items-center gap-1.5">
        <Badge variant="outline">{FindingStatuses[Number(finding.status)] ?? finding.status}</Badge>
        <ConfidenceBadge value={finding.confidence} />
        {finding.conflictType !== "None" && (
          <Badge variant="secondary">
            {finding.conflictType === "Duplicate"
              ? "Already recorded?"
              : finding.conflictType === "Contradiction"
                ? "Contradiction"
                : "Possible change"}
          </Badge>
        )}
        {finding.relationKind && <Badge variant="outline">↔ {finding.relationKind}</Badge>}
        {finding.eventKind && <Badge variant="outline">📅 {finding.eventKind}</Badge>}
      </div>

      <p className="mt-1.5 text-sm font-semibold">{finding.title}</p>
      {finding.detail && <p className="mt-0.5 text-sm text-muted-foreground">{finding.detail}</p>}

      {finding.existingValue && finding.conflictType !== "None" && finding.conflictType !== "Duplicate" && (
        <div className="mt-2 grid gap-1 rounded-md bg-secondary/50 px-2.5 py-2 text-sm sm:grid-cols-2">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Current</p>
            <p>{finding.existingValue}</p>
          </div>
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Proposed</p>
            <p>{finding.title}</p>
          </div>
        </div>
      )}
      {finding.conflictType === "Duplicate" && finding.existingValue && (
        <p className="mt-1.5 text-xs text-muted-foreground">
          Looks already recorded as “{finding.existingValue}”. Approving again would duplicate it.
        </p>
      )}
      {finding.objectName && (
        <p className="mt-1.5 text-xs text-muted-foreground">
          With: <strong className="text-foreground">{finding.objectName}</strong>
          {finding.objectOrg ? ` · ${finding.objectOrg}` : ""}
        </p>
      )}
      {finding.targetField && (
        <p className="mt-1 text-xs text-muted-foreground">
          Field: <code>{finding.targetField}</code>
          {finding.memoryKind ? ` · Memory: ${finding.memoryKind}` : ""}
        </p>
      )}
      {!finding.targetField && finding.memoryKind && (
        <p className="mt-1 text-xs text-muted-foreground">Memory: {finding.memoryKind}</p>
      )}
      {finding.uncertaintyReason && (
        <p className="mt-1.5 text-xs text-amber-800">⚠ {finding.uncertaintyReason}</p>
      )}
      {finding.sourceExcerpt && (
        <blockquote className="mt-2 border-l-2 pl-2 text-xs italic text-muted-foreground">
          “{finding.sourceExcerpt}”
        </blockquote>
      )}

      {needsPerson && (
        <div className="mt-2 rounded-md border border-dashed px-2.5 py-2">
          <Label htmlFor={`pick-${finding.ingestionFindingId}`} className="text-xs font-semibold">
            Who is this about? Pick one — extraction is not rerun.
          </Label>
          <Input
            id={`pick-${finding.ingestionFindingId}`}
            className="mt-1"
            placeholder="Search contacts…"
            value={personQuery}
            onChange={(e) => void searchPeople(e.target.value)}
          />
          <ul className="mt-1.5 flex flex-col gap-1">
            {personOptions.map((c) => (
              <li key={c.personId ?? c.name} className="flex flex-wrap items-center gap-2 text-sm">
                <span className="font-medium">{c.name}</span>
                {(c.role || c.organization) && (
                  <span className="text-xs text-muted-foreground">
                    {[c.role, c.organization].filter(Boolean).join(" · ")}
                  </span>
                )}
                {c.evidence && <span className="text-xs text-muted-foreground">({c.evidence})</span>}
                <Button
                  size="sm"
                  variant="outline"
                  disabled={busy || !c.personId}
                  onClick={() => void review({ Status: 0, SubjectPersonId: c.personId, SubjectIsNew: false })}
                >
                  Use this person
                </Button>
              </li>
            ))}
            {searching && <li className="text-xs text-muted-foreground">Searching…</li>}
          </ul>
          <div className="mt-1.5 flex flex-wrap gap-2">
            <Button
              size="sm"
              variant="ghost"
              disabled={busy}
              onClick={() =>
                void review({ Status: 0, SubjectIsNew: true, SubjectName: finding.subjectName })
              }
            >
              Create as new person
            </Button>
            <Button size="sm" variant="ghost" disabled={busy} onClick={() => void review({ Status: 2 })}>
              Ignore
            </Button>
          </div>
        </div>
      )}

      {needsEmail && (
        <div className="mt-2 flex flex-col gap-1">
          <Label htmlFor={`email-${finding.ingestionFindingId}`} className="text-xs font-semibold">
            Email is required before “{finding.subjectName}” can be created — it is never invented.
          </Label>
          <div className="flex gap-2">
            <Input
              id={`email-${finding.ingestionFindingId}`}
              type="email"
              placeholder="name@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
            <Button
              size="sm"
              disabled={busy || email.trim() === ""}
              onClick={() => void review({ Status: 0, SubjectEmail: email.trim() })}
            >
              Save
            </Button>
          </div>
        </div>
      )}

      {needsDate && (
        <div className="mt-2 flex flex-col gap-1">
          <Label htmlFor={`date-${finding.ingestionFindingId}`} className="text-xs font-semibold">
            Confirm the date — it is never parsed from prose.
          </Label>
          <div className="flex gap-2">
            <Input
              id={`date-${finding.ingestionFindingId}`}
              type="date"
              value={eventDate}
              onChange={(e) => setEventDate(e.target.value)}
            />
            <Button
              size="sm"
              disabled={busy || eventDate === ""}
              onClick={() => void review({ Status: 0, EventDate: new Date(eventDate).toISOString() })}
            >
              Save
            </Button>
          </div>
        </div>
      )}

      {editing ? (
        <div className="mt-2 flex flex-col gap-1.5">
          <Label htmlFor={`t-${finding.ingestionFindingId}`} className="text-xs font-semibold">Edit proposal</Label>
          <Input id={`t-${finding.ingestionFindingId}`} value={title} onChange={(e) => setTitle(e.target.value)} />
          <Textarea rows={2} value={detail} onChange={(e) => setDetail(e.target.value)} placeholder="Detail (optional)" />
          <div className="flex gap-2">
            <Button
              size="sm"
              disabled={busy || title.trim() === ""}
              onClick={() => {
                void review({ Status: 0, Title: title.trim(), Detail: detail.trim() === "" ? null : detail.trim() }).then(() =>
                  setEditing(false),
                );
              }}
            >
              Save edit
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setEditing(false)}>
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <div className="mt-2 flex flex-wrap gap-1.5">
          <Button size="sm" variant="outline" disabled={busy} onClick={() => void review({ Status: 1 })}>
            Approve
          </Button>
          <Button size="sm" variant="ghost" disabled={busy} onClick={() => void review({ Status: 2 })}>
            Reject
          </Button>
          <Button size="sm" variant="ghost" disabled={busy} onClick={() => setEditing(true)}>
            Edit
          </Button>
        </div>
      )}
      {error && <p className="mt-1.5 text-sm text-destructive">{error}</p>}
    </li>
  );
}

function ImpactPreview({ findings }: { findings: IngestionFinding[] }) {
  const ready = findings.filter((f) => f.status === "Approved" || f.status === "Edited");
  const blocked = findings.filter((f) => f.status === "Unresolved" || f.confidence === "Unresolved" || f.confidence === "Contradictory");
  const waiting = findings.filter((f) => f.status === "Pending");
  if (ready.length === 0) {
    return (
      <p className="text-xs text-muted-foreground">
        Nothing is ready to apply yet — review each finding above first. Unresolved items always stay pending.
      </p>
    );
  }
  return (
    <div className="text-sm">
      <p className="font-semibold">This will update {groupTitle(findings)}</p>
      <ul className="mt-1 list-disc pl-5 text-muted-foreground">
        {ready.map((f) => (
          <li key={f.ingestionFindingId}>{f.title}</li>
        ))}
      </ul>
      {(blocked.length > 0 || waiting.length > 0) && (
        <p className="mt-1 text-xs text-muted-foreground">
          {blocked.length > 0 && `${blocked.length} uncertain item(s) stay pending. `}
          {waiting.length > 0 && `${waiting.length} unreviewed item(s) are not included.`}
        </p>
      )}
    </div>
  );
}

export function Found() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [batches, setBatches] = useState<IngestionBatch[]>([]);
  const [selected, setSelected] = useState<IngestionBatch | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [applyResult, setApplyResult] = useState<IngestionApplyResult | null>(null);
  const [busy, setBusy] = useState(false);

  const loadBatches = useCallback(async () => {
    try {
      const list = await api.get<IngestionBatch[]>("/api/Ingestion/GetBatches");
      setBatches(list ?? []);
      return list ?? [];
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not load findings.");
      return [];
    }
  }, []);

  const loadBatch = useCallback(async (id: string) => {
    try {
      const batch = await api.get<IngestionBatch>(`/api/Ingestion/GetBatch?id=${id}`);
      setSelected(batch);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not load batch.");
    }
  }, []);

  useEffect(() => {
    (async () => {
      setLoading(true);
      const list = await loadBatches();
      const focused = searchParams.get("batch");
      if (focused) {
        await loadBatch(focused);
      } else if (list.length > 0) {
        await loadBatch(list[0].ingestionBatchId);
      }
      setLoading(false);
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const groups = useMemo(() => {
    const map = new Map<string, IngestionFinding[]>();
    for (const f of selected?.findings ?? []) {
      const key = groupKey(f);
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(f);
    }
    return [...map.entries()];
  }, [selected]);

  async function approvePerson(key: string, findings: IngestionFinding[]) {
    const first = findings[0];
    setError(null);
    setApplyResult(null);
    setBusy(true);
    try {
      const result = await api.post<IngestionApplyResult>("/api/Ingestion/PostApprovePerson", {
        BatchId: selected!.ingestionBatchId,
        PersonId: first.subjectPersonId,
        PersonName: first.subjectName,
        IsNew: !first.subjectPersonId,
      });
      setApplyResult(result);
      await loadBatch(selected!.ingestionBatchId);
      await loadBatches();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Approval failed.");
    } finally {
      setBusy(false);
    }
    void key;
  }

  async function approveBatch() {
    if (!selected) return;
    setError(null);
    setApplyResult(null);
    setBusy(true);
    try {
      const result = await api.post<IngestionApplyResult>(
        `/api/Ingestion/PostApproveBatch?id=${selected.ingestionBatchId}`,
      );
      setApplyResult(result);
      await loadBatch(selected.ingestionBatchId);
      await loadBatches();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Approval failed.");
    } finally {
      setBusy(false);
    }
  }

  const pendingTotal = batches.reduce((n, b) => n + (b.pendingCount ?? 0), 0);

  if (loading) return <LoadingList rows={6} />;

  return (
    <div className="max-w-4xl">
      <h1 className="font-display text-3xl font-semibold tracking-tight">
        Things I Found{pendingTotal > 0 ? ` · ${pendingTotal}` : ""}
      </h1>
      <p className="mt-1 text-sm text-muted-foreground">
        Unapproved AI proposals from your pasted text, conversations, and meeting notes.{" "}
        <strong className="text-foreground">Nothing here changes your relationships until you approve it</strong> —
        no urgency, no reminders, no scores move before that. This is separate from the{" "}
        <Link to="/attention" className="underline">attention queue</Link>, which reflects trusted state only.
      </p>
      {error && <div className="mt-3"><ErrorState message={error} /></div>}

      {batches.length === 0 ? (
        <div className="mt-6">
          <EmptyState
            title="Nothing waiting for review"
            body="Paste a profile, a conversation, or meeting notes from Add person, a relationship page, or a meeting — the findings will land here."
          />
        </div>
      ) : (
        <div className="mt-6 grid gap-6 lg:grid-cols-[240px_1fr]">
          <aside aria-label="Batches">
            <ul className="flex flex-col gap-1.5">
              {batches.map((b) => (
                <li key={b.ingestionBatchId}>
                  <button
                    type="button"
                    onClick={() => {
                      setSelected(null);
                      setApplyResult(null);
                      setSearchParams({ batch: b.ingestionBatchId });
                      void loadBatch(b.ingestionBatchId);
                    }}
                    className={`w-full rounded-lg border px-3 py-2 text-left text-sm ${
                      selected?.ingestionBatchId === b.ingestionBatchId
                        ? "border-primary bg-secondary"
                        : "hover:bg-secondary/50"
                    }`}
                  >
                    <span className="font-medium">{sourceLabel(b.sourceType)}</span>
                    <span className="block text-xs text-muted-foreground">
                      {formatDate(b.createdAtUtc)} ·{" "}
                      {b.isNoOp ? "no findings" : `${b.pendingCount} pending / ${b.findingCount} total`} · {b.status}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </aside>

          <section aria-label="Batch detail">
            {!selected ? (
              <LoadingList rows={4} />
            ) : selected.isNoOp ? (
              <div className="rounded-lg border px-4 py-3 text-sm">
                <p className="font-semibold">No meaningful relationship information found.</p>
                <p className="mt-0.5 text-muted-foreground">
                  {selected.noOpReason ?? "The text contained nothing reliable to propose."} Nothing was recorded.
                </p>
              </div>
            ) : (
              <div className="flex flex-col gap-6">
                {groups.map(([key, findings]) => (
                  <div key={key}>
                    <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                      <h2 className="font-display text-xl font-semibold">{groupTitle(findings)}</h2>
                      <Button
                        size="sm"
                        disabled={busy}
                        onClick={() => void approvePerson(key, findings)}
                      >
                        {busy ? "Applying…" : `Approve ${groupTitle(findings).split(":")[0] === "New person" ? "person" : "for this person"}`}
                      </Button>
                    </div>
                    <div className="mb-2 rounded-lg border bg-card px-3 py-2">
                      <ImpactPreview findings={findings} />
                    </div>
                    <ul className="flex flex-col gap-2">
                      {findings.map((f) => (
                        <FindingCard
                          key={f.ingestionFindingId}
                          finding={f}
                          onChanged={() => {
                            void loadBatch(selected.ingestionBatchId);
                            void loadBatches();
                          }}
                        />
                      ))}
                    </ul>
                  </div>
                ))}

                <div className="rounded-lg border px-4 py-3">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="text-sm">
                      <p className="font-semibold">Approve the whole batch</p>
                      <p className="text-muted-foreground">
                        Applies every reviewed finding; unresolved and unreviewed items stay pending.
                      </p>
                    </div>
                    <Button disabled={busy} onClick={() => void approveBatch()}>
                      {busy ? "Applying…" : "Approve all reviewed"}
                    </Button>
                  </div>
                  {applyResult && (
                    <div className="mt-2 text-sm">
                      <p>
                        Applied {applyResult.appliedCount}, skipped {applyResult.skippedCount}.
                      </p>
                      {applyResult.skipped.length > 0 && (
                        <ul className="mt-1 list-disc pl-5 text-muted-foreground">
                          {applyResult.skipped.map((s, i) => (
                            <li key={i}>{s}</li>
                          ))}
                        </ul>
                      )}
                    </div>
                  )}
                </div>
              </div>
            )}
          </section>
        </div>
      )}

      <div className="mt-6">
        <NavButton to="/" variant="ghost" size="sm">
          ← Today
        </NavButton>
      </div>
    </div>
  );
}
