import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { EvidenceChip } from "@/components/evidence-chip";
import { Separator } from "@/components/ui/separator";
import { ErrorState, LoadingList, NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { FindingKinds, MeetingStatuses } from "@/lib/types";
import type { MeetingFindingDto, MeetingPersonDto, MeetingResponse, PagedResult, PersonView } from "@/lib/types";

const MATCH_LABELS = ["Unmapped", "Suggested", "Confirmed", "Excluded"] as const;

function PersonSearch({
  value,
  onPick,
}: {
  value: string | null;
  onPick: (id: string | null, name: string | null) => void;
}) {
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
    <div className="relative min-w-40 flex-1">
      <Input
        aria-label="Map to person"
        value={value ?? query}
        onChange={(e) => {
          onPick(null, null);
          void search(e.target.value);
        }}
        onFocus={() => {
          if (results.length > 0) setOpen(true);
        }}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
        placeholder="Search contacts…"
      />
      {open && results.length > 0 && (
        <ul className="absolute z-10 mt-1 max-h-48 w-full overflow-auto rounded-md border bg-background shadow-lg">
          {results.map((p) => (
            <li key={p.personId}>
              <button
                type="button"
                className="w-full px-3 py-1.5 text-left text-sm hover:bg-secondary"
                onMouseDown={() => {
                  onPick(p.personId, p.name);
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

function BriefView({ briefJson }: { briefJson: string }) {
  let participants: Array<Record<string, unknown>> = [];
  try {
    const parsed = JSON.parse(briefJson) as { participants?: Array<Record<string, unknown>> };
    participants = Array.isArray(parsed.participants) ? parsed.participants : [];
  } catch {
    return <p className="text-xs text-muted-foreground">Brief content is not valid JSON — edit it below.</p>;
  }
  if (participants.length === 0) {
    return <p className="text-xs text-muted-foreground">Brief has no participant sections yet — generate it above.</p>;
  }
  return (
    <ul className="flex flex-col gap-3">
      {participants.map((p: Record<string, unknown>, i: number) => (
        <li key={i} className="rounded-lg border px-3 py-2 text-sm">
          <p className="font-semibold">{String(p.displayName ?? p.personId ?? `Participant ${i + 1}`)}</p>
          {p.whoIsThis != null && <p className="mt-0.5 text-muted-foreground">Who: {String(p.whoIsThis)}</p>}
          {p.state != null && <p className="mt-0.5 text-muted-foreground">State: {JSON.stringify(p.state)}</p>}
          {Array.isArray(p.thingsToRemember) && (p.thingsToRemember as unknown[]).length > 0 && (
            <div className="mt-1">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Remember</p>
              <ul className="list-disc pl-4 text-muted-foreground">
                {(p.thingsToRemember as unknown[]).map((t, j) => (<li key={j}>{String(t)}</li>))}
              </ul>
            </div>
          )}
          {Array.isArray(p.talkingPoints) && (p.talkingPoints as unknown[]).length > 0 && (
            <div className="mt-1">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Talking points</p>
              <ul className="list-disc pl-4 text-muted-foreground">
                {(p.talkingPoints as unknown[]).map((t, j) => (<li key={j}>{String(t)}</li>))}
              </ul>
            </div>
          )}
          {Array.isArray(p.questionsToAsk) && (p.questionsToAsk as unknown[]).length > 0 && (
            <div className="mt-1">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Questions</p>
              <ul className="list-disc pl-4 text-muted-foreground">
                {(p.questionsToAsk as unknown[]).map((t, j) => (<li key={j}>{String(t)}</li>))}
              </ul>
            </div>
          )}
          {Array.isArray(p.commitments) && p.commitments.length > 0 && (
            <p className="mt-0.5 text-muted-foreground">Open commitments: {(p.commitments as string[]).join("; ")}</p>
          )}
          {Array.isArray(p.relevantEvents) && (p.relevantEvents as unknown[]).length > 0 && (
            <p className="mt-0.5 text-muted-foreground">Relevant events: {(p.relevantEvents as string[]).join("; ")}</p>
          )}
          {Array.isArray(p.relevantGoals) && (p.relevantGoals as unknown[]).length > 0 && (
            <p className="mt-0.5 text-muted-foreground">Goals: {(p.relevantGoals as string[]).join("; ")}</p>
          )}
        </li>
      ))}
    </ul>
  );
}

export function MeetingDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [meeting, setMeeting] = useState<MeetingResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [transcript, setTranscript] = useState("");
  const [notes, setNotes] = useState("");
  const [instructions, setInstructions] = useState("");
  const [editingSource, setEditingSource] = useState(false);
  const [draftNames, setDraftNames] = useState<Record<string, { id: string | null; name: string | null }>>({});
  const [findingAssignees, setFindingAssignees] = useState<Record<string, { id: string | null; name: string | null }>>({});
  const [briefJson, setBriefJson] = useState("");
  const [editingBrief, setEditingBrief] = useState(false);
  const [actualDate, setActualDate] = useState("");

  const load = useCallback(async () => {
    if (!id) return;
    setError(null);
    try {
      const m = await api.get<MeetingResponse>(`/api/Meeting/GetMeeting?id=${id}`);
      setMeeting(m);
      setDraftNames({});
      if (m.brief) setBriefJson(m.brief.briefJson);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load meeting.");
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load]);

  async function run<T>(fn: () => Promise<T>, after?: (r: T) => void) {
    setError(null);
    setBusy(true);
    try {
      const r = await fn();
      if (after) after(r);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Operation failed.");
    } finally {
      setBusy(false);
    }
  }

  if (error && !meeting) return <ErrorState message={error} onRetry={() => void load()} />;
  if (!meeting) return <LoadingList rows={6} />;

  const editable = meeting.status !== 4 && meeting.status !== 5;
  const isPrep = meeting.status === 0;

  function mappingPayload(): Array<{ MeetingPersonId: string; MappedPersonId: string | null; MatchStatus: number; SelectedForLogging: boolean }> {
    return meeting!.people.map((p) => {
      const draft = draftNames[p.meetingPersonId];
      return {
        MeetingPersonId: p.meetingPersonId,
        MappedPersonId: draft ? draft.id : p.mappedPersonId,
        MatchStatus: draft ? (draft.id ? 2 : 0) : p.matchStatus,
        SelectedForLogging: p.selectedForLogging,
      };
    });
  }

  async function saveMappings() {
    await run(() =>
      api.put<MeetingResponse>(`/api/Meeting/PutMeetingPersons?id=${meeting!.meetingId}`, mappingPayload()),
    );
  }

  async function toggleSelected(row: MeetingPersonDto) {
    if (!row.mappedPersonId || row.matchStatus !== 2) return;
    const payload = mappingPayload().map((m) =>
      m.MeetingPersonId === row.meetingPersonId ? { ...m, SelectedForLogging: !row.selectedForLogging } : m,
    );
    await run(() => api.put<MeetingResponse>(`/api/Meeting/PutMeetingPersons?id=${meeting!.meetingId}`, payload));
  }

  async function reviewFinding(f: MeetingFindingDto, status: number) {
    const assignee = findingAssignees[f.meetingFindingId];
    await run(() =>
      api.put<MeetingResponse>("/api/Meeting/PutMeetingFinding", {
        MeetingFindingId: f.meetingFindingId,
        Status: status,
        MappedPersonId: assignee ? assignee.id : (f.mappedPersonId ?? null),
      }),
    );
  }

  return (
    <div className="max-w-3xl">
      <NavButton to="/meetings" variant="ghost" size="sm" className="mb-4">
        ← Meetings
      </NavButton>
      <div className="flex flex-wrap items-center gap-2">
        <h1 className="font-display text-3xl font-semibold tracking-tight">{meeting.title}</h1>
        <Badge variant="outline">{MeetingStatuses[meeting.status]}</Badge>
      </div>
      <p className="mt-1 text-sm tabular-nums text-muted-foreground">
        {meeting.actualOccurredAtUtc
          ? `Happened ${formatDate(meeting.actualOccurredAtUtc)} (planned ${formatDate(meeting.occurredAtUtc)})`
          : `Planned ${formatDate(meeting.occurredAtUtc)}`}
        {meeting.description ? ` · ${meeting.description}` : ""}
      </p>
      {error && <p className="mt-2 text-sm text-destructive">{error}</p>}

      {isPrep && (
        <div className="mt-4 rounded-lg border border-dashed px-4 py-3 text-sm">
          <span className="font-semibold">Preparation. </span>
          <span className="text-muted-foreground">
            Plan here — participants, agenda, and the brief below stay attached when you start logging.
          </span>
          <div className="mt-2">
            <Button size="sm" disabled={busy} onClick={() => void run(() => api.post<MeetingResponse>(`/api/Meeting/PostMeetingBeginLogging?id=${meeting.meetingId}`))}>
              Start logging this meeting
            </Button>
          </div>
        </div>
      )}

      <section className="mt-6">
        <div className="mb-2 flex items-center justify-between">
          <h2 className="text-base font-semibold">Source material</h2>
          {editable && !editingSource && (
            <Button size="sm" variant="ghost" onClick={() => { setTranscript(""); setNotes(""); setInstructions(meeting.userInstructions ?? ""); setEditingSource(true); }}>
              {meeting.hasTranscript || meeting.hasNotes ? "Replace" : "Add transcript / notes"}
            </Button>
          )}
        </div>
        {!editingSource && (
          <div className="flex flex-col gap-2">
            <p className="text-sm text-muted-foreground">
              {meeting.hasTranscript ? "Transcript attached. " : ""}
              {meeting.hasNotes ? "Notes attached. " : ""}
              {!meeting.hasTranscript && !meeting.hasNotes ? "No transcript or notes yet. " : ""}
              Raw source is preserved separately from AI interpretation.
            </p>
            {(meeting.rawTranscript || meeting.rawNotes) && (
              <details className="rounded-lg border px-3 py-2">
                <summary className="cursor-pointer text-sm font-medium">
                  View raw source (observed evidence)
                </summary>
                {meeting.rawTranscript && (
                  <pre className="mt-2 max-h-64 overflow-auto whitespace-pre-wrap text-xs">{meeting.rawTranscript}</pre>
                )}
                {meeting.rawNotes && (
                  <pre className="mt-2 max-h-40 overflow-auto whitespace-pre-wrap text-xs">{meeting.rawNotes}</pre>
                )}
              </details>
            )}
          </div>
        )}
        {editingSource && (
          <div className="flex flex-col gap-2 rounded-lg border p-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="mtg-t">Transcript (up to 50,000 chars)</Label>
              <Textarea id="mtg-t" rows={6} value={transcript} onChange={(e) => setTranscript(e.target.value)} placeholder="Paste the transcript…" />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="mtg-n">Notes</Label>
              <Textarea id="mtg-n" rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Raw notes…" />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="mtg-i">What should the analysis focus on? (optional)</Label>
              <Input id="mtg-i" value={instructions} maxLength={1000} onChange={(e) => setInstructions(e.target.value)} placeholder="e.g. Focus on commitments and dates" />
            </div>
            <div className="flex gap-2">
              <Button
                size="sm"
                disabled={busy || (transcript.trim() === "" && notes.trim() === "")}
                onClick={() =>
                  void run(() =>
                    api.put<MeetingResponse>("/api/Meeting/PutMeetingTranscript", {
                      MeetingId: meeting.meetingId,
                      RawTranscript: transcript.trim() === "" ? null : transcript,
                      RawNotes: notes.trim() === "" ? null : notes.trim(),
                      UserInstructions: instructions.trim() === "" ? null : instructions.trim(),
                    }), () => setEditingSource(false))
                }
              >
                Save source
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setEditingSource(false)}>
                Cancel
              </Button>
            </div>
          </div>
        )}
        {meeting.status === 1 && (
          <div className="mt-2 flex flex-wrap gap-2">
            <Button size="sm" disabled={busy || (!meeting.hasTranscript && !meeting.hasNotes)} onClick={() => void run(() => api.post<MeetingResponse>(`/api/Meeting/PostMeetingProcess?id=${meeting.meetingId}`))}>
              {busy ? "Processing…" : "Process with co-pilot"}
            </Button>
            <Button
              size="sm"
              variant="outline"
              disabled={busy || (!meeting.hasTranscript && !meeting.hasNotes)}
              title="Send the transcript and notes — actual evidence only, never the agenda — into the unified review queue."
              onClick={() =>
                void (async () => {
                  setError(null);
                  setBusy(true);
                  try {
                    const batch = await api.post<{ ingestionBatchId: string }>(
                      `/api/Ingestion/PostSubmitForMeeting?meetingId=${meeting.meetingId}`,
                      {},
                    );
                    const processed = await api.post<{ ingestionBatchId: string }>(
                      `/api/Ingestion/PostProcess?id=${batch.ingestionBatchId}`,
                      {},
                    );
                    navigate(`/found?batch=${processed.ingestionBatchId}`);
                  } catch (err) {
                    setError(err instanceof ApiError ? err.body || err.message : "Operation failed.");
                  } finally {
                    setBusy(false);
                  }
                })()
              }
            >
              Send actual notes to review
            </Button>
          </div>
        )}
        {meeting.status === 2 && <p className="mt-2 text-sm text-muted-foreground">Processing… refresh in a moment.</p>}
      </section>

      <Separator className="my-6" />

      <section>
        <h2 className="mb-2 text-base font-semibold">People ({meeting.people.length})</h2>
        <p className="mb-2 text-xs text-muted-foreground">
          Detected names are never attached automatically — confirm each mapping, then tick who should receive this meeting as evidence.
        </p>
        {meeting.people.length === 0 && <p className="text-sm text-muted-foreground">No people detected yet — process the meeting first.</p>}
        <ul className="flex flex-col gap-2">
          {meeting.people.map((p) => (
            <li key={p.meetingPersonId} className="flex flex-wrap items-center gap-2 rounded-lg border px-3 py-2">
              <span className="min-w-32 text-sm font-medium">{p.detectedName}</span>
              <Badge variant="outline">{MATCH_LABELS[p.matchStatus] ?? p.matchStatus}</Badge>
              {editable ? (
                <PersonSearch
                  value={draftNames[p.meetingPersonId]?.name ?? p.mappedPersonName}
                  onPick={(pid, name) =>
                    setDraftNames((d) => ({ ...d, [p.meetingPersonId]: { id: pid, name } }))
                  }
                />
              ) : p.mappedPersonId ? (
                <Link to={`/people/${p.mappedPersonId}`} className="text-sm underline">
                  {p.mappedPersonName ?? "Open"}
                </Link>
              ) : null}
              {p.mappedPersonId && p.matchStatus === 2 && (
                <label className="flex items-center gap-1.5 text-xs">
                  <input
                    type="checkbox"
                    checked={p.selectedForLogging}
                    disabled={!editable}
                    onChange={() => void toggleSelected(p)}
                  />
                  Log for this relationship
                </label>
              )}
            </li>
          ))}
        </ul>
        {editable && meeting.people.length > 0 && (
          <div className="mt-2">
            <Button size="sm" variant="outline" disabled={busy} onClick={() => void saveMappings()}>
              Save mappings
            </Button>
          </div>
        )}
      </section>

      <Separator className="my-6" />

      <section>
        <h2 className="mb-2 text-base font-semibold">Findings ({meeting.findings.length})</h2>
        {meeting.processedSummary && (
          <div className="mb-2 rounded-lg border bg-card px-3 py-2">
            <p className="mb-1 text-xs text-muted-foreground">
              {meeting.status === 4
                ? "Meeting summary — confirmed by you, safe to rely on."
                : "AI-derived summary — pending your confirmation. Verify against the raw source above before confirming the meeting."}
            </p>
            <p className="text-sm">{meeting.processedSummary}</p>
          </div>
        )}
        {meeting.findings.length === 0 && <p className="text-sm text-muted-foreground">No findings yet.</p>}
        <ul className="flex flex-col gap-2">
          {meeting.findings.map((f) => (
            <li key={f.meetingFindingId} className="rounded-lg border px-3 py-2">
              <div className="flex flex-wrap items-center gap-1.5">
                <Badge variant="secondary">{FindingKinds[f.kind] ?? f.kind}</Badge>
                {f.status === 0 && <EvidenceChip kind="suggested" />}
                {f.status === 1 && <EvidenceChip kind="user" label="Accepted" title="You accepted this finding — it now counts as your confirmed understanding" />}
                {f.status === 2 && (
                  <Badge variant="outline" title="You rejected this finding — it will not be used">
                    Rejected
                  </Badge>
                )}
                {f.mappedPersonName && <span className="text-xs text-muted-foreground">→ {f.mappedPersonName}</span>}
              </div>
              <p className="mt-1 text-sm font-medium">{f.title}</p>
              {f.detail && <p className="mt-0.5 text-sm text-muted-foreground">{f.detail}</p>}
              {f.sourceExcerpt && <p className="mt-1 border-l-2 pl-2 text-xs italic text-muted-foreground">“{f.sourceExcerpt}”</p>}
              {editable && f.status === 0 && (
                <>
                  <div className="mt-1.5">
                    <PersonSearch
                      value={findingAssignees[f.meetingFindingId]?.name ?? f.mappedPersonName}
                      onPick={(pid, name) =>
                        setFindingAssignees((d) => ({ ...d, [f.meetingFindingId]: { id: pid, name } }))
                      }
                    />
                  </div>
                  <div className="mt-1.5 flex gap-1.5">
                    <Button size="sm" variant="ghost" onClick={() => void reviewFinding(f, 1)}>
                      Accept
                    </Button>
                    <Button size="sm" variant="ghost" onClick={() => void reviewFinding(f, 2)}>
                      Reject
                    </Button>
                  </div>
                </>
              )}
            </li>
          ))}
        </ul>
      </section>

      <Separator className="my-6" />

      <section>
        <div className="mb-2 flex items-center justify-between">
          <h2 className="text-base font-semibold">Preparation brief</h2>
          {editable && (
            <Button size="sm" variant="ghost" disabled={busy} onClick={() => void run(() => api.post<MeetingResponse>(`/api/Meeting/PostMeetingBrief?id=${meeting.meetingId}`))}>
              {meeting.brief ? "Regenerate" : "Generate"}
            </Button>
          )}
        </div>
        {meeting.brief ? (
          <>
            <BriefView briefJson={meeting.brief.briefJson} />
            {editable && (
              <div className="mt-2 flex flex-col gap-2">
                {!editingBrief ? (
                  <Button size="sm" variant="ghost" className="self-start" onClick={() => setEditingBrief(true)}>
                    Edit brief
                  </Button>
                ) : (
                  <>
                    <Textarea rows={8} value={briefJson} onChange={(e) => setBriefJson(e.target.value)} aria-label="Brief JSON" />
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        disabled={busy}
                        onClick={() =>
                          void run(
                            () => api.put<MeetingResponse>("/api/Meeting/PutMeetingBrief", { MeetingId: meeting.meetingId, BriefJson: briefJson }),
                            () => setEditingBrief(false),
                          )
                        }
                      >
                        Save brief
                      </Button>
                      <Button size="sm" variant="ghost" onClick={() => { setEditingBrief(false); if (meeting.brief) setBriefJson(meeting.brief.briefJson); }}>
                        Cancel
                      </Button>
                    </div>
                  </>
                )}
              </div>
            )}
          </>
        ) : (
          <p className="text-sm text-muted-foreground">No brief yet — generate one from mapped participants and stored context.</p>
        )}
      </section>

      <Separator className="my-6" />

      <section>
        <h2 className="mb-2 text-base font-semibold">Confirm &amp; log</h2>
        {meeting.status === 4 ? (
          <p className="text-sm text-muted-foreground">
            Confirmed{meeting.actualOccurredAtUtc ? ` — happened ${formatDate(meeting.actualOccurredAtUtc)}` : ""}.
            This meeting is evidence and cannot be deleted; its interactions feed your relationship intelligence.
          </p>
        ) : (
          <>
            <p className="mb-2 text-xs text-muted-foreground">
              Only people ticked above receive this meeting as interaction evidence. Planned items never leak into confirmed data.
            </p>
            <div className="flex flex-wrap items-end gap-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="mtg-actual">Actual date (defaults to planned)</Label>
                <Input id="mtg-actual" type="date" value={actualDate} onChange={(e) => setActualDate(e.target.value)} />
              </div>
              <Button
                size="sm"
                disabled={busy || meeting.status !== 3}
                title={meeting.status !== 3 ? "Process the meeting first" : "Log interactions for selected people"}
                onClick={() =>
                  void run(() =>
                    api.post<MeetingResponse>("/api/Meeting/PostMeetingConfirm", {
                      MeetingId: meeting.meetingId,
                      ActualOccurredAtUtc: actualDate === "" ? null : new Date(`${actualDate}T12:00:00`).toISOString(),
                    }))
                }
              >
                Confirm &amp; log
              </Button>
              {editable && (
                <Button
                  size="sm"
                  variant="ghost"
                  className="text-destructive hover:text-destructive"
                  disabled={busy}
                  onClick={() => {
                    if (!window.confirm(`Delete "${meeting.title}"?`)) return;
                    void run(() => api.del(`/api/Meeting/DeleteMeeting?id=${meeting.meetingId}`), () => navigate("/meetings"));
                  }}
                >
                  Delete
                </Button>
              )}
            </div>
          </>
        )}
      </section>
    </div>
  );
}
