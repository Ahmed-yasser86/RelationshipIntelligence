import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError, api } from "@/lib/api";
import type { OrganizationResponse } from "@/lib/types";

export function OrganizationPicker({
  idPrefix,
  selected,
  onChange,
}: {
  idPrefix: string;
  selected: string[];
  onChange: (names: string[]) => void;
}) {
  const [orgs, setOrgs] = useState<OrganizationResponse[]>([]);
  const [draft, setDraft] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function refresh() {
    try {
      setOrgs(await api.get<OrganizationResponse[]>("/api/Contacts/GetOrganizations"));
    } catch {
      setOrgs([]);
    }
  }

  useEffect(() => {
    void refresh();
  }, []);

  function toggle(name: string) {
    onChange(
      selected.includes(name)
        ? selected.filter((s) => s !== name)
        : [...selected, name],
    );
  }

  async function addNew() {
    const name = draft.trim();
    if (name === "") return;
    setError(null);
    setBusy(true);
    try {
      const created = await api.post<OrganizationResponse>(
        "/api/Contacts/PostOrganization",
        { Name: name },
      );
      setDraft("");
      await refresh();
      if (!selected.includes(created.name)) onChange([...selected, created.name]);
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) {
        if (!selected.includes(name)) onChange([...selected, name]);
        setDraft("");
        await refresh();
      } else {
        setError(err instanceof ApiError ? err.body || err.message : "Could not add organization.");
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <Label id={`${idPrefix}-label`}>Organizations</Label>
      {orgs.length > 0 ? (
        <div className="flex max-h-32 flex-wrap gap-1.5 overflow-auto" role="group" aria-labelledby={`${idPrefix}-label`}>
          {orgs.map((o) => {
            const checked = selected.includes(o.name);
            return (
              <button
                key={o.circleId}
                type="button"
                onClick={() => toggle(o.name)}
                aria-pressed={checked}
                title={checked ? `Remove ${o.name}` : `Add ${o.name}`}
                className={`rounded-full border px-2.5 py-1 text-xs transition-colors ${
                  checked
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border bg-background text-foreground hover:border-primary"
                }`}
              >
                {o.name}
              </button>
            );
          })}
        </div>
      ) : (
        <p className="text-xs text-muted-foreground">
          No organizations yet — create the first one below, or manage them all under Organizations.
        </p>
      )}
      <div className="flex gap-1.5">
        <Input
          id={`${idPrefix}-new`}
          value={draft}
          maxLength={100}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              void addNew();
            }
          }}
          placeholder="New organization…"
          aria-label="New organization name"
        />
        <Button type="button" size="sm" variant="outline" disabled={busy || draft.trim() === ""} onClick={() => void addNew()}>
          {busy ? "Adding…" : "Add"}
        </Button>
      </div>
      {selected.length > 0 && (
        <p className="text-xs text-muted-foreground">Selected: {selected.join(", ")}</p>
      )}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}
