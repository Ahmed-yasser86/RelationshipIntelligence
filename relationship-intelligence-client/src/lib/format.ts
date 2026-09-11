export function timeAgo(iso: string | null | undefined): string {
  if (!iso) return "never";
  const diffMs = Date.now() - new Date(iso).getTime();
  const days = Math.floor(diffMs / 86_400_000);
  if (days < 0) return "today";
  if (days === 0) return "today";
  if (days === 1) return "yesterday";
  if (days < 30) return `${days}d ago`;
  const months = Math.floor(days / 30);
  if (months < 12) return `${months}mo ago`;
  return `${Math.floor(months / 12)}y ago`;
}

export function formatDate(iso: string | null | undefined): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function daysSince(iso: string | null | undefined): number | null {
  if (!iso) return null;
  return Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000));
}

const BAND_STYLES: Record<string, string> = {
  Healthy: "bg-emerald-100 text-emerald-800 border-emerald-200",
  Drifting: "bg-sky-100 text-sky-800 border-sky-200",
  AtRisk: "bg-amber-100 text-amber-800 border-amber-200",
  Critical: "bg-red-100 text-red-800 border-red-200",
};

export function bandStyle(band: string): string {
  return BAND_STYLES[band] ?? "bg-muted text-muted-foreground border-border";
}

export function initials(name: string): string {
  return name
    .split(/\s+/)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? "")
    .join("");
}
