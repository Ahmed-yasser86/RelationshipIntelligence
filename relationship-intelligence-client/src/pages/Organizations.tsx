import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { EmptyState, ErrorState, LoadingList } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import type { OrganizationResponse } from "@/lib/types";

export function Organizations() {
  const [orgs, setOrgs] = useState<OrganizationResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [newName, setNewName] = useState("");
  const [busy, setBusy] = useState(false);
  const [renaming, setRenaming] = useState<string | null>(null);
  const [renameValue, setRenameValue] = useState("");

  const load = useCallback(async () => {
    setError(null);
    try {
      setOrgs(await api.get<OrganizationResponse[]>("/api/Contacts/GetOrganizations"));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load organizations.");
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function create(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const name = newName.trim();
    if (name === "") return;
    setError(null);
    setBusy(true);
    try {
      await api.post("/api/Contacts/PostOrganization", { Name: name });
      setNewName("");
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not create organization.");
    } finally {
      setBusy(false);
    }
  }

  async function rename(org: OrganizationResponse) {
    const name = renameValue.trim();
    if (name === "" || name === org.name) {
      setRenaming(null);
      return;
    }
    setError(null);
    try {
      await api.put("/api/Contacts/PutOrganization", { CircleId: org.circleId, Name: name });
      setRenaming(null);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not rename organization.");
    }
  }

  async function remove(org: OrganizationResponse) {
    if (!window.confirm(`Delete "${org.name}"?`)) return;
    setError(null);
    try {
      await api.del(`/api/Contacts/DeleteOrganization?id=${org.circleId}`);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not delete organization.");
    }
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="font-display text-3xl font-semibold tracking-tight">Organizations</h1>
        <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
          The shared directory behind every contact — companies, communities, families.
          Rename freely; members follow. Deletion is refused while contacts still belong.
        </p>
      </div>

      <form onSubmit={create} className="mb-6 flex max-w-xl items-end gap-2">
        <div className="flex flex-1 flex-col gap-1.5">
          <Label htmlFor="org-new">New organization</Label>
          <Input
            id="org-new"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="e.g. Proceedit"
            maxLength={100}
          />
        </div>
        <Button type="submit" disabled={busy || newName.trim() === ""}>
          {busy ? "Adding…" : "Add"}
        </Button>
      </form>

      {error && (
        <div className="mb-4">
          <ErrorState message={error} onRetry={() => void load()} />
        </div>
      )}
      {orgs === null && !error && <LoadingList rows={5} />}
      {orgs !== null && orgs.length === 0 && (
        <EmptyState
          title="No organizations yet"
          body="Add the first one above — it becomes pickable the next time you add or edit a contact."
        />
      )}
      {orgs !== null && orgs.length > 0 && (
        <div className="overflow-x-auto rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead className="w-28">Members</TableHead>
                <TableHead className="w-56 text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {orgs.map((org) => (
                <TableRow key={org.circleId}>
                  <TableCell className="font-medium">
                    {renaming === org.circleId ? (
                      <Input
                        aria-label={`Rename ${org.name}`}
                        autoFocus
                        value={renameValue}
                        maxLength={100}
                        onChange={(e) => setRenameValue(e.target.value)}
                        onBlur={() => void rename(org)}
                        onKeyDown={(e) => {
                          if (e.key === "Enter") void rename(org);
                          if (e.key === "Escape") setRenaming(null);
                        }}
                      />
                    ) : (
                      org.name
                    )}
                  </TableCell>
                  <TableCell className="tabular-nums">
                    {org.memberCount > 0 ? (
                      <Link
                        to={`/people?org=${encodeURIComponent(org.name)}`}
                        className="hover:underline"
                        title={`View members of ${org.name}`}
                      >
                        {org.memberCount}
                      </Link>
                    ) : (
                      <span className="text-muted-foreground">0</span>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1.5">
                      {renaming === org.circleId ? (
                        <Button size="sm" variant="ghost" onClick={() => setRenaming(null)}>
                          Cancel
                        </Button>
                      ) : (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => {
                            setRenaming(org.circleId);
                            setRenameValue(org.name);
                          }}
                        >
                          Rename
                        </Button>
                      )}
                      <Button
                        size="sm"
                        variant="ghost"
                        className="text-destructive hover:text-destructive"
                        onClick={() => void remove(org)}
                        disabled={org.memberCount > 0}
                        title={
                          org.memberCount > 0
                            ? `Move its ${org.memberCount} member(s) elsewhere first`
                            : `Delete ${org.name}`
                        }
                      >
                        Delete
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
