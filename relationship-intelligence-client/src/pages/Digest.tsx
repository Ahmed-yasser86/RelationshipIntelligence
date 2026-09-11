import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { BandBadge, EmptyState, ErrorState, LoadingList, PersonAvatar } from "@/components/states";
import { cn } from "cn";
import { ApiError, api } from "@/lib/api";
import { formatDate } from "@/lib/format";
import type { DigestPayload, DigestPreference } from "@/lib/types";

function Preferences({
  pref,
  onSaved,
}: {
  pref: DigestPreference;
  onSaved: () => void;
}) {
  const [enabled, setEnabled] = useState(pref.enabled);
  const [threshold, setThreshold] = useState(pref.threshold);
  const [count, setCount] = useState(pref.count);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSaved(false);
    setBusy(true);
    try {
      await api.post("/api/Digest/PostDigestPreference", {
        Enabled: enabled,
        Threshold: threshold,
        Count: count,
      });
      setSaved(true);
      onSaved();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} className="flex flex-wrap items-end gap-3 rounded-lg border p-4">
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={enabled}
          onChange={(e) => setEnabled(e.target.checked)}
        />
        Weekly email enabled
      </label>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="th">Minimum urgency</Label>
        <Input
          id="th"
          type="number"
          min={0}
          max={100}
          className="w-24"
          value={threshold}
          onChange={(e) => setThreshold(Number(e.target.value))}
        />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="ct">Contacts per digest</Label>
        <Input
          id="ct"
          type="number"
          min={1}
          max={7}
          className="w-24"
          value={count}
          onChange={(e) => setCount(Number(e.target.value))}
        />
      </div>
      <Button type="submit" size="sm" disabled={busy}>
        {busy ? "Saving…" : "Save"}
      </Button>
      {saved && <span className="text-xs text-emerald-700">Saved.</span>}
      {error && <span className="text-xs text-destructive">{error}</span>}
    </form>
  );
}

export function Digest() {
  const [payload, setPayload] = useState<DigestPayload | null>(null);
  const [pref, setPref] = useState<DigestPreference | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const [sendResult, setSendResult] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      const [p, pr] = await Promise.all([
        api.get<DigestPayload>("/api/Digest/GetWeeklyDigest"),
        api.get<DigestPreference>("/api/Digest/GetDigestPreference"),
      ]);
      setPayload(p);
      setPref(pr);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load the digest.");
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function send() {
    setSending(true);
    setSendResult(null);
    try {
      const res = await api.post<DigestPayload | string>("/api/Digest/PostSendDigest");
      setSendResult(typeof res === "string" ? res : `Sent — ${res.entries.length} contacts.`);
    } catch (err) {
      setSendResult(err instanceof ApiError ? err.body || err.message : "Send failed.");
    } finally {
      setSending(false);
    }
  }

  return (
    <div>
      <div className="mb-4 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Weekly digest</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            {payload
              ? `Week of ${formatDate(payload.weekStartUtc)} · network health ${payload.networkHealth}/100. Same selection the email carries — act here or from your inbox.`
              : "The five relationships to protect this week."}
          </p>
        </div>
        <Button size="sm" disabled={sending} onClick={() => void send()}>
          {sending ? "Sending…" : "Send email now"}
        </Button>
      </div>
      {sendResult && <p className="mb-3 text-sm text-muted-foreground">{sendResult}</p>}

      {error && <ErrorState message={error} onRetry={() => void load()} />}
      {payload === null && !error && <LoadingList rows={5} />}
      {payload !== null && payload.entries.length === 0 && (
        <EmptyState
          title="A quiet week"
          body="No relationship crosses your urgency threshold right now. The digest will reappear the moment something needs protection."
        />
      )}
      {payload !== null && payload.entries.length > 0 && (
        <ol className="mb-6 flex flex-col gap-3">
          {payload.entries.map((entry) => (
            <li key={entry.health.personId} className="rounded-lg border px-4 py-3">
              <div className="flex items-center gap-3">
                <PersonAvatar name={entry.health.name} />
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <Link
                      to={`/people/${entry.health.personId}`}
                      className="text-sm font-semibold hover:underline"
                    >
                      {entry.health.name ?? "Unnamed contact"}
                    </Link>
                    <BandBadge band={entry.health.band} />
                  </div>
                  <p className="mt-0.5 text-sm text-muted-foreground">{entry.suggestion}</p>
                </div>
                <a
                  href={entry.actionUrl}
                  target="_blank"
                  rel="noreferrer"
                  className={cn(buttonVariants({ size: "sm", variant: "outline" }))}
                >
                  I reached out
                </a>
              </div>
            </li>
          ))}
        </ol>
      )}

      <h2 className="mb-2 text-base font-semibold">Preferences</h2>
      {pref && <Preferences pref={pref} onSaved={() => void load()} />}
    </div>
  );
}
