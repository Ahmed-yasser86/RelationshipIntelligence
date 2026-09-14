import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { ApiError } from "@/lib/api";
import { IngestionSources, sourceHint, submitAndProcess } from "@/lib/ingestion";

/**
 * One reusable entry point into the unified ingestion pipeline: paste raw
 * text, the AI extracts findings, review happens in Things I Found. Same
 * engine and same review model from every entry point.
 */
export function IngestDialog({
  triggerLabel,
  title,
  defaultSource,
  allowSourceChoice,
}: {
  triggerLabel: string;
  title: string;
  defaultSource: number;
  allowSourceChoice: boolean;
}) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [source, setSource] = useState(String(defaultSource));
  const [text, setText] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const batch = await submitAndProcess(Number(source), text.trim());
      setOpen(false);
      setText("");
      navigate(`/found?batch=${batch.ingestionBatchId}`);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.status === 503 || err.status === 409
            ? `The assistant could not process the text (${err.body || err.message}). Check co-pilot settings and try again.`
            : err.body || err.message
          : "Could not process the text.",
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger render={<Button size="sm" variant="outline">{triggerLabel}</Button>} />
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <form onSubmit={submit} className="flex flex-col gap-3">
          {allowSourceChoice && (
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="ingest-source">What kind of text is this?</Label>
              <Select value={source} onValueChange={(v) => setSource(v ?? String(defaultSource))}>
                <SelectTrigger id="ingest-source">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(IngestionSources.PersonText)}>Profile / bio / notes</SelectItem>
                  <SelectItem value={String(IngestionSources.ConversationUpdate)}>Conversation</SelectItem>
                  <SelectItem value={String(IngestionSources.GroupText)}>Group / multi-person text</SelectItem>
                </SelectContent>
              </Select>
              <p className="text-xs text-muted-foreground">{sourceHint(Number(source))}</p>
            </div>
          )}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ingest-text">Paste the text you already have</Label>
            <Textarea
              id="ingest-text"
              rows={8}
              required
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="Paste a LinkedIn profile, an email signature, a conversation, meeting notes…"
            />
          </div>
          <p className="text-xs text-muted-foreground">
            The AI organizes what it finds into per-person proposals. You review and approve each change in Things I
            Found — nothing is saved silently.
          </p>
          {error && <p className="text-sm text-destructive">{error}</p>}
          <DialogFooter>
            <Button type="submit" disabled={busy || text.trim() === ""}>
              {busy ? "Understanding…" : "Understand this text"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
