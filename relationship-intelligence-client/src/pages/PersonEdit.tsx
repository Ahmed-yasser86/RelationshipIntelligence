import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
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
import { Textarea } from "@/components/ui/textarea";
import { LoadingList, ErrorState, NavButton } from "@/components/states";
import { ApiError, api } from "@/lib/api";
import type { CountryResponse, PersonDetail } from "@/lib/types";

export function PersonEdit() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [person, setPerson] = useState<PersonDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [countries, setCountries] = useState<CountryResponse[]>([]);
  const [form, setForm] = useState({
    Name: "",
    email: "",
    phone: "",
    Address: "",
    ContextMemory: "",
    Origin: "",
    LinkedInProfile: "",
    OtherInformation: "",
  });
  const [gender, setGender] = useState("");
  const [dob, setDob] = useState("");
  const [countryId, setCountryId] = useState("");

  useEffect(() => {
    if (!id) return;
    api
      .get<PersonDetail>(`/api/Contacts/GetContactByContactID?id=${id}`)
      .then((p) => {
        setPerson(p);
        setForm({
          Name: p.Name ?? "",
          email: p.email ?? "",
          phone: p.phone ?? "",
          Address: p.Address ?? "",
          ContextMemory: p.ContextMemory ?? "",
          Origin: p.Origin ?? "",
          LinkedInProfile: p.LinkedInProfile ?? "",
          OtherInformation: p.OtherInformation ?? "",
        });
        setDob(p.DateOfBirth ? p.DateOfBirth.slice(0, 10) : "");
        setCountryId(p.CountryId ?? "");
      })
      .catch((err) =>
        setError(err instanceof ApiError ? err.message : "Could not load person."),
      );
    api
      .get<CountryResponse[]>("/api/Contacts/GetAllCountries")
      .then(setCountries)
      .catch(() => undefined);
  }, [id]);

  function set(key: keyof typeof form) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setForm((f) => ({ ...f, [key]: e.target.value }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!id) return;
    setError(null);
    setBusy(true);
    try {
      await api.put("/api/Contacts/PutContactItemUpdateRequest", {
        PersonId: id,
        Name: form.Name.trim() === "" ? null : form.Name.trim(),
        email: form.email.trim() === "" ? null : form.email.trim(),
        phone: form.phone.trim() === "" ? null : form.phone.trim(),
        Gender: gender === "" ? null : Number(gender),
        DateOfBirth: dob === "" ? null : dob,
        Address: form.Address.trim() === "" ? null : form.Address.trim(),
        CountryId: countryId === "" ? null : countryId,
        ContextMemory: form.ContextMemory.trim() === "" ? null : form.ContextMemory.trim(),
        Origin: form.Origin.trim() === "" ? null : form.Origin.trim(),
        LinkedInProfile:
          form.LinkedInProfile.trim() === "" ? null : form.LinkedInProfile.trim(),
        OtherInformation:
          form.OtherInformation.trim() === "" ? null : form.OtherInformation.trim(),
      });
      navigate(`/people/${id}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save.");
    } finally {
      setBusy(false);
    }
  }

  if (error && !person) return <ErrorState message={error} />;
  if (!person) return <LoadingList rows={5} />;

  return (
    <div className="max-w-xl">
      <NavButton to={`/people/${id}`} variant="ghost" size="sm" className="mb-4">
        ← Back
      </NavButton>
      <h1 className="text-2xl font-semibold tracking-tight">Edit {person.Name}</h1>
      <form onSubmit={submit} className="mt-6 flex flex-col gap-3">
        <div className="grid grid-cols-2 gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="e-name">Name</Label>
            <Input id="e-name" value={form.Name} onChange={set("Name")} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="e-email">Email</Label>
            <Input id="e-email" type="email" value={form.email} onChange={set("email")} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="e-phone">Phone</Label>
            <Input id="e-phone" value={form.phone} onChange={set("phone")} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>Gender</Label>
            <Select value={gender} onValueChange={(v) => setGender(v ?? "")}>
              <SelectTrigger>
                <SelectValue placeholder={person.Gender || "Select"} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="0">Male</SelectItem>
                <SelectItem value="1">Female</SelectItem>
                <SelectItem value="2">Other</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="e-dob">Date of birth</Label>
            <Input id="e-dob" type="date" value={dob} onChange={(e) => setDob(e.target.value)} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>Country</Label>
            <Select value={countryId} onValueChange={(v) => setCountryId(v ?? "")}>
              <SelectTrigger>
                <SelectValue placeholder={person.CountryName || "Select"} />
              </SelectTrigger>
              <SelectContent>
                {countries.map((c) => (
                  <SelectItem key={c.CountryId} value={c.CountryId}>
                    {c.CountryName}
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
            ["OtherInformation", "Other information"],
          ] as const
        ).map(([key, label]) => (
          <div key={key} className="flex flex-col gap-1.5">
            <Label>{label}</Label>
            <Input value={form[key]} onChange={set(key)} />
          </div>
        ))}
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="e-mem">Memory aid</Label>
          <Textarea id="e-mem" rows={3} value={form.ContextMemory} onChange={set("ContextMemory")} />
        </div>
        {error && <p className="text-sm text-destructive">{error}</p>}
        <Button type="submit" disabled={busy}>
          {busy ? "Saving…" : "Save changes"}
        </Button>
      </form>
    </div>
  );
}
