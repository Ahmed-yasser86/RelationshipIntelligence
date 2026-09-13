import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { EmptyState, ErrorState, NavButton } from "@/components/states";
import { AttentionRow } from "@/components/attention-row";
import { BriefingBlock } from "@/components/briefing-block";
import { QuickLog } from "@/components/quick-log";
import { ApiError, api } from "@/lib/api";
import { useCopilot } from "@/lib/copilot";
import { silenceDays } from "@/lib/format";
import { EventTypes } from "@/lib/types";
import type { NetworkGraph, RelationshipHealth } from "@/lib/types";

interface Snapshot {
  critical: number;
  atRisk: number;
  drifting: number;
  nodes: number;
  edges: number;
  clusters: number;
  bridges: number;
  digestCount: number;
}

interface ComingUp {
  personId: string;
  personName: string | null;
  title: string;
  type: number;
  inDays: number;
  silence: string;
  hasSignal: boolean;
}

export function Overview() {
  const navigate = useNavigate();
  const { openCopilot } = useCopilot();
  const [snapshot, setSnapshot] = useState<Snapshot | null>(null);
  const [topAttention, setTopAttention] = useState<RelationshipHealth[]>([]);
  const [comingUp, setComingUp] = useState<ComingUp[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadQueue = useCallback(async () => {
    try {
      // top=200 covers the full network so band counts are totals, not a
      // capped subset. Display still slices top 3.
      const queue = await api.get<RelationshipHealth[]>("/api/Contacts/GetRelationshipQueue?top=200");
      setTopAttention(queue.slice(0, 3));
      return queue;
    } catch {
      return null;
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [queue, graph, digest] = await Promise.all([
          loadQueue(),
          api.get<NetworkGraph>("/api/Network/GetNetworkGraph"),
          api.get<{ entries: unknown[] }>("/api/Digest/GetWeeklyDigest"),
        ]);
        if (cancelled) return;
        if (queue === null) throw new Error("Could not load the overview.");
        const upcoming: ComingUp[] = [];
        for (const q of queue) {
          for (const ev of q.upcomingEvents ?? []) {
            const silent = silenceDays(q.silenceDays, q.lastContactAtUtc);
            upcoming.push({
              personId: q.personId,
              personName: q.name,
              title: ev.title,
              type: ev.type,
              inDays: ev.inDays,
              silence:
                silent == null
                  ? "no contact recorded"
                  : silent === 0
                    ? "in touch today"
                    : `quiet for ${silent}d`,
              hasSignal: q.hasEventSignal,
            });
          }
        }
        upcoming.sort((a, b) => a.inDays - b.inDays);
        if (!cancelled) setComingUp(upcoming.slice(0, 8));
        setSnapshot({
          critical: queue.filter((q) => q.band === "Critical").length,
          atRisk: queue.filter((q) => q.band === "AtRisk").length,
          drifting: queue.filter((q) => q.band === "Drifting").length,
          nodes: graph.nodes.length,
          edges: graph.edges.length,
          clusters: graph.clusterCount,
          bridges: graph.nodes.filter((n) => n.isBridge).length,
          digestCount: digest.entries.length,
        });
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : "Could not load the overview.");
        }
      }
    })();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="flex max-w-3xl flex-col gap-8">
      <section>
        <h1 className="font-display text-4xl font-semibold tracking-tight">Today</h1>
        <p className="mt-2 max-w-2xl text-[15px] leading-relaxed text-muted-foreground">
          This is <strong className="text-foreground">relationship intelligence</strong>,
          not a contact manager. Who is cooling, why the evidence says so, and what
          to do this week.
        </p>
      </section>

      {error && <ErrorState message={error} />}
      {snapshot === null && !error && (
        <p className="text-sm text-muted-foreground">Loading your network snapshot…</p>
      )}
      <BriefingBlock />

      {topAttention.length > 0 && (
        <section aria-label="Top attention">
          <div className="mb-2 flex items-center justify-between">
            <h2 className="font-display text-xl font-semibold">Needs attention first</h2>
            <NavButton to="/attention" size="sm" variant="ghost">
              Full queue →
            </NavButton>
          </div>
          <ol className="flex flex-col gap-3">
            {topAttention.map((item, i) => (
              <AttentionRow
                key={item.personId}
                item={item}
                rank={i + 1}
                onLogDone={() => void loadQueue()}
                onExplain={() => navigate("/attention")}
                onAsk={(entry) => openCopilot({ personId: entry.personId, personName: entry.name })}
                logAction={(person, onDone) => <QuickLog person={person} onDone={onDone} />}
              />
            ))}
          </ol>
        </section>
      )}
      {snapshot !== null && (
        <section aria-label="This week">
          <h2 className="mb-2 font-display text-xl font-semibold">This week in your network</h2>
          <div className="flex flex-wrap items-center gap-x-6 gap-y-2 rounded-lg border bg-card px-4 py-3 text-sm">
            <span className="tnum">
              <strong className="text-red-700">{snapshot.critical} critical</strong>
              {" · "}
              <strong className="text-amber-700">{snapshot.atRisk} at risk</strong>
              {" · "}
              <span className="text-muted-foreground">{snapshot.drifting} drifting</span>
            </span>
            <span className="text-muted-foreground">
              {snapshot.nodes} people · {snapshot.edges} shared-context connections ·{" "}
              {snapshot.clusters} shared-context groups · {snapshot.bridges} articulation points
            </span>
            <span className="text-muted-foreground">
              Digest holds {snapshot.digestCount} relationships to protect
            </span>
            <NavButton to="/attention" size="sm" className="ml-auto">
              Open the attention queue
            </NavButton>
          </div>
        </section>
      )}

      {comingUp !== null && comingUp.length > 0 && (
        <section aria-label="Coming up">
          <h2 className="mb-2 font-display text-xl font-semibold">Coming up</h2>
          <ul className="flex flex-col gap-2">
            {comingUp.map((c) => (
              <li key={`${c.personId}-${c.title}`} className="rounded-lg border px-4 py-2.5 text-sm">
                <div className="flex flex-wrap items-center gap-x-2">
                  <Link to={`/people/${c.personId}`} className="font-semibold hover:underline">
                    {c.personName ?? "Unnamed contact"}
                  </Link>
                  <span className="text-muted-foreground">
                    {c.title} ({EventTypes[c.type] ?? c.type}){" "}
                    {c.inDays === 0 ? "today" : `in ${c.inDays}d`}
                  </span>
                  {c.hasSignal && (
                    <span className="rounded-full border border-amber-300 bg-amber-50 px-2 py-0.5 text-xs text-amber-800">
                      needs attention too
                    </span>
                  )}
                </div>
                <p className="mt-0.5 text-xs text-muted-foreground">{c.silence}</p>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section aria-label="Explore">
        <h2 className="mb-2 font-display text-xl font-semibold">Explore</h2>
        <div className="grid gap-2 sm:grid-cols-3">
          <Link to="/network" className="rounded-lg border px-4 py-3 hover:bg-secondary/50">
            <p className="text-sm font-semibold">Network structure</p>
            <p className="mt-0.5 text-xs text-muted-foreground">
              Shared-context groups, articulation points, and isolates — who connects your worlds.
            </p>
          </Link>
          <Link to="/people" className="rounded-lg border px-4 py-3 hover:bg-secondary/50">
            <p className="text-sm font-semibold">People &amp; history</p>
            <p className="mt-0.5 text-xs text-muted-foreground">
              Searchable records with full interaction timelines.
            </p>
          </Link>
          <Link to="/digest" className="rounded-lg border px-4 py-3 hover:bg-secondary/50">
            <p className="text-sm font-semibold">Weekly digest</p>
            <p className="mt-0.5 text-xs text-muted-foreground">
              The same selection by email, with one-click logging.
            </p>
          </Link>
        </div>
      </section>

      <section aria-label="Method">
        <h2 className="mb-2 font-display text-xl font-semibold">How the scores work</h2>
        <div className="rounded-lg border px-4 py-3 text-sm text-muted-foreground">
          <p>
            Tie strength decays exponentially between logged interactions (60-day
            half-life) and rises equally with each one — no per-channel weights, no
            hidden adjustments. Urgency is the deficit against your strongest tie.
            Bands, articulation points, and rhythms are display aids over this single dynamic.
          </p>
        </div>
      </section>

      {snapshot !== null && snapshot.critical + snapshot.atRisk === 0 && (
        <EmptyState
          title="A quiet network"
          body="Nothing currently needs attention. The model is watching — drift shows up here weeks before it is felt."
        />
      )}
    </div>
  );
}
