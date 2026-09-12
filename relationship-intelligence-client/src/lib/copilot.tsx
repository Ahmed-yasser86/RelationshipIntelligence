import { createContext, useCallback, useContext, useMemo, useState } from "react";
import type { ReactNode } from "react";

export interface CopilotContextValue {
  personId: string | null;
  personName: string | null;
}

interface CopilotState extends CopilotContextValue {
  open: boolean;
  openCopilot: (ctx?: CopilotContextValue) => void;
  closeCopilot: () => void;
}

const Ctx = createContext<CopilotState | null>(null);

export function CopilotProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const [ctx, setCtx] = useState<CopilotContextValue>({ personId: null, personName: null });

  const openCopilot = useCallback((next?: CopilotContextValue) => {
    if (next) setCtx(next);
    setOpen(true);
  }, []);
  const closeCopilot = useCallback(() => setOpen(false), []);

  const value = useMemo(
    () => ({ ...ctx, open, openCopilot, closeCopilot }),
    [ctx, open, openCopilot, closeCopilot],
  );
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useCopilot(): CopilotState {
  const state = useContext(Ctx);
  if (!state) throw new Error("useCopilot must be used inside CopilotProvider.");
  return state;
}
