import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { EmptyState, ErrorState, LoadingList, NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { OutreachChannels } from "@/lib/types";
import type { OutreachBatch } from "@/lib/types";

const SIGNAL_OPTIONS = [
  { value: "attentionQueue", label: "Attention queue" },
  { value: "outsideCadence", label: "Outside normal rhythm" },
  { value: "neglected", label: "Neglected" },
  { value: "recentMeetings", label: "After recent meetings" },
  { value: "upcomingEvents", label: "Upcoming events" },
  { value: "pendingCommitments", label: "Pending commitments" },
] as const;

const BATCH_STATUSES = ["Draft", "Ready", "Approved", "Discarded"] as const;

export function Outreach() {
  const navigate = useNavigate();
  const [batches, setBatches] = useState<OutreachBatch[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [signals, setSignals] = useState<string[]>(["attentionQueue"]);
  const [nlText, setNlText] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setError(null);
    try {
      setBatches(await api.get<OutreachBatch[]>("/api/Outreach/GetBatches"));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load batches.");
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  function toggleSignal(value: string) {
    setSignals((s) => (s.includes(value) ? s.filter((x) => x !== value) : [...s, value]));
  }

  async function buildFromSignals() {
    if (signals.length === 0) return;
    setError(null);
    setBusy(true);
    try {
      const batch = await api.post<OutreachBatch>("/api/Outreach/PostBatchFromSignals", {
        SignalFilters: signals,
        TimeWindowDays: 14,
        MaxMembers: 12,
        Intent: "Reconnect",
      });
      navigate(`/outreach/${batch.outreachBatchId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not build batch.");
    } finally {
      setBusy(false);
    }
  }

  async function buildFromNl(e: React.FormEvent) {
    e.preventDefault();
    if (nlText.trim() === "") return;
    setError(null);
    setBusy(true);
    try {
      const batch = await api.post<OutreachBatch>("/api/Outreach/PostBatchFromNL", {
        Text: nlText.trim(),
      });
      navigate(`/outreach/${batch.outreachBatchId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not build batch.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-semibold tracking-tight">Outreach</h1>
        <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
          The system suggests who is worth contacting and why. You choose the people, pick one
          communication mode, review every personalized draft, and approve — nothing sends by itself.
        </p>
      </div>

      <div className="mb-6 grid gap-4 lg:grid-cols-2">
        <form onSubmit={buildFromNl} className="flex flex-col gap-2 rounded-lg border p-4">
          <Label htmlFor="outreach-nl">Describe who to reach</Label>
          <Input
            id="outreach-nl"
            value={nlText}
            onChange={(e) => setNlText(e.target.value)}
            placeholder="e.g. Everyone I should reconnect with this week, by email"
            maxLength={500}
          />
          <div>
            <Button type="submit" size="sm" disabled={busy || nlText.trim() === ""}>
              {busy ? "Building…" : "Build batch"}
            </Button>
          </div>
        </form>
        <div className="flex flex-col gap-2 rounded-lg border p-4">
          <span className="text-sm font-medium">Or pick signals directly</span>
          <div className="flex flex-wrap gap-1.5">
            {SIGNAL_OPTIONS.map((s) => {
              const on = signals.includes(s.value);
              return (
                <button
                  key={s.value}
                  type="button"
                  aria-pressed={on}
                  onClick={() => toggleSignal(s.value)}
                  className={`rounded-full border px-2.5 py-1 text-xs transition-colors ${
                    on ? "border-primary bg-primary text-primary-foreground" : "border-border hover:border-primary"
                  }`}
                >
                  {s.label}
                </button>
              );
            })}
          </div>
          <div>
            <Button size="sm" variant="outline" disabled={busy || signals.length === 0} onClick={() => void buildFromSignals()}>
              {busy ? "Building…" : "Build batch"}
            </Button>
          </div>
        </div>
      </div>

      {error && (
        <div className="mb-4">
          <ErrorState message={error} onRetry={() => void load()} />
        </div>
      )}
      {batches === null && !error && <LoadingList rows={3} />}
      {batches !== null && batches.length === 0 && (
        <EmptyState
          title="No outreach batches yet"
          body="Build your first batch above — from a plain request or from intelligence signals."
        />
      )}
      {batches !== null && batches.length > 0 && (
        <ul className="flex flex-col gap-2">
          {batches.map((b) => (
            <li key={b.outreachBatchId} className="flex items-center gap-3 rounded-lg border px-4 py-3">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Link to={`/outreach/${b.outreachBatchId}`} className="truncate text-sm font-semibold hover:underline">
                    {b.intent} · {OutreachChannels[b.channel] ?? b.channel}
                  </Link>
                  <Badge variant="outline">{BATCH_STATUSES[b.status] ?? b.status}</Badge>
                </div>
                <p className="mt-0.5 text-xs tabular-nums text-muted-foreground">
                  {b.members.filter((m) => !m.excluded).length} selected · {b.drafts.length} drafts · {formatDate(b.createdAtUtc)}
                </p>
              </div>
              <NavButton to={`/outreach/${b.outreachBatchId}`} size="sm" variant="ghost">
                Open
              </NavButton>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
