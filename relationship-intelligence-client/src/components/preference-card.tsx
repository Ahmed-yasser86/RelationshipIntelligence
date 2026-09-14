import { useEffect, useState } from "react";
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
import { ApiError, api } from "@/lib/api";
import { CadencePresets } from "@/lib/types";
import type { RelationshipPreference, ReminderDue } from "@/lib/types";

/**
 * User-controlled relationship parameters in human terms. Saves intention
 * (cadence, importance, priority, suggestions, reminders); the deterministic
 * model stays untouched.
 */
export function PreferenceCard({ personId, personName }: { personId: string; personName: string }) {
  const [pref, setPref] = useState<RelationshipPreference | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [cadence, setCadence] = useState<string>("");
  const [customCadence, setCustomCadence] = useState("");
  const [importance, setImportance] = useState(false);
  const [priority, setPriority] = useState(false);
  const [intentional, setIntentional] = useState(false);
  const [excluded, setExcluded] = useState(false);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const p = await api.get<RelationshipPreference>(`/api/Preference/GetPreference?personId=${personId}`);
        if (cancelled) return;
        setPref(p);
        if (p.desiredCadenceDays != null) {
          const preset = CadencePresets.find((c) => c.days === p.desiredCadenceDays);
          setCadence(preset ? String(preset.days) : "custom");
          if (!preset) setCustomCadence(String(p.desiredCadenceDays));
        }
        setImportance(p.importance === 1);
        setPriority(p.priority === 1);
        setIntentional(p.keepInTouchIntentionally);
        setExcluded(p.excludeFromSuggestions);
      } catch (err) {
        if (!cancelled && !(err instanceof ApiError && err.status === 404)) {
          setError(err instanceof ApiError ? err.body || err.message : "Could not load preferences.");
        }
      } finally {
        if (!cancelled) setLoaded(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [personId]);

  async function save() {
    setError(null);
    setMessage(null);
    setBusy(true);
    try {
      const days = cadence === "" ? null : cadence === "custom" ? Number(customCadence) : Number(cadence);
      if (days != null && (!Number.isFinite(days) || days < 1 || days > 365)) {
        throw new ApiError(400, "Cadence must be between 1 and 365 days.");
      }
      const saved = await api.put<RelationshipPreference>("/api/Preference/PutPreference", {
        PersonId: personId,
        DesiredCadenceDays: days,
        Importance: importance ? 1 : 0,
        Priority: priority ? 1 : 0,
        KeepInTouchIntentionally: intentional,
        ExcludeFromSuggestions: excluded,
      });
      setPref(saved);
      setMessage("Saved. These shape suggestions and attention — never scores.");
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save.");
    } finally {
      setBusy(false);
    }
  }

  if (!loaded) return <p className="text-sm text-muted-foreground">Loading preferences…</p>;

  return (
    <section aria-label="Relationship preferences" className="rounded-lg border px-4 py-3">
      <h3 className="font-display text-lg font-semibold">How to stay in touch</h3>
      <p className="mt-0.5 text-xs text-muted-foreground">
        Your intent for {personName} — how often, how important, whether to keep in touch on purpose.
      </p>
      <div className="mt-3 flex flex-col gap-2.5">
        <div className="flex flex-col gap-1">
          <Label htmlFor={`cad-${personId}`}>How often do you want to stay in touch?</Label>
          <div className="flex gap-2">
            <Select value={cadence} onValueChange={(v) => setCadence(v ?? "")}>
              <SelectTrigger id={`cad-${personId}`} className="flex-1">
                <SelectValue placeholder="No specific rhythm" />
              </SelectTrigger>
              <SelectContent>
                {CadencePresets.map((c) => (
                  <SelectItem key={c.days} value={String(c.days)}>
                    {c.label}
                  </SelectItem>
                ))}
                <SelectItem value="custom">Custom…</SelectItem>
              </SelectContent>
            </Select>
            {cadence === "custom" && (
              <Input
                type="number"
                min={1}
                max={365}
                className="w-24"
                aria-label="Custom cadence in days"
                value={customCadence}
                onChange={(e) => setCustomCadence(e.target.value)}
              />
            )}
          </div>
        </div>
        {[
          ["Important relationship", importance, setImportance],
          ["High priority", priority, setPriority],
          ["Keep in touch intentionally (special contact)", intentional, setIntentional],
          ["Exclude from proactive suggestions", excluded, setExcluded],
        ].map(([label, value, set]) => (
          <label key={label as string} className="flex cursor-pointer items-center gap-2 text-sm">
            <input
              type="checkbox"
              className="h-4 w-4"
              checked={value as boolean}
              onChange={(e) => (set as (v: boolean) => void)(e.target.checked)}
            />
            {label as string}
          </label>
        ))}
        {error && <p className="text-sm text-destructive">{error}</p>}
        {message && <p className="text-sm text-emerald-700">{message}</p>}
        <div>
          <Button size="sm" disabled={busy} onClick={() => void save()}>
            {busy ? "Saving…" : pref ? "Save preferences" : "Set preferences"}
          </Button>
        </div>
      </div>
    </section>
  );
}

export function ReminderCard({ personId, personName }: { personId: string; personName: string }) {
  const [pref, setPref] = useState<RelationshipPreference | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [interval, setInterval] = useState("10");
  const [customInterval, setCustomInterval] = useState("");
  const [strict, setStrict] = useState(false);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function reload() {
    try {
      const p = await api.get<RelationshipPreference>(`/api/Preference/GetPreference?personId=${personId}`);
      setPref(p);
      if (p.reminderIntervalDays != null) {
        const preset = CadencePresets.find((c) => c.days === p.reminderIntervalDays);
        setInterval(preset ? String(preset.days) : "custom");
        if (!preset) setCustomInterval(String(p.reminderIntervalDays));
      }
      setStrict(p.reminderStrict);
    } catch (err) {
      if (!(err instanceof ApiError && err.status === 404)) {
        setError(err instanceof ApiError ? err.body || err.message : "Could not load reminder.");
      }
    } finally {
      setLoaded(true);
    }
  }

  useEffect(() => {
    void reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [personId]);

  async function run(fn: () => Promise<RelationshipPreference>, ok: string) {
    setError(null);
    setMessage(null);
    setBusy(true);
    try {
      const saved = await fn();
      setPref(saved);
      if (saved.reminderIntervalDays != null) {
        const preset = CadencePresets.find((c) => c.days === saved.reminderIntervalDays);
        setInterval(preset ? String(preset.days) : "custom");
      }
      setMessage(ok);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Operation failed.");
    } finally {
      setBusy(false);
    }
  }

  function intervalDays(): number | null {
    const v = interval === "custom" ? Number(customInterval) : Number(interval);
    if (!Number.isFinite(v) || v < 1 || v > 365) return null;
    return Math.round(v);
  }

  if (!loaded) return <p className="text-sm text-muted-foreground">Loading reminder…</p>;

  return (
    <section aria-label="Reminder" className="rounded-lg border px-4 py-3">
      <h3 className="font-display text-lg font-semibold">Reminder</h3>
      <p className="mt-0.5 text-xs text-muted-foreground">
        A reminder is your own intention — it never means {personName} is urgent, and snoozing it never logs contact.
      </p>
      {pref?.reminderEnabled ? (
        <div className="mt-2.5 text-sm">
          <p>
            <strong>Reminder: every {pref.reminderIntervalDays} days.</strong>{" "}
            <span className="text-muted-foreground">You configured this reminder.</span>
            {pref.snoozedUntilUtc && (
              <span className="text-muted-foreground">
                {" "}Snoozed until {new Date(pref.snoozedUntilUtc).toLocaleDateString()}.
              </span>
            )}
          </p>
          <div className="mt-2 flex flex-wrap gap-1.5">
            <Button size="sm" variant="outline" disabled={busy} onClick={() => void run(() => api.post<RelationshipPreference>(`/api/Preference/PostSnooze?personId=${personId}&days=7`, {}), "Snoozed for 7 days.")}>
              Snooze 7d
            </Button>
            <Button size="sm" variant="outline" disabled={busy} onClick={() => void run(() => api.post<RelationshipPreference>(`/api/Preference/PostSkip?personId=${personId}`, {}), "Skipped this round.")}>
              Skip
            </Button>
            <Button size="sm" variant="outline" disabled={busy} onClick={() => void run(() => api.post<RelationshipPreference>(`/api/Preference/PostComplete?personId=${personId}`, {}), "Marked done — log a real interaction separately if you connected.")}>
              Done
            </Button>
            <Button size="sm" variant="ghost" disabled={busy} onClick={() => void run(() => api.del<RelationshipPreference>(`/api/Preference/DeleteReminder?personId=${personId}`), "Reminder turned off.")}>
              Turn off
            </Button>
          </div>
          <details className="mt-2">
            <summary className="cursor-pointer text-xs text-muted-foreground">Change the schedule</summary>
            <div className="mt-1.5 flex flex-wrap items-center gap-2">
              <Select value={interval} onValueChange={(v) => setInterval(v ?? "10")}>
                <SelectTrigger className="w-40" aria-label="Reminder interval">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CadencePresets.map((c) => (
                    <SelectItem key={c.days} value={String(c.days)}>
                      {c.label}
                    </SelectItem>
                  ))}
                  <SelectItem value="custom">Custom…</SelectItem>
                </SelectContent>
              </Select>
              {interval === "custom" && (
                <Input
                  type="number"
                  min={1}
                  max={365}
                  className="w-24"
                  aria-label="Custom interval in days"
                  value={customInterval}
                  onChange={(e) => setCustomInterval(e.target.value)}
                />
              )}
              <Button
                size="sm"
                disabled={busy || intervalDays() == null}
                onClick={() => {
                  const days = intervalDays();
                  if (days == null) return;
                  void run(
                    () => api.post<RelationshipPreference>("/api/Preference/PostReminder", { PersonId: personId, IntervalDays: days, Strict: strict }),
                    `Reminder set: every ${days} days.`,
                  );
                }}
              >
                Update
              </Button>
            </div>
          </details>
        </div>
      ) : (
        <div className="mt-2.5 flex flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <Label htmlFor={`ri-${personId}`} className="text-sm">Remind me about {personName} every</Label>
            <Select value={interval} onValueChange={(v) => setInterval(v ?? "10")}>
              <SelectTrigger id={`ri-${personId}`} className="w-40">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {CadencePresets.map((c) => (
                  <SelectItem key={c.days} value={String(c.days)}>
                    {c.label}
                  </SelectItem>
                ))}
                <SelectItem value="custom">Custom…</SelectItem>
              </SelectContent>
            </Select>
            {interval === "custom" && (
              <Input
                type="number"
                min={1}
                max={365}
                className="w-24"
                aria-label="Custom interval in days"
                value={customInterval}
                onChange={(e) => setCustomInterval(e.target.value)}
              />
            )}
          </div>
          <div>
            <Button
              size="sm"
              disabled={busy || intervalDays() == null}
              onClick={() => {
                const days = intervalDays();
                if (days == null) return;
                void run(
                  () => api.post<RelationshipPreference>("/api/Preference/PostReminder", { PersonId: personId, IntervalDays: days, Strict: strict }),
                  `Reminder set: every ${days} days. You configured this reminder.`,
                );
              }}
            >
              {busy ? "Setting…" : "Set reminder"}
            </Button>
          </div>
        </div>
      )}
      {error && <p className="mt-1.5 text-sm text-destructive">{error}</p>}
      {message && <p className="mt-1.5 text-sm text-emerald-700">{message}</p>}
    </section>
  );
}

export function DueReminders() {
  const [due, setDue] = useState<ReminderDue[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    api
      .get<ReminderDue[]>("/api/Preference/GetDueReminders")
      .then((d) => {
        if (!cancelled) setDue(d ?? []);
      })
      .catch(() => {
        if (!cancelled) setDue([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (due === null || due.length === 0) return null;

  return (
    <section aria-label="Reminders">
      <h2 className="mb-2 font-display text-xl font-semibold">Reminders</h2>
      <ul className="flex flex-col gap-2">
        {due.map((r) => (
          <li key={r.personId} className="rounded-lg border px-4 py-2.5 text-sm">
            <div className="flex flex-wrap items-center gap-x-2">
              <a href={`/people/${r.personId}`} className="font-semibold hover:underline">
                {r.personName ?? "Unnamed contact"}
              </a>
              <span className="text-muted-foreground">
                every {r.intervalDays}d
                {r.silenceDays != null ? ` · quiet for ${r.silenceDays}d` : ""}
              </span>
            </div>
            <p className="mt-0.5 text-xs text-muted-foreground">
              Reminder: every {r.intervalDays} days — {r.sourceLabel} Needs-attention state is separate.
            </p>
          </li>
        ))}
      </ul>
    </section>
  );
}
