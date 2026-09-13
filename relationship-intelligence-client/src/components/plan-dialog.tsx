import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { ApiError, api } from "@/lib/api";

interface PlanAction {
  action: string;
  personId: string | null;
  personName: string | null;
  whyNow: string;
  outcome: string;
}

interface PlanSuggestion {
  personId: string;
  intent: string;
  suggestedActions: PlanAction[];
  timingNotes: string;
  limitedContext: boolean;
}

export function PlanDialog({ personId, personName }: { personId: string; personName: string | null }) {
  const [open, setOpen] = useState(false);
  const [plan, setPlan] = useState<PlanSuggestion | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function load() {
    setError(null);
    setBusy(true);
    try {
      setPlan(
        await api.post<PlanSuggestion>("/api/Copilot/PostSuggestPlan", {
          PersonId: personId,
        }),
      );
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.status === 409
            ? "Configure an AI provider in the co-pilot setup first."
            : err.status === 503
              ? "The language model is unreachable right now."
              : err.body || err.message
          : "Could not build a plan.",
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        setOpen(o);
        if (o && !plan) void load();
      }}
    >
      <DialogTrigger render={<Button size="sm" variant="outline">Plan relationship</Button>} />
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Plan: {personName ?? "this relationship"}</DialogTitle>
        </DialogHeader>
        {busy && !plan && <p className="text-sm text-muted-foreground">Thinking…</p>}
        {error && <p className="text-sm text-destructive">{error}</p>}
        {plan && (
          <div className="flex flex-col gap-2">
            <p className="text-sm text-muted-foreground">
              Intent: {plan.intent}. Advisory only — you decide what to do.
              {plan.limitedContext ? " Context is thin, so treat this as a starting point." : ""}
            </p>
            <ul className="flex flex-col gap-2">
              {plan.suggestedActions.map((a, i) => (
                <li key={i} className="rounded-lg border px-3 py-2 text-sm">
                  <p className="font-medium">{a.action}</p>
                  <p className="mt-0.5 text-muted-foreground">Why now: {a.whyNow}</p>
                  <p className="text-muted-foreground">Outcome: {a.outcome}</p>
                </li>
              ))}
            </ul>
            {plan.timingNotes && <p className="text-xs text-muted-foreground">{plan.timingNotes}</p>}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
