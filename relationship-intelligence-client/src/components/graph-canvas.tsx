import { useEffect, useMemo, useRef } from "react";
import * as d3 from "d3";
import type { NetworkEdge, NetworkNode } from "@/lib/types";

export interface GraphSelection {
  id: string | null;
  neighbors: Set<string>;
}

interface SimNode extends d3.SimulationNodeDatum {
  id: string;
  node: NetworkNode;
}

interface SimLink extends d3.SimulationLinkDatum<SimNode> {
  reason: string;
  weight: number;
}

function urgencyColor(u: number, scored: boolean): string {
  if (!scored) return "#94a3b8";
  if (u > 85) return "#dc2626";
  if (u > 65) return "#d97706";
  if (u >= 40) return "#0284c7";
  return "#059669";
}

export function GraphCanvas({
  nodes,
  edges,
  selectedId,
  onSelect,
}: {
  nodes: NetworkNode[];
  edges: NetworkEdge[];
  selectedId: string | null;
  onSelect: (id: string | null) => void;
}) {
  const svgRef = useRef<SVGSVGElement>(null);
  const positions = useRef(new Map<string, { x: number; y: number }>());
  const zoomRef = useRef<d3.ZoomBehavior<SVGSVGElement, unknown> | null>(null);

  const neighbors = useMemo(() => {
    const map = new Map<string, Set<string>>();
    for (const n of nodes) map.set(n.personId, new Set());
    for (const e of edges) {
      map.get(e.from)?.add(e.to);
      map.get(e.to)?.add(e.from);
    }
    return map;
  }, [nodes, edges]);

  useEffect(() => {
    const svgEl = svgRef.current;
    if (!svgEl) return;
    const svg = d3.select(svgEl);
    svg.selectAll("*").remove();

    const width = Math.max(600, svgEl.clientWidth || 800);
    const height = 560;
    svgEl.setAttribute("viewBox", `0 0 ${width} ${height}`);

    const g = svg.append("g");

    const simNodes: SimNode[] = nodes.map((n) => {
      const saved = positions.current.get(n.personId);
      return {
        id: n.personId,
        node: n,
        x: saved?.x ?? width / 2 + (Math.random() - 0.5) * 200,
        y: saved?.y ?? height / 2 + (Math.random() - 0.5) * 200,
      };
    });
    const byId = new Map(simNodes.map((n) => [n.id, n]));
    const simLinks: SimLink[] = edges.flatMap((e) => {
      const source = byId.get(e.from);
      const target = byId.get(e.to);
      return source && target ? [{ source, target, reason: e.reason, weight: 1 }] : [];
    });

    const selectedNeighbors = selectedId
      ? (neighbors.get(selectedId) ?? new Set<string>())
      : null;

    const link = g
      .append("g")
      .selectAll<SVGLineElement, SimLink>("line")
      .data(simLinks)
      .join("line")
      .attr("stroke", (d) => {
        if (!selectedId) return "#94a3b8";
        const s = (d.source as SimNode).id;
        const t = (d.target as SimNode).id;
        return s === selectedId || t === selectedId ? "#475569" : "#e2e8f0";
      })
      .attr("stroke-width", (d) =>
        selectedId &&
        (d.source as SimNode).id !== selectedId &&
        (d.target as SimNode).id !== selectedId
          ? 1
          : 1 + d.weight,
      )
      .append("title")
      .text((d) => (d as unknown as SimLink).reason);

    const node = g
      .append("g")
      .selectAll<SVGGElement, SimNode>("g")
      .data(simNodes)
      .join("g")
      .style("cursor", "pointer")
      .style("opacity", (d) => {
        if (!selectedId) return 1;
        return d.id === selectedId || (selectedNeighbors?.has(d.id) ?? false) ? 1 : 0.25;
      })
      .on("click", (_event, d) => {
        onSelect(selectedId === d.id ? null : d.id);
      });

    node
      .append("circle")
      .attr("r", (d) => 5 + Math.min(8, d.node.degree * 1.5))
      .attr("fill", (d) => urgencyColor(d.node.urgencyScore, d.node.evidenceStatus !== "NoHistory"))
      .attr("stroke", (d) =>
        d.id === selectedId ? "#0f172a" : d.node.isBridge ? "#7c3aed" : "#ffffff",
      )
      .attr("stroke-width", (d) => (d.id === selectedId ? 3 : d.node.isBridge ? 2 : 1.5))
      .attr("stroke-dasharray", (d) =>
        d.id !== selectedId && d.node.isBridge ? "3 2" : null,
      )
      .append("title")
      .text((d) => `${d.node.name ?? "Unnamed"} — urgency ${Math.round(d.node.urgencyScore)}`);

    node
      .filter(
        (d) =>
          d.id === selectedId ||
          d.node.isBridge ||
          (selectedId != null && (selectedNeighbors?.has(d.id) ?? false)),
      )
      .append("text")
      .attr("x", 11)
      .attr("y", 4)
      .attr("font-size", 11)
      .attr("fill", "currentColor")
      .text((d) => d.node.name ?? "?");

    const simulation = d3
      .forceSimulation(simNodes)
      .force(
        "link",
        d3
          .forceLink<SimNode, SimLink>(simLinks)
          .id((d) => d.id)
          .distance((d) => 70 - Math.min(30, d.weight * 15))
          .strength(0.6),
      )
      .force("charge", d3.forceManyBody().strength(-160))
      .force("center", d3.forceCenter(width / 2, height / 2))
      .force("collide", d3.forceCollide().radius((d) => 12 + Math.min(8, (d as SimNode).node.degree)))
      .on("tick", () => {
        link
          .attr("x1", (d) => (d.source as SimNode).x ?? 0)
          .attr("y1", (d) => (d.source as SimNode).y ?? 0)
          .attr("x2", (d) => (d.target as SimNode).x ?? 0)
          .attr("y2", (d) => (d.target as SimNode).y ?? 0);
        node.attr("transform", (d) => `translate(${d.x ?? 0},${d.y ?? 0})`);
        for (const n of simNodes) {
          if (n.x != null && n.y != null) positions.current.set(n.id, { x: n.x, y: n.y });
        }
      });

    const drag = d3
      .drag<SVGGElement, SimNode>()
      .on("start", (event, d) => {
        if (!event.active) simulation.alphaTarget(0.3).restart();
        d.fx = d.x;
        d.fy = d.y;
      })
      .on("drag", (event, d) => {
        d.fx = event.x;
        d.fy = event.y;
      })
      .on("end", (event, d) => {
        if (!event.active) simulation.alphaTarget(0);
        d.fx = null;
        d.fy = null;
      });
    node.call(drag);

    const zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.2, 4])
      .on("zoom", (event) => {
        g.attr("transform", event.transform.toString());
      });
    zoomRef.current = zoom;
    svg.call(zoom);
    svg.on("dblclick.zoom", null);
    svg.on("click", (event) => {
      if (event.target === svgEl) onSelect(null);
    });

    return () => {
      simulation.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [nodes, edges, selectedId]);

  return (
    <div className="relative">
      <svg
        ref={svgRef}
        className="block h-[560px] w-full rounded-lg border bg-card text-foreground"
        role="img"
        aria-label="Contact network graph"
      />
      <div className="absolute right-2 top-2 flex gap-1">
        <button
          type="button"
          aria-label="Zoom in"
          className="rounded-md border bg-background px-2 py-1 text-sm"
          onClick={() => {
            if (svgRef.current && zoomRef.current) {
              d3.select(svgRef.current)
                .transition()
                .duration(200)
                .call(zoomRef.current.scaleBy as never, 1.3);
            }
          }}
        >
          +
        </button>
        <button
          type="button"
          aria-label="Zoom out"
          className="rounded-md border bg-background px-2 py-1 text-sm"
          onClick={() => {
            if (svgRef.current && zoomRef.current) {
              d3.select(svgRef.current)
                .transition()
                .duration(200)
                .call(zoomRef.current.scaleBy as never, 0.77);
            }
          }}
        >
          −
        </button>
        <button
          type="button"
          aria-label="Reset view"
          className="rounded-md border bg-background px-2 py-1 text-sm"
          onClick={() => {
            if (svgRef.current && zoomRef.current) {
              d3.select(svgRef.current)
                .transition()
                .duration(200)
                .call(
                  zoomRef.current.transform as never,
                  d3.zoomIdentity as never,
                );
            }
          }}
        >
          Reset
        </button>
      </div>
    </div>
  );
}
