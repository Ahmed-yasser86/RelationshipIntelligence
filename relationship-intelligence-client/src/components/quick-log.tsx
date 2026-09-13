import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
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
import { InteractionTypes } from "@/lib/types";
import type { RelationshipHealth } from "@/lib/types";

export function QuickLog({
  person,
  onDone,
}: {
  person: RelationshipHealth;
  onDone: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [type, setType] = useState("1");
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const data = new FormData(e.currentTarget);
    const formTitle = (data.get("title") ?? "").toString().trim();
    setError(null);
    setBusy(true);
    try {
      await api.post("/api/Contacts/PostLogInteraction", {
        PersonId: person.personId,
        TimeOfInteraction: new Date().toISOString(),
        InteractionType: Number(type),
        InteractionTitle: formTitle,
        InteractionDescription: null,
      });
      setOpen(false);
      setTitle("");
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not log.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger
        render={<Button size="sm" variant="outline">Log</Button>}
      />
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Log contact with {person.name ?? "this contact"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={submit} className="flex flex-col gap-3">
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor={`ql-type-${person.personId}`}>Type</Label>
              <Select value={type} onValueChange={(v) => setType(v ?? "1")}>
                <SelectTrigger id={`ql-type-${person.personId}`}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {InteractionTypes.map((t, i) => (
                    <SelectItem key={t} value={String(i)}>
                      {t}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor={`ql-title-${person.personId}`}>What was it about</Label>
              <Input
                id={`ql-title-${person.personId}`}
                name="title"
                required
                maxLength={100}
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </div>
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
          <DialogFooter>
            <Button type="submit" disabled={busy}>
              {busy ? "Saving…" : "Save"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
