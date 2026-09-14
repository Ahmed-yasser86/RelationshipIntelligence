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
import { ChannelEditor } from "@/components/channel-editor";
import { OrganizationPicker } from "@/components/organization-picker";
import { NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import { IngestionSources, submitAndProcess } from "@/lib/ingestion";
import type { ContactChannelRequest, CountryResponse, SystemStatusTagResponse } from "@/lib/types";

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
  const [statusTags, setStatusTags] = useState<SystemStatusTagResponse[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [quick, setQuick] = useState({ Name: "", email: "", CurrentRoles: "", Origin: "", TalkStyle: "" });
  const [quickOrgs, setQuickOrgs] = useState<string[]>([]);
  const [fullOrgs, setFullOrgs] = useState<string[]>([]);
  const [fullChannels, setFullChannels] = useState<ContactChannelRequest[]>([]);
  const [full, setFull] = useState({
    Name: "",
    email: "",
    phone: "",
    Gender: "",
    DateOfBirth: "",
    Address: "",
    CountryId: "",
    StatusTagId: "",
    ContextMemory: "",
    Origin: "",
    LinkedInProfile: "",
    CurrentRoles: "",
    UserDefinedTags: "",
  });

  useEffect(() => {
    api
      .get<CountryResponse[]>("/api/Contacts/GetAllCountries")
      .then(setCountries)
      .catch(() => undefined);
    api
      .get<SystemStatusTagResponse[]>("/api/Contacts/GetSystemStatusTags")
      .then(setStatusTags)
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
        Organizations: quickOrgs.length > 0 ? quickOrgs : null,
        CurrentRoles: splitList(quick.CurrentRoles),
        Origin: quick.Origin.trim() === "" ? null : quick.Origin.trim(),
      });
      // Optional communication profile: stored as memory, same endpoint
      // and ownership as every other entry. Never blocks navigation.
      if (quick.TalkStyle.trim() !== "") {
        try {
          await api.post("/api/Contacts/PostMemoryEntry", {
            PersonId: res.personId,
            Kind: 10,
            Title: quick.TalkStyle.trim().slice(0, 200),
            Detail: null,
          });
        } catch {
          /* profile stays teachable later from the person's page */
        }
      }
      navigate(`/people/${res.personId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not add person.");
    } finally {
      setBusy(false);
    }
  }

  const [pasted, setPasted] = useState("");
  const [pastedError, setPastedError] = useState<string | null>(null);

  async function submitFromText(e: React.FormEvent) {
    e.preventDefault();
    setPastedError(null);
    setBusy(true);
    try {
      const batch = await submitAndProcess(IngestionSources.PersonText, pasted.trim());
      navigate(`/found?batch=${batch.ingestionBatchId}`);
    } catch (err) {
      if (err instanceof ApiError && err.status === 503) {
        // Provider-side outage (see api log "Ingestion extraction model call
        // failed"): the batch is already saved, so send the user to review it
        // — processing can be retried from /found once the provider recovers.
        navigate(`/found`);
      } else {
        setPastedError(
          err instanceof ApiError
            ? err.status === 409
              ? `The assistant could not process the text (${err.body || err.message}). Check co-pilot settings and try again.`
              : err.body || err.message
            : "Could not process the text.",
        );
      }
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
        Organizations: fullOrgs.length > 0 ? fullOrgs : null,
        CurrentRoles: splitList(full.CurrentRoles),
        ConnectionChannels:
          fullChannels.filter((c) => c.name.trim() !== "").length > 0
            ? fullChannels
                .filter((c) => c.name.trim() !== "")
                .map((c) => ({
                  Name: c.name.trim(),
                  Value: c.value?.trim() === "" || c.value == null ? null : c.value.trim(),
                }))
            : null,
        UserDefinedTags: splitList(full.UserDefinedTags),
        SystemStatusTags: full.StatusTagId === "" ? null : [Number(full.StatusTagId)],
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
      <h1 className="font-display text-3xl font-semibold tracking-tight">Add person</h1>
      <p className="mt-1 text-sm text-muted-foreground">
        Quick add captures the essentials; the full form records everything known.
      </p>

      <Tabs defaultValue="quick" className="mt-6">
        <TabsList>
          <TabsTrigger value="quick">Quick add</TabsTrigger>
          <TabsTrigger value="full">Full profile</TabsTrigger>
          <TabsTrigger value="text">From text</TabsTrigger>
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
            <OrganizationPicker idPrefix="q-org" selected={quickOrgs} onChange={setQuickOrgs} />
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="q-roles">Current roles (comma-separated)</Label>
              <Input id="q-roles" value={quick.CurrentRoles} onChange={setQ("CurrentRoles")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="q-origin">How you met</Label>
              <Textarea id="q-origin" rows={2} value={quick.Origin} onChange={setQ("Origin")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="q-talk">How you talk to them (optional)</Label>
              <Textarea
                id="q-talk"
                rows={2}
                value={quick.TalkStyle}
                onChange={setQ("TalkStyle")}
                placeholder="e.g. casual, direct, short messages"
              />
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
                    <SelectItem value="2">Other</SelectItem>
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
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="f-status">Status</Label>
                <Select value={full.StatusTagId} onValueChange={(v) => setFull((f) => ({ ...f, StatusTagId: v ?? "" }))}>
                  <SelectTrigger id="f-status">
                    <SelectValue placeholder="None" />
                  </SelectTrigger>
                  <SelectContent>
                    {statusTags.map((t) => (
                      <SelectItem key={t.statusTagId} value={String(t.statusTagId)}>
                        {t.name}
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
                ["ContextMemory", "Memory note — what should you remember about this person?"],
                ["CurrentRoles", "Roles (comma-separated)"],
                ["UserDefinedTags", "Tags (comma-separated)"],
              ] as const
            ).map(([key, label]) => (
              <div key={key} className="flex flex-col gap-1.5">
                <Label htmlFor={`f-${key}`}>{label}</Label>
                <Input id={`f-${key}`} value={full[key]} onChange={setF(key)} />
              </div>
            ))}
            <ChannelEditor idPrefix="f-ch" channels={fullChannels} onChange={setFullChannels} />
            <OrganizationPicker idPrefix="f-org" selected={fullOrgs} onChange={setFullOrgs} />
            {error && <p className="text-sm text-destructive">{error}</p>}
            <Button type="submit" disabled={busy}>
              {busy ? "Adding…" : "Add person"}
            </Button>
          </form>
        </TabsContent>
        <TabsContent value="text">
          <form onSubmit={submitFromText} className="flex flex-col gap-3 pt-2">
            <p className="text-sm text-muted-foreground">
              Paste anything you already have — a LinkedIn profile, an email signature, a bio, notes. The AI
              extracts every identifiable field, then you review and approve each change.
            </p>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="paste-profile">Pasted information</Label>
              <Textarea
                id="paste-profile"
                rows={10}
                required
                value={pasted}
                onChange={(e) => setPasted(e.target.value)}
                placeholder={"Sara Ali — Engineering Manager at Microsoft, Cairo. Prefers WhatsApp. Met at RiseUp 2025…"}
              />
            </div>
            {pastedError && <p className="text-sm text-destructive">{pastedError}</p>}
            <Button type="submit" disabled={busy || pasted.trim() === ""}>
              {busy ? "Understanding…" : "Understand this text"}
            </Button>
          </form>
        </TabsContent>
      </Tabs>
    </div>
  );
}
