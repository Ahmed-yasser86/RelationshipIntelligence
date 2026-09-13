import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { ApiError, api } from "@/lib/api";
import { useCopilot } from "@/lib/copilot";
import type { BriefingPayload } from "@/lib/types";

const CACHE_KEY = "ri.briefing";
const CACHE_MS = 15 * 60_000;

interface Cached {
  at: number;
  payload: BriefingPayload;
}

function readCache(): BriefingPayload | null {
  try {
    const raw = localStorage.getItem(CACHE_KEY);
    if (!raw) return null;
    const cached = JSON.parse(raw) as Cached;
    if (Date.now() - cached.at > CACHE_MS) return null;
    return cached.payload;
  } catch {
    return null;
  }
}

export function BriefingBlock() {
  const { openCopilot } = useCopilot();
  const [briefing, setBriefing] = useState<BriefingPayload | null>(() => readCache());
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(briefing === null);

  useEffect(() => {
    if (briefing !== null) return;
    let cancelled = false;
    (async () => {
      try {
        const payload = await api.post<BriefingPayload>("/api/Copilot/PostBriefing");
        if (cancelled) return;
        setBriefing(payload);
        try {
          localStorage.setItem(CACHE_KEY, JSON.stringify({ at: Date.now(), payload } satisfies Cached));
        } catch {
          /* cache is best-effort */
        }
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiError ? err.body || err.message : "Briefing unavailable.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function refresh() {
    setLoading(true);
    setError(null);
    try {
      const payload = await api.post<BriefingPayload>("/api/Copilot/PostBriefing");
      setBriefing(payload);
      try {
        localStorage.setItem(CACHE_KEY, JSON.stringify({ at: Date.now(), payload } satisfies Cached));
      } catch {
        /* cache is best-effort */
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Briefing unavailable.");
    } finally {
      setLoading(false);
    }
  }

  if (loading && briefing === null) {
    return (
      <section aria-label="Briefing">
        <h2 className="mb-2 font-display text-xl font-semibold">Today&apos;s briefing</h2>
        <p className="text-sm text-muted-foreground">Composing your briefing…</p>
      </section>
    );
  }

  if (error && briefing === null) {
    return (
      <section aria-label="Briefing">
        <h2 className="mb-2 font-display text-xl font-semibold">Today&apos;s briefing</h2>
        <p className="text-sm text-muted-foreground">{error}</p>
        <div className="mt-2">
          <Button size="sm" variant="outline" onClick={() => void refresh()}>
            Retry
          </Button>
        </div>
      </section>
    );
  }

  if (briefing === null) return null;

  return (
    <section aria-label="Briefing">
      <div className="mb-2 flex items-center justify-between">
        <h2 className="font-display text-xl font-semibold">Today&apos;s briefing</h2>
        <div className="flex gap-2">
          <Button size="sm" variant="ghost" onClick={() => openCopilot()}>
            Discuss with co-pilot
          </Button>
          <Button size="sm" variant="ghost" onClick={() => void refresh()}>
            Refresh
          </Button>
        </div>
      </div>
      <div className="rounded-lg border px-4 py-3">
        <p className="text-sm">{briefing.summary}</p>
        {briefing.attentionNow.length > 0 && (
          <ul className="mt-2 space-y-1">
            {briefing.attentionNow.map((a) => (
              <li key={a.personId} className="text-sm">
                <Link to={`/people/${a.personId}`} className="font-medium hover:underline">
                  {a.name}
                </Link>
                <span className="text-muted-foreground"> — {a.reason}</span>
              </li>
            ))}
          </ul>
        )}
        {briefing.followUpsDue.length > 0 && (
          <div className="mt-2">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Follow-ups due</p>
            <ul className="mt-1 space-y-0.5">
              {briefing.followUpsDue.map((f, i) => (
                <li key={i} className="text-sm text-muted-foreground">{f}</li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </section>
  );
}
