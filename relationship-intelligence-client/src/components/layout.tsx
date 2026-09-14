import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { CopilotDrawer } from "@/components/copilot-drawer";
import { GlobalLog } from "@/components/global-log";
import { Onboarding } from "@/components/onboarding";
import { useAuth } from "@/lib/auth";
import { useCopilot } from "@/lib/copilot";
import { cn } from "cn";
import {
  Building2,
  CalendarDays,
  HeartPulse,
  Inbox,
  LayoutDashboard,
  LogOut,
  Network,
  Newspaper,
  Send,
  Sparkles,
  Users,
} from "lucide-react";

const NAV_GROUPS = [
  {
    label: "Act",
    items: [
      { to: "/", label: "Today", icon: LayoutDashboard, end: true },
      { to: "/attention", label: "Attention", icon: HeartPulse, end: true },
      { to: "/outreach", label: "Outreach", icon: Send, end: false },
    ],
  },
  {
    label: "Review",
    items: [{ to: "/found", label: "Things I Found", icon: Inbox, end: true }],
  },
  {
    label: "Understand",
    items: [
      { to: "/people", label: "People", icon: Users, end: false },
      { to: "/meetings", label: "Meetings", icon: CalendarDays, end: false },
      { to: "/network", label: "Network", icon: Network, end: false },
    ],
  },
  {
    label: "System",
    items: [
      { to: "/digest", label: "Digest", icon: Newspaper, end: false },
      { to: "/organizations", label: "Organizations", icon: Building2, end: true },
    ],
  },
];

export function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { openCopilot } = useCopilot();

  return (
    <div className="min-h-screen bg-background text-foreground">
      <div className="mx-auto flex min-h-screen max-w-6xl">
        <aside className="flex w-60 shrink-0 flex-col border-r bg-card/60 px-4 py-6 max-md:hidden">
          <Link to="/" className="mb-1 px-2 font-display text-[22px] font-semibold tracking-tight">
            Relationship Intelligence
          </Link>
          <p className="mb-6 px-2 text-xs text-muted-foreground">
            Who is cooling, and what to do this week.
          </p>
          <div className="mb-4 px-2">
            <GlobalLog />
          </div>
          <nav className="flex flex-col gap-4">
            {NAV_GROUPS.map((group) => (
              <div key={group.label}>
                <p className="mb-1 px-2.5 text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">
                  {group.label}
                </p>
                <div className="flex flex-col gap-0.5">
                  {group.items.map(({ to, label, icon: Icon, end }) => (
                    <NavLink
                      key={to}
                      to={to}
                      end={end}
                      className={({ isActive }) =>
                        cn(
                          "flex items-center gap-2.5 rounded-md px-2.5 py-2 text-sm font-medium transition-colors duration-150",
                          isActive
                            ? "bg-secondary text-secondary-foreground"
                            : "text-muted-foreground hover:bg-secondary/60 hover:text-foreground",
                        )
                      }
                    >
                      <Icon className="h-4 w-4" />
                      {label}
                    </NavLink>
                  ))}
                </div>
              </div>
            ))}
          </nav>
          <div className="mt-auto">
            <Separator className="my-4" />
            <Button
              variant="outline"
              size="sm"
              className="mb-2 w-full justify-start"
              onClick={() => openCopilot()}
            >
              <Sparkles className="h-4 w-4" /> Ask co-pilot
            </Button>
            <p className="truncate px-2 text-xs text-muted-foreground">
              {user?.name || user?.email}
            </p>
            <Button
              variant="ghost"
              size="sm"
              className="mt-1 w-full justify-start text-muted-foreground"
              onClick={() => {
                logout();
                navigate("/login");
              }}
            >
              <LogOut className="h-4 w-4" /> Sign out
            </Button>
          </div>
        </aside>
        <main className="min-w-0 flex-1 px-4 py-6 sm:px-8">
          <div className="mb-4 flex items-center gap-2 overflow-x-auto border-b pb-3 md:hidden">
            <Link to="/" className="mr-1 shrink-0 font-display text-lg font-semibold tracking-tight">
              RI
            </Link>
            {NAV_GROUPS.flatMap((g) => g.items).map(({ to, label, end }) => (
              <NavLink
                key={to}
                to={to}
                end={end}
                className={({ isActive }) =>
                  cn(
                    "shrink-0 rounded-full border px-3 py-1 text-xs font-medium",
                    isActive
                      ? "border-primary bg-primary text-primary-foreground"
                      : "text-muted-foreground",
                  )
                }
              >
                {label}
              </NavLink>
            ))}
          </div>
          <Outlet />
        </main>
        <Onboarding />
        <CopilotDrawer />
      </div>
    </div>
  );
}
