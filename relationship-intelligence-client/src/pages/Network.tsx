import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { EmptyState, ErrorState, LoadingList } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import type { NetworkGraph } from "@/lib/types";

const WIDTH = 900;
const ROW_H = 34;

function urgencyColor(u: number): string {
  if (u > 85) return "#dc2626";
  if (u > 65) return "#d97706";
  if (u >= 40) return "#0284c7";
  return "#059669";
}

export function Network() {
  const [graph, setGraph] = useState<NetworkGraph | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      setGraph(await api.get<NetworkGraph>("/api/Network/GetNetworkGraph"));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load the network.");
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const layout = useMemo(() => {
    if (!graph) return null;
    // Deterministic layout: nodes grouped by connected component, laid out in
    // columns; isolates in a final column. Edges drawn between node rows.
    const adj = new Map<string, Set<string>>();
    for (const n of graph.Nodes) adj.set(n.PersonId, new Set());
    for (const e of graph.Edges) {
      adj.get(e.From)?.add(e.To);
      adj.get(e.To)?.add(e.From);
    }
    const visited = new Set<string>();
    const clusters: string[][] = [];
    for (const n of graph.Nodes) {
      if (visited.has(n.PersonId)) continue;
      const cluster: string[] = [];
      const queue = [n.PersonId];
      visited.add(n.PersonId);
      while (queue.length > 0) {
        const id = queue.shift()!;
        cluster.push(id);
        for (const next of adj.get(id) ?? []) {
          if (!visited.has(next)) {
            visited.add(next);
            queue.push(next);
          }
        }
      }
      clusters.push(cluster);
    }
    clusters.sort((a, b) => b.length - a.length);
    const pos = new Map<string, { x: number; y: number }>();
    const colW = Math.max(220, WIDTH / Math.max(1, clusters.length));
    clusters.forEach((cluster, ci) => {
      cluster.forEach((id, ri) => {
        pos.set(id, { x: 110 + ci * colW, y: 30 + ri * ROW_H });
      });
    });
    const height = Math.max(120, Math.max(...clusters.map((c) => c.length)) * ROW_H + 40);
    return { pos, clusters, height };
  }, [graph]);

  const selectedNode = selected
    ? graph?.Nodes.find((n) => n.PersonId === selected) ?? null
    : null;
  const selectedEdges = useMemo(() => {
    if (!graph || !selected) return [];
    return graph.Edges.filter((e) => e.From === selected || e.To === selected);
  }, [graph, selected]);

  return (
    <div>
      <div className="mb-4 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Network</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            {graph
              ? `${graph.Nodes.length} people in ${graph.ClusterCount} separate groups. Ringed nodes are bridges — they connect parts of your network that would otherwise be disconnected, and research shows bridges are lost faster.`
              : "Your contacts as a map, grouped by shared organizations, tags, and channels."}
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => void load()}>
          Refresh
        </Button>
      </div>

      {error && <ErrorState message={error} onRetry={() => void load()} />}
      {graph === null && !error && <LoadingList rows={4} />}
      {graph !== null && graph.Nodes.length === 0 && (
        <EmptyState
          title="No network yet"
          body="Add people with organizations, tags, or channels and connections will appear here."
        />
      )}
      {graph !== null && graph.Nodes.length > 0 && layout && (
        <div className="grid gap-4 xl:grid-cols-[1fr_300px]">
          <div className="overflow-x-auto rounded-lg border">
            <svg
              width={WIDTH}
              height={layout.height}
              className="block min-w-full bg-card"
              role="img"
              aria-label="Contact network graph"
            >
              {layout.clusters.map((cluster, ci) => (
                <text
                  key={ci}
                  x={110 + ci * Math.max(220, WIDTH / Math.max(1, layout.clusters.length))}
                  y={14}
                  textAnchor="middle"
                  className="fill-muted-foreground"
                  fontSize={11}
                >
                  {cluster.length === 1 ? "Unconnected" : `Group ${ci + 1} · ${cluster.length}`}
                </text>
              ))}
              {graph.Edges.map((e, i) => {
                const a = layout.pos.get(e.From);
                const b = layout.pos.get(e.To);
                if (!a || !b) return null;
                const active = selected == null || e.From === selected || e.To === selected;
                return (
                  <line
                    key={i}
                    x1={a.x}
                    y1={a.y}
                    x2={b.x}
                    y2={b.y}
                    stroke={active ? "#94a3b8" : "#e2e8f0"}
                    strokeWidth={active ? 1.5 : 1}
                  >
                    <title>{e.Reason}</title>
                  </line>
                );
              })}
              {graph.Nodes.map((n) => {
                const p = layout.pos.get(n.PersonId);
                if (!p) return null;
                const dim = selected != null && n.PersonId !== selected &&
                  !selectedEdges.some((e) => e.From === n.PersonId || e.To === n.PersonId);
                return (
                  <g
                    key={n.PersonId}
                    transform={`translate(${p.x},${p.y})`}
                    onClick={() => setSelected(selected === n.PersonId ? null : n.PersonId)}
                    style={{ cursor: "pointer", opacity: dim ? 0.35 : 1 }}
                  >
                    {n.IsBridge && (
                      <circle r={11} fill="none" stroke="#7c3aed" strokeWidth={2} strokeDasharray="3 2" />
                    )}
                    <circle r={7} fill={urgencyColor(n.UrgencyScore)} />
                    <text x={14} y={4} fontSize={12} className="fill-foreground">
                      {n.Name}
                    </text>
                  </g>
                );
              })}
            </svg>
          </div>
          <aside className="rounded-lg border p-4">
            {selectedNode == null ? (
              <p className="text-sm text-muted-foreground">
                Select a node to inspect it. Color shows urgency (green → red).
                Dashed rings mark bridges.
              </p>
            ) : (
              <div className="flex flex-col gap-2">
                <Link
                  to={`/people/${selectedNode.PersonId}`}
                  className="text-sm font-semibold hover:underline"
                >
                  {selectedNode.Name}
                </Link>
                <div className="flex gap-1.5">
                  {selectedNode.IsBridge && <Badge variant="secondary">Bridge</Badge>}
                  {selectedNode.IsIsolated && <Badge variant="outline">Unconnected</Badge>}
                  <Badge variant="outline">{selectedNode.Degree} connections</Badge>
                </div>
                <p className="text-xs text-muted-foreground">
                  Urgency {Math.round(selectedNode.UrgencyScore)}/100
                </p>
                {selectedEdges.length > 0 && (
                  <>
                    <p className="mt-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                      Connected through
                    </p>
                    <ul className="flex flex-col gap-1">
                      {selectedEdges.slice(0, 8).map((e, i) => (
                        <li key={i} className="text-xs text-muted-foreground">
                          {e.Reason}
                        </li>
                      ))}
                    </ul>
                  </>
                )}
              </div>
            )}
          </aside>
        </div>
      )}
    </div>
  );
}
