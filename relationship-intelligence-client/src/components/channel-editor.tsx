import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { ContactChannelRequest } from "@/lib/types";

const COMMON_CHANNELS = [
  "WhatsApp",
  "Phone",
  "Email",
  "LinkedIn",
  "Telegram",
  "SMS",
  "Slack",
  "In person",
];

export function ChannelEditor({
  idPrefix,
  channels,
  onChange,
}: {
  idPrefix: string;
  channels: ContactChannelRequest[];
  onChange: (channels: ContactChannelRequest[]) => void;
}) {
  function set(i: number, patch: Partial<ContactChannelRequest>) {
    onChange(channels.map((c, j) => (j === i ? { ...c, ...patch } : c)));
  }

  function remove(i: number) {
    onChange(channels.filter((_, j) => j !== i));
  }

  function add() {
    onChange([...channels, { name: "", value: null }]);
  }

  return (
    <div className="flex flex-col gap-2">
      <Label id={`${idPrefix}-label`}>Channels</Label>
      {channels.length === 0 && (
        <p className="text-xs text-muted-foreground">
          How do you reach this person? Add a channel with its number or handle.
        </p>
      )}
      {channels.map((c, i) => (
        <div key={i} className="flex items-center gap-1.5">
          <Input
            aria-label={`Channel ${i + 1} name`}
            list={`${idPrefix}-suggestions`}
            value={c.name}
            maxLength={100}
            onChange={(e) => set(i, { name: e.target.value })}
            placeholder="WhatsApp"
            className="w-32"
          />
          <Input
            aria-label={`Channel ${i + 1} number or handle`}
            value={c.value ?? ""}
            maxLength={200}
            onChange={(e) => set(i, { value: e.target.value })}
            placeholder="number or handle"
            className="flex-1"
          />
          <Button
            type="button"
            size="sm"
            variant="ghost"
            aria-label={`Remove channel ${i + 1}`}
            onClick={() => remove(i)}
          >
            ✕
          </Button>
        </div>
      ))}
      <datalist id={`${idPrefix}-suggestions`}>
        {COMMON_CHANNELS.map((c) => (
          <option key={c} value={c} />
        ))}
      </datalist>
      <div>
        <Button type="button" size="sm" variant="outline" onClick={add}>
          Add channel
        </Button>
      </div>
    </div>
  );
}
