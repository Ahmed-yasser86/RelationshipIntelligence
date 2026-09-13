import { useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
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
import type { PagedResult, PersonView, SystemStatusTagResponse } from "@/lib/types";

const PAGE_SIZE = 10;

function lastContact(p: PersonView): string {
  const times = (p.interactions ?? [])
    .map((i) => i.timeOfInteraction)
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
  const [statusTags, setStatusTags] = useState<SystemStatusTagResponse[]>([]);
  const requestId = useRef(0);

  useEffect(() => {
    api.get<string[]>("/api/Contacts/GetValidSearchFields").then(setSearchFields).catch(() => undefined);
    api.get<string[]>("/api/Contacts/GetValidSortFields").then(setSortFields).catch(() => undefined);
    api.get<SystemStatusTagResponse[]>("/api/Contacts/GetSystemStatusTags").then(setStatusTags).catch(() => undefined);
  }, []);

  const load = useCallback(
    async (pageNumber: number, overrideQuery?: string) => {
      const mine = ++requestId.current;
      setError(null);
      const activeQuery = overrideQuery ?? query;
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
        } else if (activeQuery.trim() !== "") {
          const params = new URLSearchParams({
            QueryParamter: activeQuery.trim(),
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
        if (requestId.current !== mine) return;
        setData(result);
        setPage(pageNumber);
      } catch (err) {
        if (requestId.current !== mine) return;
        setError(err instanceof ApiError ? err.message : "Could not load people.");
      }
    },
    [query, field, sortBy, composite],
  );

  useEffect(() => {
    const mine = ++requestId.current;
    void (async () => {
      try {
        const result = await api.get<PagedResult<PersonView>>(
          `/api/Contacts/GetContactsGrid?pageNumber=1&pageSize=${PAGE_SIZE}`,
        );
        if (requestId.current !== mine) return;
        setData(result);
        setPage(1);
      } catch (err) {
        if (requestId.current !== mine) return;
        setError(err instanceof ApiError ? err.message : "Could not load people.");
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const data = new FormData(e.currentTarget);
    const q = (data.get("q") ?? "").toString();
    setQuery(q);
    const freshComposite = { ...composite };
    (Object.keys(composite) as (keyof typeof composite)[]).forEach((k) => {
      if (k === "SystemStatusTagName") return;
      const v = data.get(`cf-${k}`);
      if (v != null) freshComposite[k] = v.toString();
    });
    setComposite(freshComposite);
    void loadComposite(1, q, freshComposite);
  }

  type Composite = typeof composite;

  async function loadComposite(
    pageNumber: number,
    activeQuery: string,
    activeComposite: Composite,
  ) {
    const mine = ++requestId.current;
    setError(null);
    try {
      const hasComposite = Object.values(activeComposite).some((v) => v.trim() !== "");
      let result: PagedResult<PersonView>;
      if (hasComposite) {
        const params = new URLSearchParams({
          pageNumber: String(pageNumber),
          pageSize: String(PAGE_SIZE),
        });
        result = await api.post<PagedResult<PersonView>>(
          `/api/Contacts/QueryContactsByCompositeFilter?${params}`,
          Object.fromEntries(
            Object.entries(activeComposite).map(([k, v]) => [k, v.trim() === "" ? null : v.trim()]),
          ),
        );
      } else {
        return load(pageNumber, activeQuery);
      }
      if (requestId.current !== mine) return;
      setData(result);
      setPage(pageNumber);
    } catch (err) {
      if (requestId.current !== mine) return;
      setError(err instanceof ApiError ? err.message : "Could not load people.");
    }
  }

  const totalPages =
    data != null ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  const [searchParams, setSearchParams] = useSearchParams();
  useEffect(() => {
    const org = (searchParams.get("org") ?? "").trim();
    if (org === "") return;
    setSearchParams({}, { replace: true });
    setShowFilters(true);
    const next = {
      Name: "",
      Email: "",
      Phone: "",
      CircleName: org,
      ContactItemRole: "",
      SystemStatusTagName: "",
      UserDefinedTagName: "",
    };
    setComposite(next);
    void loadComposite(1, "", next);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div>
      <div className="mb-4 flex items-end justify-between">
        <div>
          <h1 className="font-display text-3xl font-semibold tracking-tight">People</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data ? `${data.totalCount} people in your network` : "Your network"}
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
            name="q"
            placeholder="Name, email, phone, organization…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="people-field">Field</Label>
          <Select value={field} onValueChange={(v) => setField(v ?? "Name")}>
            <SelectTrigger id="people-field" className="w-44">
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
          <Label htmlFor="people-sort">Sort</Label>
          <Select value={sortBy} onValueChange={(v) => setSortBy(v ?? "Name")}>
            <SelectTrigger id="people-sort" className="w-44">
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
              ["UserDefinedTagName", "Tag"],
            ] as const
          ).map(([key, label]) => (
            <div key={key} className="flex flex-col gap-1.5">
              <Label htmlFor={`cf-${key}`}>{label}</Label>
              <Input
                id={`cf-${key}`}
                name={`cf-${key}`}
                value={composite[key]}
                onChange={(e) => setComposite((c) => ({ ...c, [key]: e.target.value }))}
              />
            </div>
          ))}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="cf-SystemStatusTagName">Status tag</Label>
            <Select
              value={composite.SystemStatusTagName}
              onValueChange={(v) =>
                setComposite((c) => ({ ...c, SystemStatusTagName: v === "__any" ? "" : (v ?? "") }))
              }
            >
              <SelectTrigger id="cf-SystemStatusTagName" name="cf-SystemStatusTagName">
                <SelectValue placeholder="Any status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__any">Any status</SelectItem>
                {statusTags.map((t) => (
                  <SelectItem key={t.statusTagId} value={t.name}>
                    {t.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
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
      {data !== null && data.items.length === 0 && (
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
      {data !== null && data.items.length > 0 && (
        <>
          <ul className="flex flex-col gap-2">
            {data.items.map((p) => (
              <li key={p.personId} className="rounded-lg border px-4 py-3">
                <div className="flex items-center gap-3">
                  <PersonAvatar name={p.name} />
                  <div className="min-w-0 flex-1">
                    <Link
                      to={`/people/${p.personId}`}
                      className="truncate text-sm font-semibold hover:underline"
                    >
                      {p.name ?? "Unnamed contact"}
                    </Link>
                    <p className="truncate text-xs text-muted-foreground">
                      {[p.circles[0]?.name, p.contactItemRoles[0]?.role]
                        .filter(Boolean)
                        .join(" · ") || p.countryName || p.email || ""}
                      {" · "}last contact {lastContact(p)}
                    </p>
                    <div className="mt-1 flex flex-wrap gap-1">
                      {p.systemStatusTags.slice(0, 3).map((t) => (
                        <Badge key={t.statusTagId} variant="secondary" className="text-[11px]">
                          {t.name}
                        </Badge>
                      ))}
                      {p.userDefinedTags.slice(0, 3).map((t) => (
                        <Badge key={t.tagId} variant="outline" className="text-[11px]">
                          {t.tagName}
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
              Page {data.pageNumber} of {totalPages} · {data.totalCount} total
            </span>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={data.pageNumber <= 1}
                onClick={() => void load(data.pageNumber - 1)}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={!data.hasMore}
                onClick={() => void load(data.pageNumber + 1)}
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
