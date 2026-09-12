import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { EmptyState, ErrorState, NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
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

export function Overview() {
  const [snapshot, setSnapshot] = useState<Snapshot | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [queue, graph, digest] = await Promise.all([
          api.get<RelationshipHealth[]>("/api/Contacts/GetRelationshipQueue?top=50"),
          api.get<NetworkGraph>("/api/Network/GetNetworkGraph"),
          api.get<{ entries: unknown[] }>("/api/Digest/GetWeeklyDigest"),
        ]);
        if (cancelled) return;
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
  }, []);

  return (
    <div className="flex max-w-3xl flex-col gap-8">
      <section>
        <h1 className="text-3xl font-semibold tracking-tight">Overview</h1>
        <p className="mt-2 max-w-2xl text-[15px] leading-relaxed text-muted-foreground">
          This is <strong className="text-foreground">relationship intelligence</strong>,
          not a contact manager. Every contact you log builds a temporal record; an
          exponential tie-decay model scores each relationship against its own rhythm;
          and the system tells you <strong className="text-foreground">who is cooling,
          why the evidence says so, and what to do this week</strong>.
        </p>
        <ol className="mt-4 grid gap-2 sm:grid-cols-4">
          {[
            ["Observe", "Log interactions as they happen"],
            ["Understand", "See rhythm, drift, and state"],
            ["Discover", "Find bridges, clusters, and risks"],
            ["Act", "Work the weekly queue"],
          ].map(([step, desc]) => (
            <li key={step} className="rounded-lg border px-3 py-2.5">
              <p className="text-sm font-semibold">{step}</p>
              <p className="mt-0.5 text-xs text-muted-foreground">{desc}</p>
            </li>
          ))}
        </ol>
      </section>

      {error && <ErrorState message={error} />}
      {snapshot === null && !error && (
        <p className="text-sm text-muted-foreground">Loading your network snapshot…</p>
      )}
      {snapshot !== null && (
        <section aria-label="This week">
          <h2 className="mb-2 text-base font-semibold">This week in your network</h2>
          <div className="flex flex-wrap items-center gap-x-6 gap-y-2 rounded-lg border px-4 py-3 text-sm">
            <span>
              <strong className="text-red-700">{snapshot.critical} critical</strong>
              {" · "}
              <strong className="text-amber-700">{snapshot.atRisk} at risk</strong>
              {" · "}
              <span className="text-muted-foreground">{snapshot.drifting} drifting</span>
            </span>
            <span className="text-muted-foreground">
              {snapshot.nodes} people · {snapshot.edges} connections ·{" "}
              {snapshot.clusters} groups · {snapshot.bridges} bridges
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

      <section aria-label="Explore">
        <h2 className="mb-2 text-base font-semibold">Explore</h2>
        <div className="grid gap-2 sm:grid-cols-3">
          <Link to="/network" className="rounded-lg border px-4 py-3 hover:bg-secondary/50">
            <p className="text-sm font-semibold">Network structure</p>
            <p className="mt-0.5 text-xs text-muted-foreground">
              Clusters, bridges, and isolates — who connects your worlds.
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
        <h2 className="mb-2 text-base font-semibold">How the scores work</h2>
        <div className="rounded-lg border px-4 py-3 text-sm text-muted-foreground">
          <p>
            Tie strength decays exponentially between logged interactions (60-day
            half-life) and rises equally with each one — no per-channel weights, no
            hidden adjustments. Urgency is the deficit against your strongest tie.
            Bands, bridges, and rhythms are display aids over this single dynamic.
          </p>
          <a
            href="https://github.com/Ahmed-yasser86/RelationshipIntelligence/blob/relationship-intelligence-main/RELATIONSHIP_INTELLIGENCE_METHODOLOGY.md"
            target="_blank"
            rel="noreferrer"
            className="mt-1 inline-block text-sm font-medium text-foreground underline"
          >
            Read the full methodology →
          </a>
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
