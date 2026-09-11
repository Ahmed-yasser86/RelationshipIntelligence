import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
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
import { EmptyState, ErrorState, LoadingList, NavButton, PersonAvatar } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { timeAgo } from "@/lib/format";
import type { PagedResult, PersonView } from "@/lib/types";

const PAGE_SIZE = 10;

function lastContact(p: PersonView): string {
  const times = (p.Interactions ?? [])
    .map((i) => i.TimeOfInteraction)
    .sort()
    .reverse();
  return times.length > 0 ? timeAgo(times[0]) : "no contact yet";
}

export function People() {
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PagedResult<PersonView> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [searchFields, setSearchFields] = useState<string[]>(["Name"]);
  const [sortFields, setSortFields] = useState<string[]>(["Name"]);
  const [query, setQuery] = useState("");
  const [field, setField] = useState("Name");
  const [sortBy, setSortBy] = useState("Name");
  const [composite, setComposite] = useState({
    Name: "",
    Email: "",
    Phone: "",
    CircleName: "",
    ContactItemRole: "",
    SystemStatusTagName: "",
    UserDefinedTagName: "",
  });
  const [showFilters, setShowFilters] = useState(false);

  useEffect(() => {
    api.get<string[]>("/api/Contacts/GetValidSearchFields").then(setSearchFields).catch(() => undefined);
    api.get<string[]>("/api/Contacts/GetValidSortFields").then(setSortFields).catch(() => undefined);
  }, []);

  const load = useCallback(
    async (pageNumber: number) => {
      setError(null);
      try {
        const hasComposite = Object.values(composite).some((v) => v.trim() !== "");
        let result: PagedResult<PersonView>;
        if (hasComposite) {
          const params = new URLSearchParams({
            pageNumber: String(pageNumber),
            pageSize: String(PAGE_SIZE),
          });
          result = await api.post<PagedResult<PersonView>>(
            `/api/Contacts/QueryContactsByCompositeFilter?${params}`,
            Object.fromEntries(
              Object.entries(composite).map(([k, v]) => [k, v.trim() === "" ? null : v.trim()]),
            ),
          );
        } else if (query.trim() !== "") {
          const params = new URLSearchParams({
            QueryParamter: query.trim(),
            SearchBy: field,
            pageNumber: String(pageNumber),
            pageSize: String(PAGE_SIZE),
          });
          result = await api.get<PagedResult<PersonView>>(
            `/api/Contacts/GetContactsFilteredByBatches?${params}`,
          );
        } else if (sortBy !== "Name") {
          const params = new URLSearchParams({
            page: String(pageNumber),
            size: String(PAGE_SIZE),
            sortBy,
          });
          result = await api.get<PagedResult<PersonView>>(
            `/api/Contacts/GetSearchSortedPeopleBy?${params}`,
          );
        } else {
          result = await api.get<PagedResult<PersonView>>(
            `/api/Contacts/GetContactsGrid?pageNumber=${pageNumber}&pageSize=${PAGE_SIZE}`,
          );
        }
        setData(result);
        setPage(pageNumber);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Could not load people.");
      }
    },
    [query, field, sortBy, composite],
  );

  useEffect(() => {
    void load(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function submit(e: React.FormEvent) {
    e.preventDefault();
    void load(1);
  }

  const totalPages =
    data != null ? Math.max(1, Math.ceil(data.TotalCount / data.PageSize)) : 1;

  return (
    <div>
      <div className="mb-4 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">People</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data ? `${data.TotalCount} people in your network` : "Your network"}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => setShowFilters((s) => !s)}>
            {showFilters ? "Hide filters" : "Filters"}
          </Button>
          <NavButton to="/people/new" size="sm">
            Add person
          </NavButton>
        </div>
      </div>

      <form onSubmit={submit} className="mb-4 flex flex-wrap items-end gap-2">
        <div className="flex min-w-52 flex-1 flex-col gap-1.5">
          <Label htmlFor="q">Search</Label>
          <Input
            id="q"
            placeholder="Name, email, phone, organization…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>Field</Label>
          <Select value={field} onValueChange={(v) => setField(v ?? "Name")}>
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {searchFields.map((f) => (
                <SelectItem key={f} value={f}>
                  {f}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>Sort</Label>
          <Select value={sortBy} onValueChange={(v) => setSortBy(v ?? "Name")}>
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {sortFields.map((f) => (
                <SelectItem key={f} value={f}>
                  {f}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <Button type="submit">Apply</Button>
      </form>

      {showFilters && (
        <form
          onSubmit={submit}
          className="mb-4 grid grid-cols-2 gap-2 rounded-lg border p-4 md:grid-cols-4"
        >
          {(
            [
              ["Name", "Name"],
              ["Email", "Email"],
              ["Phone", "Phone"],
              ["CircleName", "Organization"],
              ["ContactItemRole", "Role"],
              ["SystemStatusTagName", "Status tag"],
              ["UserDefinedTagName", "Tag"],
            ] as const
          ).map(([key, label]) => (
            <div key={key} className="flex flex-col gap-1.5">
              <Label>{label}</Label>
              <Input
                value={composite[key]}
                onChange={(e) => setComposite((c) => ({ ...c, [key]: e.target.value }))}
              />
            </div>
          ))}
          <div className="flex items-end gap-2">
            <Button type="submit" size="sm">
              Filter
            </Button>
            <Button
              type="button"
              size="sm"
              variant="ghost"
              onClick={() => {
                setComposite({
                  Name: "",
                  Email: "",
                  Phone: "",
                  CircleName: "",
                  ContactItemRole: "",
                  SystemStatusTagName: "",
                  UserDefinedTagName: "",
                });
                setQuery("");
              }}
            >
              Clear
            </Button>
          </div>
        </form>
      )}

      {error && <ErrorState message={error} onRetry={() => void load(page)} />}
      {data === null && !error && <LoadingList rows={6} />}
      {data !== null && data.Items.length === 0 && (
        <EmptyState
          title="No people found"
          body="Nothing matches the current search. Clear the search or add the person to your network."
          action={
            <NavButton to="/people/new" size="sm">
              Add person
            </NavButton>
          }
        />
      )}
      {data !== null && data.Items.length > 0 && (
        <>
          <ul className="flex flex-col gap-2">
            {data.Items.map((p) => (
              <li key={p.PersonId} className="rounded-lg border px-4 py-3">
                <div className="flex items-center gap-3">
                  <PersonAvatar name={p.Name} />
                  <div className="min-w-0 flex-1">
                    <Link
                      to={`/people/${p.PersonId}`}
                      className="truncate text-sm font-semibold hover:underline"
                    >
                      {p.Name}
                    </Link>
                    <p className="truncate text-xs text-muted-foreground">
                      {[p.Circles[0]?.Name, p.ContactItemRoles[0]?.Role]
                        .filter(Boolean)
                        .join(" · ") || p.CountryName || p.email || ""}
                      {" · "}last contact {lastContact(p)}
                    </p>
                    <div className="mt-1 flex flex-wrap gap-1">
                      {p.SystemStatusTags.slice(0, 3).map((t) => (
                        <Badge key={t.StatusTagId} variant="secondary" className="text-[11px]">
                          {t.Name}
                        </Badge>
                      ))}
                      {p.UserDefinedTags.slice(0, 3).map((t) => (
                        <Badge key={t.TagId} variant="outline" className="text-[11px]">
                          {t.TagName}
                        </Badge>
                      ))}
                    </div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
          <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
            <span>
              Page {data.PageNumber} of {totalPages} · {data.TotalCount} total
            </span>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={data.PageNumber <= 1}
                onClick={() => void load(data.PageNumber - 1)}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={!data.HasMore}
                onClick={() => void load(data.PageNumber + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
