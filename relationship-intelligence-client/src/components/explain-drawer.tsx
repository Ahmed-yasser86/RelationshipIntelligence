import { Badge } from "@/components/ui/badge";
import { EvidenceChip } from "@/components/evidence-chip";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Separator } from "@/components/ui/separator";
import { BandBadge } from "@/components/states";
import { daysSince, formatDate, timeAgo } from "@/lib/format";
import { InteractionTypes } from "@/lib/types";
import type { InteractionResponse, RelationshipHealth } from "@/lib/types";

const BAND_HEX: Record<string, string> = {
  Healthy: "#059669",
  Drifting: "#0284c7",
  AtRisk: "#d97706",
  Critical: "#dc2626",
};

function bandHex(band: string): string {
  return BAND_HEX[band] ?? "#64748b";
}

function Row({ k, v }: { k: string; v: string }) {
  return (
    <div className="flex justify-between gap-4 py-1 text-sm">
      <dt className="shrink-0 text-muted-foreground">{k}</dt>
      <dd className="text-right tabular-nums">{v}</dd>
    </div>
  );
}

function Step({ n, title, hint, kind }: { n: string; title: string; hint: string; kind: "observed" | "derived" }) {
  return (
    <div className="mb-2 flex items-center gap-2">
      <span className="flex h-5 w-5 items-center justify-center rounded-full bg-primary text-[11px] font-bold text-primary-foreground">
        {n}
      </span>
      <h3 className="text-xs font-semibold uppercase tracking-wide">{title}</h3>
      <span className="text-xs text-muted-foreground">· {hint}</span>
      <EvidenceChip kind={kind} className="ml-auto" />
    </div>
  );
}

function ScoreDial({ value, band }: { value: number; band: string }) {
  const r = 40;
  const c = 2 * Math.PI * r;
  const frac = Math.min(100, Math.max(0, value)) / 100;
  return (
    <div className="relative h-24 w-24 shrink-0" role="img" aria-label={`Urgency ${Math.round(value)} of 100, ${band}`}>
      <svg viewBox="0 0 96 96" className="h-full w-full -rotate-90">
        <circle cx="48" cy="48" r={r} fill="none" strokeWidth="10" className="stroke-secondary" />
        <circle
          cx="48"
          cy="48"
          r={r}
          fill="none"
          stroke={bandHex(band)}
          strokeWidth="10"
          strokeLinecap="round"
          strokeDasharray={`${frac * c} ${c}`}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-2xl font-bold tabular-nums">{Math.round(value)}</span>
        <span className="text-[10px] text-muted-foreground">/ 100</span>
      </div>
    </div>
  );
}

function ThresholdBar({ value }: { value: number }) {
  const cuts = [40, 65, 85, 100];
  const segments = [
    { label: "Healthy", cls: "bg-emerald-500" },
    { label: "Drifting", cls: "bg-sky-500" },
    { label: "AtRisk", cls: "bg-amber-500" },
    { label: "Critical", cls: "bg-red-500" },
  ];
  return (
    <div>
      <div className="relative flex h-2.5 w-full overflow-visible rounded-full">
        {segments.map((s, i) => (
          <div
            key={s.label}
            className={`${s.cls} h-full ${i === 0 ? "rounded-l-full" : ""} ${i === segments.length - 1 ? "rounded-r-full" : ""}`}
            style={{ width: `${cuts[i] - (i === 0 ? 0 : cuts[i - 1])}%` }}
            title={s.label}
          />
        ))}
        <div
          className="absolute top-1/2 h-4 w-0.5 -translate-x-1/2 -translate-y-1/2 rounded bg-foreground"
          style={{ left: `${Math.min(100, Math.max(0, value))}%` }}
          title={`Score ${Math.round(value)}`}
        />
      </div>
      <div className="mt-1 flex justify-between text-[10px] text-muted-foreground tabular-nums">
        <span>0</span>
        <span>40</span>
        <span>65</span>
        <span>85</span>
        <span>100</span>
      </div>
    </div>
  );
}

function RhythmBar({
  silence,
  rhythm,
  band,
}: {
  silence: number | null;
  rhythm: number | null;
  band: string;
}) {
  if (silence == null) {
    return <p className="text-sm text-muted-foreground">No contact recorded yet.</p>;
  }
  if (rhythm == null) {
    return (
      <div>
        <div className="h-2.5 w-full overflow-hidden rounded-full bg-secondary">
          <div className="h-full rounded-full" style={{ width: "100%", backgroundColor: bandHex(band) }} />
        </div>
        <p className="mt-1 text-xs text-muted-foreground">
          Quiet for {silence}d — no rhythm measured yet, so there is nothing to compare against.
        </p>
      </div>
    );
  }
  const scale = Math.max(silence, rhythm * 1.2, 1);
  const over = silence > rhythm;
  return (
    <div>
      <div className="relative h-2.5 w-full rounded-full bg-secondary">
        <div
          className="h-full rounded-full"
          style={{ width: `${(silence / scale) * 100}%`, backgroundColor: bandHex(band) }}
        />
        <div
          className="absolute top-1/2 h-4 w-0.5 -translate-x-1/2 -translate-y-1/2 rounded bg-foreground"
          style={{ left: `${(rhythm / scale) * 100}%` }}
          title={`Typical rhythm: ~${Math.round(rhythm)} days`}
        />
      </div>
      <div className="mt-1 flex justify-between text-xs tabular-nums">
        <span className="font-medium" style={{ color: bandHex(band) }}>
          {silence}d quiet
        </span>
        <span className="text-muted-foreground">rhythm ~{Math.round(rhythm)}d</span>
      </div>
      <p className="mt-1 text-xs text-muted-foreground">
        {over
          ? `Silence has run ${(silence / rhythm).toFixed(1)}× past the usual rhythm — that overshoot is what raises urgency.`
          : "Still inside the usual rhythm — silence alone is not raising urgency yet."}
      </p>
    </div>
  );
}

function QuantileBar({ quantile }: { quantile: number | null }) {
  if (quantile == null) {
    return <p className="text-sm text-muted-foreground">Not enough history to compare gaps.</p>;
  }
  const pct = Math.round(quantile * 100);
  return (
    <div>
      <div className="relative h-2 w-full rounded-full bg-secondary">
        <div className="h-full rounded-full bg-foreground/70" style={{ width: `${pct}%` }} />
        <div
          className="absolute top-1/2 h-3.5 w-0.5 -translate-x-1/2 -translate-y-1/2 rounded bg-foreground"
          style={{ left: `${pct}%` }}
        />
      </div>
      <p className="mt-1 text-xs text-muted-foreground">
        Longer than {pct}% of your past gaps with this contact.
      </p>
    </div>
  );
}

function EventTimeline({ events }: { events: InteractionResponse[] }) {
  if (events.length === 0) {
    return (
      <div className="rounded-md border border-dashed px-3 py-4 text-center text-xs text-muted-foreground">
        No logged contact yet — the first logged interaction starts the history.
      </div>
    );
  }
  const times = events.map((e) => +new Date(e.timeOfInteraction));
  const first = Math.min(...times);
  const now = Date.now();
  const span = Math.max(now - first, 86_400_000);
  return (
    <div>
      <div className="relative h-8 w-full">
        <div className="absolute left-0 right-0 top-1/2 h-0.5 -translate-y-1/2 rounded bg-secondary" />
        {events.map((e, i) => {
          const left = ((+new Date(e.timeOfInteraction) - first) / span) * 100;
          const last = i === events.length - 1;
          return (
            <div
              key={`${e.interactionId ?? i}`}
              className={`absolute top-1/2 h-2.5 w-2.5 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-background ${
                last ? "h-3.5 w-3.5 ring-2 ring-offset-1" : ""
              }`}
              style={{
                left: `${Math.min(100, Math.max(0, left))}%`,
                backgroundColor: last ? bandHex("AtRisk") : "var(--primary)",
                ["--tw-ring-color" as string]: last ? bandHex("AtRisk") : undefined,
              }}
              title={`${InteractionTypes[e.interactionType] ?? "Contact"} · ${formatDate(e.timeOfInteraction)}${e.interactionTitle ? ` — ${e.interactionTitle}` : ""}`}
            />
          );
        })}
        <div
          className="absolute top-1/2 h-3 w-0.5 -translate-y-1/2 rounded bg-foreground"
          style={{ left: "100%" }}
          title="Today"
        />
      </div>
      <div className="flex justify-between text-[11px] text-muted-foreground tabular-nums">
        <span>{formatDate(events[0].timeOfInteraction)}</span>
        <span>
          {events.length} event{events.length === 1 ? "" : "s"} · today
        </span>
      </div>
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
  const estimated =
    health.evidenceStatus === "Insufficient" ||
    (health.cadenceReferenceDays != null && n < 3);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Why {health.name ?? "this contact"} scores here</DialogTitle>
        </DialogHeader>

        <div className="flex items-center gap-4 rounded-lg border p-3">
          <ScoreDial value={health.urgencyScore} band={health.band} />
          <div className="flex min-w-0 flex-col items-start gap-1.5">
            <BandBadge band={health.band} />
            {health.evidenceStatus === "Established" ? (
              <Badge
                variant="outline"
                className="border-emerald-300 bg-emerald-50 text-emerald-800"
                title={`Scored from ${n} logged interaction(s) — a measured rhythm`}
              >
                Measured rhythm
              </Badge>
            ) : (
              <Badge
                variant="outline"
                className="border-amber-300 bg-amber-50 text-amber-800"
                title={`Scored from ${n} logged interaction(s) — an early estimate, not a measured rhythm`}
              >
                Early estimate
              </Badge>
            )}
            <p className="text-xs text-muted-foreground">
              Last contact {formatDate(health.lastContactAtUtc)} ({timeAgo(health.lastContactAtUtc)})
            </p>
          </div>
        </div>

        <div className="flex flex-col gap-4">
          <section>
            <Step n="1" title="Observed" hint="what you logged" kind="observed" />
            <EventTimeline events={events} />
            <dl className="mt-2">
              <Row k="Logged interactions" v={String(n)} />
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
            <Step n="2" title="Derived" hint="what the model infers" kind="derived" />
            <div className="flex flex-col gap-3">
              <div>
                <p className="mb-1 text-xs font-medium">
                  Silence against typical rhythm
                  {health.cadenceReferenceDays == null
                    ? ""
                    : estimated
                      ? " (rhythm estimated, not measured)"
                      : ""}
                </p>
                <RhythmBar silence={silent} rhythm={health.cadenceReferenceDays} band={health.band} />
              </div>
              <div>
                <p className="mb-1 text-xs font-medium">Silence against own history</p>
                <QuantileBar quantile={health.silenceQuantile} />
              </div>
              <dl>
                <Row k="Tie strength" v={health.tieStrength.toFixed(2)} />
              </dl>
            </div>
            <p className="mt-1 text-xs text-muted-foreground">
              Strength decays exponentially between events (60-day half-life) and rises
              equally with each one. No per-channel weights, no hidden adjustments.
              {n < 3 &&
                " With fewer than 3 past gaps this is an early estimate — it sharpens as history accumulates."}
            </p>
          </section>

          <Separator />

          <section>
            <Step n="3" title="Result" hint="where it lands" kind="derived" />
            <ThresholdBar value={health.urgencyScore} />
            <dl className="mt-2">
              <Row k="Band" v={health.band} />
            </dl>
            <p className="mt-1 text-xs text-muted-foreground">
              Urgency is the deficit against your strongest tie — a ranking aid, not a
              probability. Flags (articulation point, key) describe structure, not risk magnitude.
            </p>
          </section>
        </div>
      </DialogContent>
    </Dialog>
  );
}
