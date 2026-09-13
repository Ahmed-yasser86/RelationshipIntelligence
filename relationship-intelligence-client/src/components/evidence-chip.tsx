import { cn } from "cn";

export type EvidenceKind =
  | "user"
  | "observed"
  | "derived"
  | "suggested"
  | "meeting"
  | "unknown";

const STYLES: Record<EvidenceKind, { dot: string; chip: string; label: string; hint: string }> = {
  user: {
    dot: "bg-foreground",
    chip: "border-foreground/30 bg-background text-foreground",
    label: "Yours",
    hint: "Written or confirmed by you — canonical truth",
  },
  observed: {
    dot: "bg-sky-600",
    chip: "border-sky-300 bg-sky-50 text-sky-900",
    label: "Observed",
    hint: "Dated events you logged — the only thing the model measures",
  },
  derived: {
    dot: "bg-violet-600",
    chip: "border-violet-300 bg-violet-50 text-violet-900",
    label: "Derived",
    hint: "Calculated from observations by the model — checkable, not a fact about the world",
  },
  suggested: {
    dot: "bg-amber-500",
    chip: "border-dashed border-amber-400 bg-amber-50 text-amber-900",
    label: "Suggested",
    hint: "Proposed by the assistant — needs your review before it counts",
  },
  meeting: {
    dot: "bg-teal-600",
    chip: "border-teal-300 bg-teal-50 text-teal-900",
    label: "From meeting",
    hint: "Derived from a meeting you confirmed",
  },
  unknown: {
    dot: "bg-muted-foreground/40",
    chip: "border-border text-muted-foreground",
    label: "Not recorded",
    hint: "The system does not know this — anything here would be invented",
  },
};

export function EvidenceChip({
  kind,
  label,
  title,
  className,
}: {
  kind: EvidenceKind;
  label?: string;
  title?: string;
  className?: string;
}) {
  const s = STYLES[kind];
  return (
    <span
      title={title ?? s.hint}
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[11px] font-medium",
        s.chip,
        className,
      )}
    >
      <span aria-hidden className={cn("h-1.5 w-1.5 rounded-full", s.dot)} />
      {label ?? s.label}
    </span>
  );
}

export function ProvenanceDot({ kind, className }: { kind: EvidenceKind; className?: string }) {
  return (
    <span title={STYLES[kind].hint} className={cn("inline-block h-1.5 w-1.5 rounded-full", STYLES[kind].dot, className)} aria-hidden />
  );
}
