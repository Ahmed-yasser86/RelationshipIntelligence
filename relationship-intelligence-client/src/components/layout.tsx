import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { useAuth } from "@/lib/auth";
import { cn } from "cn";
import {
  HeartPulse,
  LayoutDashboard,
  LogOut,
  Network,
  Newspaper,
  Users,
} from "lucide-react";

const NAV = [
  { to: "/", label: "Overview", icon: LayoutDashboard, end: true },
  { to: "/attention", label: "Attention", icon: HeartPulse, end: true },
  { to: "/people", label: "People", icon: Users, end: false },
  { to: "/network", label: "Network", icon: Network, end: false },
  { to: "/digest", label: "Digest", icon: Newspaper, end: false },
];

export function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-background text-foreground">
      <div className="mx-auto flex min-h-screen max-w-6xl">
        <aside className="flex w-56 shrink-0 flex-col border-r px-4 py-6">
          <Link to="/" className="mb-1 px-2 text-lg font-semibold tracking-tight">
            Relationship Intelligence
          </Link>
          <p className="mb-6 px-2 text-xs text-muted-foreground">
            Who is cooling, and what to do this week.
          </p>
          <nav className="flex flex-col gap-1">
            {NAV.map(({ to, label, icon: Icon, end }) => (
              <NavLink
                key={to}
                to={to}
                end={end}
                className={({ isActive }) =>
                  cn(
                    "flex items-center gap-2.5 rounded-md px-2.5 py-2 text-sm font-medium",
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
          </nav>
          <div className="mt-auto">
            <Separator className="my-4" />
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
        <main className="min-w-0 flex-1 px-8 py-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
