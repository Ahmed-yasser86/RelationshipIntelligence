import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import type { CountryResponse } from "@/lib/types";

function splitList(v: string): string[] | null {
  const items = v
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
  return items.length > 0 ? items : null;
}

export function PersonNew() {
  const navigate = useNavigate();
  const [countries, setCountries] = useState<CountryResponse[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [quick, setQuick] = useState({ Name: "", email: "", Organizations: "", CurrentRoles: "", Origin: "" });
  const [full, setFull] = useState({
    Name: "",
    email: "",
    phone: "",
    Gender: "",
    DateOfBirth: "",
    Address: "",
    CountryId: "",
    ContextMemory: "",
    Origin: "",
    LinkedInProfile: "",
    Organizations: "",
    CurrentRoles: "",
    ConnectionChannels: "",
    UserDefinedTags: "",
  });

  useEffect(() => {
    api
      .get<CountryResponse[]>("/api/Contacts/GetAllCountries")
      .then(setCountries)
      .catch(() => undefined);
  }, []);

  function setQ(key: keyof typeof quick) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setQuick((f) => ({ ...f, [key]: e.target.value }));
  }
  function setF(key: keyof typeof full) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setFull((f) => ({ ...f, [key]: e.target.value }));
  }

  async function submitQuick(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const res = await api.post<{ personId: string }>("/api/Contacts/PostQuickAddContact", {
        Name: quick.Name.trim(),
        email: quick.email.trim(),
        Organizations: splitList(quick.Organizations),
        CurrentRoles: splitList(quick.CurrentRoles),
        Origin: quick.Origin.trim() === "" ? null : quick.Origin.trim(),
      });
      navigate(`/people/${res.personId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not add person.");
    } finally {
      setBusy(false);
    }
  }

  async function submitFull(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const res = await api.post<{ personId: string }>("/api/Contacts/PostAddPersoneRequest", {
        Name: full.Name.trim() === "" ? null : full.Name.trim(),
        email: full.email.trim() === "" ? null : full.email.trim(),
        phone: full.phone.trim() === "" ? null : full.phone.trim(),
        Gender: full.Gender === "" ? null : Number(full.Gender),
        DateOfBirth: full.DateOfBirth === "" ? null : full.DateOfBirth,
        Address: full.Address.trim() === "" ? null : full.Address.trim(),
        CountryId: full.CountryId === "" ? null : full.CountryId,
        NewsLetter: null,
        ContextMemory: full.ContextMemory.trim() === "" ? null : full.ContextMemory.trim(),
        Origin: full.Origin.trim() === "" ? null : full.Origin.trim(),
        LinkedInProfile: full.LinkedInProfile.trim() === "" ? null : full.LinkedInProfile.trim(),
        Organizations: splitList(full.Organizations),
        CurrentRoles: splitList(full.CurrentRoles),
        ConnectionChannels: splitList(full.ConnectionChannels),
        UserDefinedTags: splitList(full.UserDefinedTags),
      });
      navigate(`/people/${res.personId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not add person.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="max-w-xl">
      <NavButton to="/people" variant="ghost" size="sm" className="mb-4">
        ← People
      </NavButton>
      <h1 className="text-2xl font-semibold tracking-tight">Add person</h1>
      <p className="mt-1 text-sm text-muted-foreground">
        Quick add captures the essentials; the full form records everything known.
      </p>

      <Tabs defaultValue="quick" className="mt-6">
        <TabsList>
          <TabsTrigger value="quick">Quick add</TabsTrigger>
          <TabsTrigger value="full">Full profile</TabsTrigger>
        </TabsList>
        <TabsContent value="quick">
          <form onSubmit={submitQuick} className="flex flex-col gap-3 pt-2">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="q-name">Name</Label>
              <Input id="q-name" required value={quick.Name} onChange={setQ("Name")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="q-email">Email</Label>
              <Input id="q-email" type="email" required value={quick.email} onChange={setQ("email")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="q-org">Organizations (comma-separated)</Label>
              <Input id="q-org" value={quick.Organizations} onChange={setQ("Organizations")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="q-roles">Current roles (comma-separated)</Label>
              <Input id="q-roles" value={quick.CurrentRoles} onChange={setQ("CurrentRoles")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="q-origin">How you met</Label>
              <Textarea id="q-origin" rows={2} value={quick.Origin} onChange={setQ("Origin")} />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <Button type="submit" disabled={busy}>
              {busy ? "Adding…" : "Add person"}
            </Button>
          </form>
        </TabsContent>
        <TabsContent value="full">
          <form onSubmit={submitFull} className="flex flex-col gap-3 pt-2">
            <div className="grid grid-cols-2 gap-3">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="f-name">Name</Label>
                <Input id="f-name" value={full.Name} onChange={setF("Name")} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="f-email">Email</Label>
                <Input id="f-email" type="email" value={full.email} onChange={setF("email")} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="f-phone">Phone</Label>
                <Input id="f-phone" value={full.phone} onChange={setF("phone")} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="f-gender">Gender</Label>
                <Select value={full.Gender} onValueChange={(v) => setFull((f) => ({ ...f, Gender: v ?? "" }))}>
                  <SelectTrigger id="f-gender">
                    <SelectValue placeholder="Select" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="0">Male</SelectItem>
                    <SelectItem value="1">Female</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="f-dob">Date of birth</Label>
                <Input id="f-dob" type="date" value={full.DateOfBirth} onChange={setF("DateOfBirth")} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="f-country">Country</Label>
                <Select value={full.CountryId} onValueChange={(v) => setFull((f) => ({ ...f, CountryId: v ?? "" }))}>
                  <SelectTrigger id="f-country">
                    <SelectValue placeholder="Select" />
                  </SelectTrigger>
                  <SelectContent>
                    {countries.map((c) => (
                      <SelectItem key={c.countryId} value={c.countryId}>
                        {c.countryName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            {(
              [
                ["Address", "Address"],
                ["LinkedInProfile", "LinkedIn URL"],
                ["Origin", "How you met"],
                ["ContextMemory", "Memory aid"],
                ["Organizations", "Organizations (comma-separated)"],
                ["CurrentRoles", "Roles (comma-separated)"],
                ["ConnectionChannels", "Channels (comma-separated)"],
                ["UserDefinedTags", "Tags (comma-separated)"],
              ] as const
            ).map(([key, label]) => (
              <div key={key} className="flex flex-col gap-1.5">
                <Label htmlFor={`f-${key}`}>{label}</Label>
                <Input id={`f-${key}`} value={full[key]} onChange={setF(key)} />
              </div>
            ))}
            {error && <p className="text-sm text-destructive">{error}</p>}
            <Button type="submit" disabled={busy}>
              {busy ? "Adding…" : "Add person"}
            </Button>
          </form>
        </TabsContent>
      </Tabs>
    </div>
  );
}
