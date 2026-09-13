import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
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
import { Separator } from "@/components/ui/separator";
import { ErrorState, LoadingList, NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { useCopilot } from "@/lib/copilot";
import { DraftStatuses, OutreachChannels } from "@/lib/types";
import type { CommunicationDraft, OutreachBatch, OutreachBatchMember } from "@/lib/types";

function MemberRow({
  batchId,
  member,
  onChanged,
}: {
  batchId: string;
  member: OutreachBatchMember;
  onChanged: (batch: OutreachBatch) => void;
}) {
  const { openCopilot } = useCopilot();
  const [customizing, setCustomizing] = useState(false);
  const [channel, setChannel] = useState(member.channelOverride == null ? "__batch" : String(member.channelOverride));
  const [intent, setIntent] = useState(member.intentOverride ?? "");
  const [custom, setCustom] = useState(member.customInstruction ?? "");
  const [skipFuture, setSkipFuture] = useState(member.skipFutureSuggestions);

  async function save(overrides: Record<string, unknown>) {
    try {
      const updated = await api.put<OutreachBatch>(`/api/Outreach/PutBatchMember?id=${batchId}&memberId=${member.outreachBatchMemberId}`, {
        ChannelOverride: null,
        ClearChannelOverride: false,
        IntentOverride: null,
        CustomInstruction: null,
        Excluded: member.excluded,
        SkipFutureSuggestions: skipFuture,
        ...overrides,
      });
      onChanged(updated);
    } catch {
      /* surfaced by parent reload */
    }
  }

  return (
    <li className={`rounded-lg border px-3 py-2 ${member.excluded ? "opacity-50" : ""}`}>
      <div className="flex flex-wrap items-center gap-2">
        <Link to={`/people/${member.personId}`} className="text-sm font-medium hover:underline">
          {member.personName ?? "Unnamed contact"}
        </Link>
        {member.channelOverride != null && (
          <Badge variant="outline">via {OutreachChannels[member.channelOverride]}</Badge>
        )}
        <span className="text-xs text-muted-foreground">— {member.reason}</span>
      </div>
      <div className="mt-1.5 flex flex-wrap gap-1.5">
        <Button
          size="sm"
          variant="ghost"
          onClick={() => void save({ Excluded: !member.excluded })}
        >
          {member.excluded ? "Include" : "Remove"}
        </Button>
        <Button size="sm" variant="ghost" onClick={() => setCustomizing((c) => !c)}>
          {customizing ? "Hide options" : "Options"}
        </Button>
        <Button
          size="sm"
          variant="ghost"
          onClick={() => openCopilot({ personId: member.personId, personName: member.personName })}
        >
          Ask about them
        </Button>
      </div>
      {customizing && (
        <div className="mt-2 grid gap-2 rounded-md border p-2 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`ob-ch-${member.outreachBatchMemberId}`}>Channel for this person</Label>
            <Select
              value={channel}
              onValueChange={(v) => {
                setChannel(v ?? "__batch");
                void save(v === "__batch" ? { ClearChannelOverride: true } : { ChannelOverride: Number(v) });
              }}
            >
              <SelectTrigger id={`ob-ch-${member.outreachBatchMemberId}`}>
                <SelectValue placeholder="Batch default" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__batch">Batch default</SelectItem>
                {OutreachChannels.map((c, i) => (
                  <SelectItem key={c} value={String(i)}>
                    {c}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`ob-intent-${member.outreachBatchMemberId}`}>Different intent (optional)</Label>
            <Input
              id={`ob-intent-${member.outreachBatchMemberId}`}
              value={intent}
              maxLength={500}
              onChange={(e) => setIntent(e.target.value)}
              onBlur={() => void save({ IntentOverride: intent.trim() === "" ? null : intent.trim() })}
              placeholder="Batch intent applies"
            />
          </div>
          <div className="flex flex-col gap-1.5 sm:col-span-2">
            <Label htmlFor={`ob-custom-${member.outreachBatchMemberId}`}>Personal instruction (optional)</Label>
            <Input
              id={`ob-custom-${member.outreachBatchMemberId}`}
              value={custom}
              maxLength={1000}
              onChange={(e) => setCustom(e.target.value)}
              onBlur={() => void save({ CustomInstruction: custom.trim() === "" ? null : custom.trim() })}
              placeholder="e.g. Make this one more formal"
            />
          </div>
          <label className="flex items-center gap-2 text-xs sm:col-span-2">
            <input
              type="checkbox"
              checked={skipFuture}
              onChange={(e) => {
                setSkipFuture(e.target.checked);
                void save({ SkipFutureSuggestions: e.target.checked });
              }}
            />
            Don&apos;t suggest this person again for the next 30 days
          </label>
        </div>
      )}
    </li>
  );
}

function DraftCard({
  draft,
  onChanged,
}: {
  draft: CommunicationDraft;
  onChanged: (draft: CommunicationDraft) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [body, setBody] = useState(draft.body);
  const [subject, setSubject] = useState(draft.subject ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function review(status: number, nextBody?: string, nextSubject?: string) {
    setError(null);
    setBusy(true);
    try {
      const updated = await api.put<CommunicationDraft>(`/api/Outreach/PutDraft?id=${draft.communicationDraftId}`, {
        Status: status,
        Body: nextBody ?? body,
        Subject: nextSubject ?? subject,
      });
      setEditing(false);
      onChanged(updated);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save.");
    } finally {
      setBusy(false);
    }
  }

  async function regenerate() {
    setError(null);
    setBusy(true);
    try {
      const updated = await api.post<CommunicationDraft>(`/api/Outreach/PostDraftRegenerate?id=${draft.communicationDraftId}`, {});
      setBody(updated.body);
      setSubject(updated.subject ?? "");
      setEditing(false);
      onChanged(updated);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not regenerate.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <li className="rounded-lg border px-3 py-2">
      <div className="flex flex-wrap items-center gap-1.5">
        <Link to={`/people/${draft.personId}`} className="text-sm font-semibold hover:underline">
          {draft.personName ?? "Unnamed contact"}
        </Link>
        <Badge variant="outline">{OutreachChannels[draft.channel]}</Badge>
        {draft.kind === 1 && <Badge variant="secondary">Call prep</Badge>}
        <Badge variant="outline">{DraftStatuses[draft.status] ?? draft.status}</Badge>
        {draft.isAiGenerated ? (
          <EvidenceChip kind="suggested" label="AI draft" title="Generated by the assistant from this person's context — review before approving" />
        ) : (
          <EvidenceChip kind="user" label="Edited by you" />
        )}
        {draft.limitedContext && (
          <EvidenceChip kind="unknown" label="Light on context" title="Little stored context exists for this person — review carefully instead of trusting personalization." />
        )}
      </div>
      {draft.subject && <p className="mt-1 text-sm font-medium">Subject: {draft.subject}</p>}
      {editing ? (
        <div className="mt-1.5 flex flex-col gap-2">
          {draft.channel === 0 && (
            <Input aria-label="Draft subject" value={subject} maxLength={200} onChange={(e) => setSubject(e.target.value)} />
          )}
          <Textarea aria-label="Draft body" rows={6} value={body} maxLength={4000} onChange={(e) => setBody(e.target.value)} />
          <div className="flex gap-1.5">
            <Button size="sm" disabled={busy} onClick={() => void review(1)}>
              Save edit
            </Button>
            <Button size="sm" variant="ghost" onClick={() => { setEditing(false); setBody(draft.body); setSubject(draft.subject ?? ""); }}>
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <p className="mt-1 whitespace-pre-wrap text-sm">{draft.body}</p>
      )}
      {draft.originalBody && (
        <details className="mt-1 rounded border px-2 py-1">
          <summary className="cursor-pointer text-xs text-muted-foreground">
            View original AI draft (your edit is shown above)
          </summary>
          <p className="mt-1 whitespace-pre-wrap text-xs text-muted-foreground">{draft.originalBody}</p>
        </details>
      )}
      {draft.contextUsed && <p className="mt-1 text-xs text-muted-foreground">Grounded in: {draft.contextUsed}</p>}
      {error && <p className="mt-1 text-xs text-destructive">{error}</p>}
      <div className="mt-1.5 flex flex-wrap gap-1.5">
        {draft.status !== 2 && draft.status !== 3 && (
          <>
            {!editing && (
              <Button size="sm" variant="ghost" onClick={() => setEditing(true)}>
                Edit
              </Button>
            )}
            <Button size="sm" variant="ghost" disabled={busy} onClick={() => void regenerate()}>
              Regenerate
            </Button>
            <Button size="sm" variant="ghost" disabled={busy} onClick={() => void review(2)}>
              Approve
            </Button>
            <Button size="sm" variant="ghost" disabled={busy} onClick={() => void review(3)}>
              Reject
            </Button>
          </>
        )}
      </div>
    </li>
  );
}

export function OutreachDetail() {
  const { id } = useParams();
  const [batch, setBatch] = useState<OutreachBatch | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [channel, setChannel] = useState("0");
  const [intent, setIntent] = useState("");
  const [instruction, setInstruction] = useState("");

  const load = useCallback(async () => {
    if (!id) return;
    setError(null);
    try {
      const b = await api.get<OutreachBatch>(`/api/Outreach/GetBatch?id=${id}`);
      setBatch(b);
      setChannel(String(b.channel));
      setIntent(b.intent);
      setInstruction(b.globalInstruction ?? "");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load batch.");
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load]);

  async function saveSettings() {
    if (!batch) return;
    setError(null);
    setBusy(true);
    try {
      const updated = await api.put<OutreachBatch>(`/api/Outreach/PutBatchSettings?id=${batch.outreachBatchId}`, {
        Channel: Number(channel),
        Intent: intent.trim() === "" ? null : intent.trim(),
        GlobalInstruction: instruction.trim() === "" ? null : instruction.trim(),
      });
      setBatch(updated);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save settings.");
    } finally {
      setBusy(false);
    }
  }

  async function generate() {
    if (!batch) return;
    setError(null);
    setBusy(true);
    try {
      setBatch(await api.post<OutreachBatch>(`/api/Outreach/PostBatchDrafts?id=${batch.outreachBatchId}`, {}));
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not generate drafts.");
    } finally {
      setBusy(false);
    }
  }

  async function approveAll() {
    if (!batch) return;
    setError(null);
    setBusy(true);
    try {
      setBatch(await api.post<OutreachBatch>(`/api/Outreach/PostBatchApprove?id=${batch.outreachBatchId}`, {}));
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not approve.");
    } finally {
      setBusy(false);
    }
  }

  function updateDraft(updated: CommunicationDraft) {
    setBatch((b) => (b ? { ...b, drafts: b.drafts.map((d) => (d.communicationDraftId === updated.communicationDraftId ? updated : d)) } : b));
  }

  if (error && !batch) return <ErrorState message={error} onRetry={() => void load()} />;
  if (!batch) return <LoadingList rows={6} />;

  const selected = batch.members.filter((m) => !m.excluded);
  const reviewable = batch.drafts.filter((d) => d.status === 0 || d.status === 1);
  const approved = batch.drafts.filter((d) => d.status === 2);
  const locked = batch.status !== 0 && batch.status !== 1;

  return (
    <div className="max-w-3xl">
      <NavButton to="/outreach" variant="ghost" size="sm" className="mb-4">
        ← Outreach
      </NavButton>
      <h1 className="font-display text-3xl font-semibold tracking-tight">{batch.intent}</h1>
      <p className="mt-1 text-sm text-muted-foreground">
        {selected.length} selected · {batch.drafts.length} drafts · {approved.length} approved.
        Approval never contacts anyone — log the interaction yourself afterwards.
      </p>
      {error && <p className="mt-2 text-sm text-destructive">{error}</p>}

      <section className="mt-6">
        <h2 className="mb-1 text-base font-semibold">1 · Who to contact</h2>
        <p className="mb-2 text-xs text-muted-foreground">
          Each person carries the reason they were suggested. Remove anyone who should not be contacted.
        </p>
        <ul className="flex flex-col gap-2">
          {batch.members.map((m) => (
            <MemberRow key={m.outreachBatchMemberId} batchId={batch.outreachBatchId} member={m} onChanged={setBatch} />
          ))}
        </ul>
      </section>

      <Separator className="my-6" />

      <section>
        <h2 className="mb-1 text-base font-semibold">2 · How to reach them</h2>
        <p className="mb-2 text-xs text-muted-foreground">
          One decision for the whole batch — email, LinkedIn, text, or call preparation. Individuals can override above.
        </p>
        <div className="grid gap-2 rounded-lg border p-3 sm:grid-cols-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ob-channel">Channel</Label>
            <Select value={channel} onValueChange={(v) => setChannel(v ?? "0")} disabled={locked}>
              <SelectTrigger id="ob-channel">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {OutreachChannels.map((c, i) => (
                  <SelectItem key={c} value={String(i)}>
                    {c}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ob-intent">Intent</Label>
            <Input id="ob-intent" value={intent} maxLength={500} disabled={locked} onChange={(e) => setIntent(e.target.value)} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ob-instr">Global instruction (optional)</Label>
            <Input id="ob-instr" value={instruction} maxLength={1000} disabled={locked} onChange={(e) => setInstruction(e.target.value)} placeholder="e.g. Keep it casual and short" />
          </div>
        </div>
        {!locked && (
          <div className="mt-2 flex gap-2">
            <Button size="sm" variant="outline" disabled={busy} onClick={() => void saveSettings()}>
              Save settings
            </Button>
            <Button size="sm" disabled={busy || selected.length === 0} onClick={() => void generate()}>
              {busy ? "Preparing…" : `Prepare ${selected.length} personalized draft${selected.length === 1 ? "" : "s"}`}
            </Button>
          </div>
        )}
      </section>

      <Separator className="my-6" />

      <section>
        <h2 className="mb-1 text-base font-semibold">3 · Review every draft</h2>
        <p className="mb-2 text-xs text-muted-foreground">
          Each draft is grounded in that person&apos;s context — or flagged when context is thin. Edit, regenerate, approve, or reject individually.
        </p>
        {batch.drafts.length === 0 && (
          <p className="text-sm text-muted-foreground">No drafts yet — prepare them above.</p>
        )}
        <ul className="flex flex-col gap-2">
          {batch.drafts.map((d) => (
            <DraftCard key={d.communicationDraftId} draft={d} onChanged={updateDraft} />
          ))}
        </ul>
        {!locked && reviewable.length > 0 && (
          <div className="mt-3">
            <Button size="sm" disabled={busy} onClick={() => void approveAll()}>
              Approve all reviewed ({reviewable.length})
            </Button>
          </div>
        )}
        {approved.length > 0 && (
          <div className="mt-3 rounded-lg border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm text-emerald-900">
            {approved.length} approved. Nothing was sent — when each conversation happens, log it as an interaction so the relationship learns from it.
          </div>
        )}
      </section>
    </div>
  );
}
