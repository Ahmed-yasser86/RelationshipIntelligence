import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
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
import { GraphCanvas } from "@/components/graph-canvas";
import { EmptyState, ErrorState, LoadingList, NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import type { NetworkGraph } from "@/lib/types";

const BANDS = ["All", "Healthy", "Drifting", "AtRisk", "Critical"];

export function Network() {
  const [graph, setGraph] = useState<NetworkGraph | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [searchParams, setSearchParams] = useSearchParams();
  const [query, setQuery] = useState("");
  const [band, setBand] = useState("All");
  const [bridgesOnly, setBridgesOnly] = useState(false);
  const [showIsolates, setShowIsolates] = useState(false);

  const selectedId = searchParams.get("person");
  const setSelectedId = useCallback(
    (id: string | null) => {
      setSearchParams(id ? { person: id } : {}, { replace: true });
    },
    [setSearchParams],
  );

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

  const urgencyById = useMemo(() => {
    const map = new Map<string, number>();
    for (const n of graph?.nodes ?? []) map.set(n.personId, n.urgencyScore);
    return map;
  }, [graph]);

  const bandOf = useCallback(
    (id: string): string => {
      const u = urgencyById.get(id) ?? 0;
      if (u > 85) return "Critical";
      if (u > 65) return "AtRisk";
      if (u >= 40) return "Drifting";
      return "Healthy";
    },
    [urgencyById],
  );

  const visible = useMemo(() => {
    if (!graph) return null;
    let nodes = graph.nodes;
    if (band !== "All") nodes = nodes.filter((n) => bandOf(n.personId) === band);
    if (bridgesOnly) nodes = nodes.filter((n) => n.isBridge);
    const ids = new Set(nodes.map((n) => n.personId));
    const edges = graph.edges.filter((e) => ids.has(e.from) && ids.has(e.to));
    const connected = new Set<string>();
    for (const e of edges) {
      connected.add(e.from);
      connected.add(e.to);
    }
    const isolated = nodes.filter((n) => !connected.has(n.personId));
    const shown = showIsolates ? nodes : nodes.filter((n) => connected.has(n.personId));
    const shownIds = new Set(shown.map((n) => n.personId));
    return {
      nodes: shown,
      edges: edges.filter((e) => shownIds.has(e.from) && shownIds.has(e.to)),
      isolated,
      total: nodes.length,
    };
  }, [graph, band, bridgesOnly, showIsolates, bandOf]);

  const searchMatches = useMemo(() => {
    if (!graph || query.trim() === "") return [];
    const q = query.trim().toLowerCase();
    return graph.nodes.filter((n) => (n.name ?? "").toLowerCase().includes(q)).slice(0, 8);
  }, [graph, query]);

  const selectedNode = selectedId
    ? (graph?.nodes.find((n) => n.personId === selectedId) ?? null)
    : null;
  const selectedEdges = useMemo(() => {
    if (!graph || !selectedId) return [];
    return graph.edges.filter((e) => e.from === selectedId || e.to === selectedId);
  }, [graph, selectedId]);
  const neighborNames = useMemo(() => {
    if (!graph) return new Map<string, string>();
    const map = new Map<string, string>();
    for (const n of graph.nodes) map.set(n.personId, n.name ?? "Unnamed contact");
    return map;
  }, [graph]);

  return (
    <div>
      <div className="mb-4">
        <h1 className="text-2xl font-semibold tracking-tight">Network</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          {graph
            ? `${visible?.total ?? 0} people in view (${graph.nodes.length} received) · ${graph.edges.length} connections · ${graph.clusterCount} groups. Ringed nodes are bridges — they hold otherwise separate parts of your network together, and research shows they are lost faster.`
            : "Your contacts as a map. Force layout: connected people pull together, everyone else pushes apart."}
        </p>
      </div>

      {error && <ErrorState message={error} onRetry={() => void load()} />}
      {graph === null && !error && <LoadingList rows={4} />}
      {graph !== null && graph.nodes.length === 0 && (
        <EmptyState
          title="No network yet"
          body="Add people with organizations, tags, or channels and connections will appear here."
        />
      )}

      {graph !== null && graph.nodes.length > 0 && visible && (
        <>
          <div className="mb-3 flex flex-wrap items-end gap-2">
            <div className="relative flex min-w-52 flex-1 flex-col gap-1.5">
              <Label htmlFor="net-search">Find person</Label>
              <Input
                id="net-search"
                placeholder="Type a name, then pick a match"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
              />
              {searchMatches.length > 0 && (
                <ul className="absolute top-full z-10 mt-1 max-h-48 w-full overflow-auto rounded-md border bg-popover shadow-md">
                  {searchMatches.map((m) => (
                    <li key={m.personId}>
                      <button
                        type="button"
                        className="w-full px-3 py-1.5 text-left text-sm hover:bg-secondary"
                        onClick={() => {
                          setSelectedId(m.personId);
                          setQuery("");
                        }}
                      >
                        {m.name ?? "Unnamed contact"}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="net-band">Health band</Label>
              <Select value={band} onValueChange={(v) => setBand(v ?? "All")}>
                <SelectTrigger id="net-band" className="w-36">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {BANDS.map((b) => (
                    <SelectItem key={b} value={b}>
                      {b}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <label className="flex items-center gap-1.5 text-sm">
              <input
                type="checkbox"
                checked={bridgesOnly}
                onChange={(e) => setBridgesOnly(e.target.checked)}
              />
              Bridges only
            </label>
            <label className="flex items-center gap-1.5 text-sm">
              <input
                type="checkbox"
                checked={showIsolates}
                onChange={(e) => setShowIsolates(e.target.checked)}
              />
              Show unconnected ({visible.isolated.length})
            </label>
            <Button variant="outline" size="sm" onClick={() => void load()}>
              Refresh
            </Button>
          </div>

          <div className="grid gap-4 xl:grid-cols-[1fr_320px]">
            <GraphCanvas
              nodes={visible.nodes}
              edges={visible.edges}
              selectedId={selectedId}
              onSelect={setSelectedId}
            />
            <aside className="flex min-w-0 flex-col gap-4">
              <div className="rounded-lg border p-4">
                {selectedNode == null ? (
                  <p className="text-sm text-muted-foreground">
                    Select a node to inspect it — its neighborhood highlights, everything
                    else fades. Color shows urgency (green → red); dashed rings mark
                    bridges. Drag nodes to rearrange; scroll to zoom.
                  </p>
                ) : (
                  <div className="flex flex-col gap-2">
                    <Link
                      to={`/people/${selectedNode.personId}`}
                      className="text-sm font-semibold hover:underline"
                    >
                      {selectedNode.name ?? "Unnamed contact"}
                    </Link>
                    <div className="flex flex-wrap gap-1.5">
                      {selectedNode.isBridge && <Badge variant="secondary">Bridge</Badge>}
                      {selectedNode.isIsolated && <Badge variant="outline">Unconnected</Badge>}
                      <Badge variant="outline">{selectedNode.degree} connections</Badge>
                      <Badge variant="outline">
                        urgency {Math.round(selectedNode.urgencyScore)}
                      </Badge>
                    </div>
                    {selectedEdges.length > 0 && (
                      <>
                        <p className="mt-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                          Connected through ({selectedEdges.length})
                        </p>
                        <ul className="flex max-h-56 flex-col gap-1 overflow-auto">
                          {selectedEdges.slice(0, 20).map((e, i) => {
                            const other =
                              e.from === selectedNode.personId ? e.to : e.from;
                            return (
                              <li key={i} className="text-xs">
                                <Link
                                  to={`/people/${other}`}
                                  className="font-medium hover:underline"
                                >
                                  {neighborNames.get(other) ?? "?"}
                                </Link>
                                <span className="text-muted-foreground"> — {e.reason}</span>
                              </li>
                            );
                          })}
                        </ul>
                      </>
                    )}
                    <NavButton to={`/people/${selectedNode.personId}`} size="sm" variant="outline">
                      Open full profile →
                    </NavButton>
                  </div>
                )}
              </div>

              {!showIsolates && visible.isolated.length > 0 && (
                <div className="rounded-lg border p-4">
                  <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    Unconnected ({visible.isolated.length})
                  </p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Hidden from the canvas to keep the structure readable — nothing is
                    discarded. Enable “Show unconnected” to lay them out, or open one:
                  </p>
                  <ul className="mt-2 flex max-h-48 flex-col gap-1 overflow-auto">
                    {visible.isolated.slice(0, 30).map((n) => (
                      <li key={n.personId} className="text-xs">
                        <Link to={`/people/${n.personId}`} className="hover:underline">
                          {n.name ?? "Unnamed contact"}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </aside>
          </div>
        </>
      )}
    </div>
  );
}
