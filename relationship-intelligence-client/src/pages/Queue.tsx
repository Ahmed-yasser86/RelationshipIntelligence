import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { BandBadge, EmptyState, ErrorState, LoadingList, PersonAvatar, UrgencyBar } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { daysSince, timeAgo } from "@/lib/format";
import type { RelationshipHealth } from "@/lib/types";

function cadenceLine(item: RelationshipHealth): string {
  const silent = daysSince(item.lastContactAtUtc);
  const rhythm =
    item.cadenceReferenceDays != null
      ? `roughly every ${Math.round(item.cadenceReferenceDays)} days`
      : "no rhythm established yet";
  const quiet = silent == null ? "no contact recorded" : `quiet for ${silent}d`;
  return `Your rhythm: ${rhythm} — ${quiet}. Last contact ${timeAgo(item.lastContactAtUtc)}.`;
}

export function Queue() {
  const [items, setItems] = useState<RelationshipHealth[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      const queue = await api.get<RelationshipHealth[]>(
        "/api/Contacts/GetRelationshipQueue?top=7",
      );
      setItems(queue);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load the queue.");
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Needs attention</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Ranked by how far each relationship has drifted past its own rhythm —
            not by recency alone. Act on the top of the list first.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => void load()}>
          Refresh
        </Button>
      </div>

      {error && <ErrorState message={error} onRetry={() => void load()} />}
      {items === null && !error && <LoadingList rows={5} />}
      {items !== null && items.length === 0 && (
        <EmptyState
          title="Nothing needs attention"
          body="Every relationship is within its natural rhythm. Log interactions as they happen and this queue will surface drift early — usually weeks before you would notice on your own."
        />
      )}
      {items !== null && items.length > 0 && (
        <ol className="flex flex-col gap-3">
          {items.map((item, i) => (
            <li key={`${item.personId}-${i}`} className="rounded-lg border px-4 py-3">
              <div className="flex items-center gap-3">
                <span className="w-5 shrink-0 text-sm tabular-nums text-muted-foreground">
                  {i + 1}
                </span>
                <PersonAvatar name={item.name} />
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <Link
                      to={`/people/${item.personId}`}
                      className="truncate text-sm font-semibold hover:underline"
                    >
                      {item.name ?? "Unnamed contact"}
                    </Link>
                    <BandBadge band={item.band} />
                    {item.isBridge && <Badge variant="secondary">Bridge</Badge>}
                    {item.isImportant && <Badge variant="secondary">Key</Badge>}
                  </div>
                  <p className="mt-0.5 truncate text-xs text-muted-foreground">
                    {cadenceLine(item)}
                  </p>
                </div>
                <UrgencyBar value={item.urgencyScore} />
              </div>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}
