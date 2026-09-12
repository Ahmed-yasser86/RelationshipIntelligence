import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Separator } from "@/components/ui/separator";
import { BandBadge } from "@/components/states";
import { daysSince, formatDate, timeAgo } from "@/lib/format";
import type { InteractionResponse, RelationshipHealth } from "@/lib/types";

function Row({ k, v }: { k: string; v: string }) {
  return (
    <div className="flex justify-between gap-4 py-1 text-sm">
      <dt className="shrink-0 text-muted-foreground">{k}</dt>
      <dd className="text-right tabular-nums">{v}</dd>
    </div>
  );
}

export function ExplainDrawer({
  health,
  interactions,
  open,
  onOpenChange,
}: {
  health: RelationshipHealth;
  interactions: InteractionResponse[];
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const events = [...(interactions ?? [])].sort(
    (a, b) => +new Date(a.timeOfInteraction) - +new Date(b.timeOfInteraction),
  );
  const n = events.length;
  const silent = daysSince(health.lastContactAtUtc);
  const quantile =
    health.silenceQuantile == null
      ? "not enough history"
      : `${Math.round(health.silenceQuantile * 100)}% of past gaps were shorter`;
  const estimated =
    health.evidenceStatus === "Insufficient" ||
    (health.cadenceReferenceDays != null && n < 3);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            Why {health.name ?? "this contact"} scores {Math.round(health.urgencyScore)}
            <BandBadge band={health.band} />
          </DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <section>
            <h3 className="mb-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              1 · Observed
            </h3>
            <dl>
              <Row k="Logged interactions" v={String(n)} />
              <Row
                k="Evidence grade"
                v={
                  health.evidenceStatus === "Established"
                    ? "Established — measured rhythm"
                    : "Early estimate — scored mostly on defaults"
                }
              />
              <Row
                k="First / last contact"
                v={
                  n > 0
                    ? `${formatDate(events[0].timeOfInteraction)} → ${formatDate(events[n - 1].timeOfInteraction)}`
                    : "—"
                }
              />
              <Row k="Current silence" v={silent == null ? "—" : `${silent} days`} />
            </dl>
            <p className="mt-1 text-xs text-muted-foreground">
              Only dated contact events are observed. Message content, sentiment, and
              the other side's behavior are not visible to the model.
            </p>
          </section>

          <Separator />

          <section>
            <h3 className="mb-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              2 · Derived
            </h3>
            <dl>
              <Row
                k="Typical rhythm"
                v={
                  health.cadenceReferenceDays != null
                    ? `~${Math.round(health.cadenceReferenceDays)} days between contacts${estimated ? " (estimated default, not measured)" : ""}`
                    : "cannot be inferred yet — no repeated contact to measure a rhythm from"
                }
              />
              <Row k="Silence vs own history" v={quantile} />
              <Row k="Tie strength" v={health.tieStrength.toFixed(2)} />
            </dl>
            <p className="mt-1 text-xs text-muted-foreground">
              Strength decays exponentially between events (60-day half-life) and rises
              equally with each one. No per-channel weights, no hidden adjustments.
              {n < 3 &&
                " With fewer than 3 past gaps this is an early estimate — it sharpens as history accumulates."}
            </p>
          </section>

          <Separator />

          <section>
            <h3 className="mb-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              3 · Result
            </h3>
            <dl>
              <Row k="Urgency" v={`${Math.round(health.urgencyScore)} / 100`} />
              <Row k="Band" v={health.band} />
              <Row
                k="Last contact"
                v={`${formatDate(health.lastContactAtUtc)} (${timeAgo(health.lastContactAtUtc)})`}
              />
            </dl>
            <p className="mt-1 text-xs text-muted-foreground">
              Urgency is the deficit against your strongest tie — a ranking aid, not a
              probability. Flags (bridge, key) describe structure, not risk magnitude.
            </p>
          </section>
        </div>
      </DialogContent>
    </Dialog>
  );
}
