import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { EvidenceChip } from "@/components/evidence-chip";
import { ApiError, api } from "@/lib/api";
import { daysSince } from "@/lib/format";
import type { EventOccurrence, MemoryEntryResponse, OutreachBatch, RelationshipHealth } from "@/lib/types";

export function NextStep({
  personId,
  personName,
  state,
  logAction,
  planAction,
}: {
  personId: string;
  personName: string | null;
  state: RelationshipHealth | null;
  logAction: React.ReactNode;
  planAction: React.ReactNode;
}) {
  const navigate = useNavigate();
  const [events, setEvents] = useState<EventOccurrence[] | null>(null);
  const [commitments, setCommitments] = useState<string[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [upcoming, memory] = await Promise.all([
          api.get<EventOccurrence[]>("/api/Contacts/GetUpcomingEvents?days=21"),
          api.get<MemoryEntryResponse[]>(`/api/Contacts/GetRelationshipMemory?personId=${personId}`),
        ]);
        if (cancelled) return;
        setEvents(upcoming.filter((e) => e.personId === personId));
        setCommitments(
          memory.filter((m) => m.status === 0 && m.kind === 5).map((m) => m.title),
        );
      } catch {
        if (!cancelled) {
          setEvents([]);
          setCommitments([]);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [personId]);

  const silent = daysSince(state?.lastContactAtUtc);
  const pastRhythm =
    state?.cadenceReferenceDays != null && silent != null && silent > state.cadenceReferenceDays;
  const soonEvent = (events ?? []).find((e) => e.inDays <= 7);
  const openCommitment = (commitments ?? [])[0];

  let suggestion: string;
  if (soonEvent) {
    suggestion = `${soonEvent.title} ${soonEvent.inDays === 0 ? "is today" : `is in ${soonEvent.inDays}d`}${silent != null && silent > 0 ? `, and you have been quiet for ${silent}d` : ""} — review the context, then reach out.`;
  } else if (openCommitment) {
    suggestion = `An open commitment needs you: ${openCommitment}.`;
  } else if (pastRhythm) {
    suggestion = `Quiet for ${silent}d against a ~${Math.round(state?.cadenceReferenceDays ?? 0)}d rhythm — a check-in would fit.`;
  } else if (state != null) {
    suggestion = "Within its natural rhythm — nothing is asking for action right now.";
  } else {
    suggestion = "Log the first interaction to start the observed history.";
  }

  async function draftMessage() {
    setError(null);
    setBusy(true);
    try {
      const batch = await api.post<OutreachBatch>("/api/Outreach/PostBatchFromPersons", {
        PersonIds: [personId],
        Intent: "Follow up",
      });
      navigate(`/outreach/${batch.outreachBatchId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not start outreach.");
    } finally {
      setBusy(false);
    }
  }

  async function prepareMeeting() {
    setError(null);
    setBusy(true);
    try {
      const meeting = await api.post<{ meetingId: string }>("/api/Meeting/PostMeetingPrep", {
        Title: `Meeting with ${personName ?? "contact"}`,
        OccurredAtUtc: new Date().toISOString(),
        ParticipantNames: personName ? [personName] : [],
      });
      navigate(`/meetings/${meeting.meetingId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not create preparation.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section aria-label="Suggested next step" className="rounded-xl border border-primary/25 bg-card px-4 py-3">
      <div className="mb-1 flex items-center gap-2">
        <h2 className="font-display text-lg font-semibold">What next?</h2>
        <EvidenceChip kind="derived" label="Suggested next step" title="Derived from rhythm, silence, events, and open commitments — a recommendation, not an instruction" />
      </div>
      <p className="text-sm">{suggestion}</p>
      {error && <p className="mt-1 text-xs text-destructive">{error}</p>}
      <div className="mt-2 flex flex-wrap gap-1.5">
        {logAction}
        {planAction}
        <Button size="sm" variant="outline" disabled={busy} onClick={() => void draftMessage()}>
          Draft a message
        </Button>
        <Button size="sm" variant="outline" disabled={busy} onClick={() => void prepareMeeting()}>
          Prepare a meeting
        </Button>
      </div>
    </section>
  );
}
