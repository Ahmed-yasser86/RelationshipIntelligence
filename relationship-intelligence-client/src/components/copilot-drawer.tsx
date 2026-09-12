import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
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
import { ApiError, api } from "@/lib/api";
import { useCopilot } from "@/lib/copilot";
import type { AiProviderSettings, ChatTurn, CopilotAnswer } from "@/lib/types";

const PROVIDERS = ["OpenAI", "Gemini", "Custom"] as const;
const PRESETS: Record<string, { model: string; baseUrl: string }> = {
  OpenAI: { model: "gpt-4o-mini", baseUrl: "https://api.openai.com/v1" },
  Gemini: { model: "gemini-3.1-flash-lite", baseUrl: "https://generativelanguage.googleapis.com/v1beta/openai" },
  Custom: { model: "", baseUrl: "https://api.openai.com/v1" },
};

interface Msg {
  role: "user" | "assistant";
  text: string;
  citations?: CopilotAnswer["citations"];
}

function ProviderSettings({ onSaved }: { onSaved: () => void }) {
  const [settings, setSettings] = useState<AiProviderSettings | null>(null);
  const [provider, setProvider] = useState("Gemini");
  const [model, setModel] = useState(PRESETS.Gemini.model);
  const [baseUrl, setBaseUrl] = useState(PRESETS.Gemini.baseUrl);
  const [key, setKey] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    api
      .get<AiProviderSettings>("/api/Copilot/GetAiSettings")
      .then((s) => {
        setSettings(s);
        if (s.model) {
          setProvider(PROVIDERS.includes(s.provider as (typeof PROVIDERS)[number]) ? s.provider : "Custom");
          setModel(s.model);
          if (s.baseUrl) setBaseUrl(s.baseUrl);
        }
      })
      .catch(() => undefined);
  }, []);

  function pickProvider(p: string) {
    setProvider(p);
    const preset = PRESETS[p];
    if (preset) {
      setModel(preset.model);
      setBaseUrl(preset.baseUrl);
    }
  }

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSaved(false);
    setBusy(true);
    try {
      const updated = await api.put<AiProviderSettings>("/api/Copilot/PutAiSettings", {
        Provider: provider,
        Model: model.trim(),
        BaseUrl: baseUrl.trim() === "" ? null : baseUrl.trim(),
        ApiKey: key.trim() === "" ? null : key.trim(),
      });
      setSettings(updated);
      setKey("");
      setSaved(true);
      onSaved();
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Could not save settings.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={save} className="flex flex-col gap-2 rounded-lg border p-3">
      <p className="text-xs font-semibold">
        AI provider {settings?.hasKey ? <span className="font-normal text-emerald-700">(key saved)</span> : <span className="font-normal text-muted-foreground">(no key saved)</span>}
      </p>
      <div className="grid grid-cols-2 gap-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="cp-provider">Provider</Label>
          <Select value={provider} onValueChange={(v) => pickProvider(v ?? "Custom")}>
            <SelectTrigger id="cp-provider">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {PROVIDERS.map((p) => (
                <SelectItem key={p} value={p}>
                  {p}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="cp-model">Model</Label>
          <Input id="cp-model" value={model} maxLength={200} onChange={(e) => setModel(e.target.value)} placeholder="e.g. gemini-3.1-flash-lite" />
        </div>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="cp-url">Base URL (OpenAI-compatible)</Label>
        <Input id="cp-url" value={baseUrl} maxLength={500} onChange={(e) => setBaseUrl(e.target.value)} />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="cp-key">API key {settings?.hasKey ? "(leave blank to keep)" : ""}</Label>
        <Input id="cp-key" type="password" value={key} autoComplete="off" onChange={(e) => setKey(e.target.value)} placeholder={settings?.hasKey ? "••••••••" : "Paste key"} />
      </div>
      {error && <p className="text-xs text-destructive">{error}</p>}
      {saved && <p className="text-xs text-emerald-700">Saved.</p>}
      <div>
        <Button type="submit" size="sm" disabled={busy || model.trim() === ""}>
          {busy ? "Saving…" : "Save provider"}
        </Button>
      </div>
    </form>
  );
}

export function CopilotDrawer() {
  const { open, closeCopilot, personId, personName } = useCopilot();
  const [messages, setMessages] = useState<Msg[]>([]);
  const [input, setInput] = useState("");
  const [busy, setBusy] = useState(false);
  const [needsSetup, setNeedsSetup] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (open) {
      setMessages([]);
      setNeedsSetup(false);
      bottomRef.current?.scrollIntoView();
    }
  }, [open, personId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  async function send(e?: React.FormEvent) {
    e?.preventDefault();
    const question = input.trim();
    if (question === "" || busy) return;
    const history: ChatTurn[] = messages.map((m) => ({ role: m.role, text: m.text }));
    setMessages((m) => [...m, { role: "user", text: question }]);
    setInput("");
    setBusy(true);
    try {
      const answer = await api.post<CopilotAnswer>("/api/Copilot/PostAsk", {
        Question: question,
        PersonId: personId,
        History: history.slice(-8),
      });
      setMessages((m) => [...m, { role: "assistant", text: answer.text, citations: answer.citations }]);
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setNeedsSetup(true);
        setMessages((m) => [...m, { role: "assistant", text: "No AI provider is configured yet. Save your provider, model, and key below to start." }]);
      } else if (err instanceof ApiError && err.status === 503) {
        setMessages((m) => [...m, { role: "assistant", text: "The language model is unreachable right now. Check the provider settings and try again." }]);
      } else {
        setMessages((m) => [...m, { role: "assistant", text: err instanceof ApiError ? err.body || err.message : "Something went wrong." }]);
      }
    } finally {
      setBusy(false);
    }
  }

  if (!open) return null;

  return (
    <div className="fixed inset-y-0 right-0 z-50 flex w-full max-w-md flex-col border-l bg-background shadow-xl" role="dialog" aria-label="Relationship co-pilot">
      <div className="flex items-center justify-between border-b px-4 py-3">
        <div>
          <p className="text-sm font-semibold">Co-pilot</p>
          <p className="text-xs text-muted-foreground">
            {personName ? `About ${personName}` : "Across your network"} · grounded in your data
          </p>
        </div>
        <div className="flex gap-1.5">
          <Button size="sm" variant="ghost" onClick={() => setShowSettings((s) => !s)}>
            {showSettings ? "Hide setup" : "Setup"}
          </Button>
          <Button size="sm" variant="ghost" onClick={closeCopilot} aria-label="Close co-pilot">
            ✕
          </Button>
        </div>
      </div>

      {(showSettings || needsSetup) && (
        <div className="border-b px-4 py-3">
          <ProviderSettings onSaved={() => setNeedsSetup(false)} />
        </div>
      )}

      <div className="flex flex-1 flex-col gap-3 overflow-auto px-4 py-3">
        {messages.length === 0 && (
          <div className="text-xs text-muted-foreground">
            <p className="mb-1 font-semibold text-foreground">Try asking</p>
            <ul className="list-disc space-y-0.5 pl-4">
              <li>Who should I follow up with this week?</li>
              {personName ? <li>What happened with {personName}?</li> : <li>Who have I been neglecting?</li>}
              <li>What important dates are coming up?</li>
            </ul>
          </div>
        )}
        {messages.map((m, i) => (
          <div key={i} className={`flex flex-col gap-1 rounded-lg px-3 py-2 text-sm ${m.role === "user" ? "self-end bg-primary text-primary-foreground" : "self-start border bg-card"}`}>
            <p className="whitespace-pre-wrap">{m.text}</p>
            {(m.citations ?? []).length > 0 && (
              <div className="flex flex-wrap gap-1">
                {(m.citations ?? []).map((c, j) => (
                  c.id ? (
                    <Link key={j} to={`/people/${c.id}`} className="text-xs underline opacity-80">
                      {c.label}
                    </Link>
                  ) : (
                    <span key={j} className="text-xs opacity-70">{c.label}</span>
                  )
                ))}
              </div>
            )}
          </div>
        ))}
        {busy && <p className="text-xs text-muted-foreground">Thinking…</p>}
        <div ref={bottomRef} />
      </div>

      <form onSubmit={send} className="flex gap-2 border-t px-4 py-3">
        <Textarea
          aria-label="Ask the co-pilot"
          rows={1}
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) void send();
          }}
          placeholder={personName ? `Ask about ${personName}…` : "Ask about your relationships…"}
          className="flex-1"
        />
        <Button type="submit" size="sm" disabled={busy || input.trim() === ""}>
          Send
        </Button>
      </form>
    </div>
  );
}
