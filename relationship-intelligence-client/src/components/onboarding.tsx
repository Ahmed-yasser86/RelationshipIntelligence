import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { ApiError, api } from "@/lib/api";

const STEPS = [
  {
    title: "Interactions are the raw material",
    body: "Everything here starts from one thing: dated contact events — a call, an email, a meeting. Log them as they happen, import history from CSV, and the system does the rest. No events, no intelligence.",
  },
  {
    title: "Cadence becomes a baseline",
    body: "For each relationship the system learns your natural rhythm — roughly every 14 days with Layla, every quarter with an old mentor — and watches how far the current silence has drifted past it.",
  },
  {
    title: "Decay becomes a queue",
    body: "An exponential decay model turns drift into a ranked list: who needs you this week, with the evidence attached. Every score can be explained — open any item and ask why.",
  },
  {
    title: "Your network has a shape",
    body: "Shared organizations, tags, and channels reveal clusters and bridges — the people holding your worlds together. They are flagged because research shows bridges are lost first. Not sure where to start? Load the demo workspace below and watch the loop work on realistic data.",
  },
];

export function Onboarding() {
  const [open, setOpen] = useState(
    () => localStorage.getItem("ri.onboarded") !== "1",
  );
  const [step, setStep] = useState(0);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  function dismiss() {
    localStorage.setItem("ri.onboarded", "1");
    setOpen(false);
  }

  async function seedDemo() {
    setBusy(true);
    setMessage(null);
    try {
      const count = await api.post<number>("/api/Contacts/PostSeedDemoWorkspace");
      setMessage(`Demo workspace ready — ${count} contacts with real histories. Explore, then remove it any time from this tour.`);
    } catch (err) {
      setMessage(err instanceof ApiError ? err.body || err.message : "Seeding failed.");
    } finally {
      setBusy(false);
    }
  }

  async function clearDemo() {
    setBusy(true);
    setMessage(null);
    try {
      const removed = await api.post<number>("/api/Contacts/PostClearDemoWorkspace");
      setMessage(`Removed ${removed} demo contacts. Your own data was untouched.`);
    } catch (err) {
      setMessage(err instanceof ApiError ? err.body || err.message : "Cleanup failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o) => !o && dismiss()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            How this works ({step + 1} of {STEPS.length})
          </DialogTitle>
        </DialogHeader>
        <div>
          <p className="text-sm font-semibold">{STEPS[step].title}</p>
          <p className="mt-1 text-sm text-muted-foreground">{STEPS[step].body}</p>
        </div>
        {step === STEPS.length - 1 && (
          <div className="flex flex-wrap items-center gap-2 rounded-md border p-3">
            <Button size="sm" variant="outline" disabled={busy} onClick={() => void seedDemo()}>
              {busy ? "Working…" : "Load demo workspace"}
            </Button>
            <Button size="sm" variant="ghost" disabled={busy} onClick={() => void clearDemo()}>
              Remove demo data
            </Button>
            {message && <p className="w-full text-xs text-muted-foreground">{message}</p>}
          </div>
        )}
        <DialogFooter>
          {step > 0 && (
            <Button variant="ghost" size="sm" onClick={() => setStep(step - 1)}>
              Back
            </Button>
          )}
          {step < STEPS.length - 1 ? (
            <Button size="sm" onClick={() => setStep(step + 1)}>
              Next
            </Button>
          ) : (
            <Button size="sm" onClick={dismiss}>
              Start exploring
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
