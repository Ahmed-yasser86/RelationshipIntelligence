import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { bandStyle, initials } from "@/lib/format";
import { cn } from "cn";
import { Link } from "react-router-dom";
import type { ReactNode } from "react";
import type { VariantProps } from "class-variance-authority";

type ButtonVariants = VariantProps<typeof buttonVariants>;

export function NavButton({
  to,
  variant,
  size,
  className,
  children,
}: {
  to: string;
  variant?: ButtonVariants["variant"];
  size?: ButtonVariants["size"];
  className?: string;
  children: ReactNode;
}) {
  return (
    <Link to={to} className={cn(buttonVariants({ variant, size }), className)}>
      {children}
    </Link>
  );
}

export function LoadingList({ rows = 5 }: { rows?: number }) {
  return (
    <div className="flex flex-col gap-3">
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} className="h-16 w-full" />
      ))}
    </div>
  );
}

export function EmptyState({
  title,
  body,
  action,
}: {
  title: string;
  body: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-start gap-2 rounded-lg border border-dashed px-6 py-10">
      <p className="text-sm font-semibold">{title}</p>
      <p className="max-w-md text-sm text-muted-foreground">{body}</p>
      {action}
    </div>
  );
}

export function ErrorState({
  message,
  onRetry,
}: {
  message: string;
  onRetry?: () => void;
}) {
  return (
    <div className="flex flex-col items-start gap-3 rounded-lg border border-destructive/30 bg-destructive/5 px-6 py-6">
      <p className="text-sm font-semibold">Something went wrong</p>
      <p className="max-w-xl text-sm text-muted-foreground">{message}</p>
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry}>
          Retry
        </Button>
      )}
    </div>
  );
}

export function BandBadge({ band }: { band: string }) {
  return (
    <Badge variant="outline" className={cn("font-medium", bandStyle(band))}>
      {band}
    </Badge>
  );
}

export function UrgencyBar({ value }: { value: number }) {
  const color =
    value > 85
      ? "bg-red-500"
      : value > 65
        ? "bg-amber-500"
        : value >= 40
          ? "bg-sky-500"
          : "bg-emerald-500";
  return (
    <div className="flex items-center gap-2">
      <div className="h-1.5 w-20 overflow-hidden rounded-full bg-secondary">
        <div className={cn("h-full rounded-full", color)} style={{ width: `${value}%` }} />
      </div>
      <span className="text-xs tabular-nums text-muted-foreground">
        {Math.round(value)}
      </span>
    </div>
  );
}

export function PersonAvatar({ name, className }: { name: string | null | undefined; className?: string }) {
  return (
    <div
      className={cn(
        "flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-secondary text-xs font-semibold text-secondary-foreground",
        className,
      )}
    >
      {initials(name)}
    </div>
  );
}
