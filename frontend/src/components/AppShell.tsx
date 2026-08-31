import {
  Database,
  Archive,
  Activity,
  LogOut,
  Rows3,
  Menu,
  Radio,
  RefreshCw,
  Send,
  PlugZap,
  Trash2,
  Users,
  X,
} from "lucide-react";
import { useState, type ReactNode } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "../auth";
import { Brand } from "./Brand";

const nav = [
  { to: "/connect-teams", label: "Connect to Teams", icon: PlugZap },
  { to: "/teams", label: "Teams", icon: Users },
  { to: "/channels", label: "Channels", icon: Rows3 },
  { to: "/messages/live", label: "Live messages", icon: Radio },
  { to: "/messages/stored", label: "Synchronized", icon: Database },
  { to: "/synchronize", label: "Synchronization", icon: RefreshCw },
  { to: "/export", label: "Export messages", icon: Archive },
  { to: "/messages/delete", label: "Delete messages", icon: Trash2 },
  { to: "/send", label: "Send message", icon: Send },
  { to: "/logs", label: "Application logs", icon: Activity },
];

/** Provides authenticated pages with responsive navigation and sign-out. */
export function AppShell({ children }: { children: ReactNode }) {
  const { signOut } = useAuth();
  const [open, setOpen] = useState(false);
  return (
    <div className="app-shell">
      <aside className={open ? "sidebar open" : "sidebar"}>
        <div className="sidebar-head">
          <Brand />
          <button
            className="icon-button mobile-only"
            onClick={() => setOpen(false)}
            aria-label="Close menu"
          >
            <X />
          </button>
        </div>
        <nav>
          {nav.map(({ to, label, icon: Icon }) => (
            <NavLink key={to} to={to} onClick={() => setOpen(false)}>
              <Icon size={19} />
              {label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-foot">
          <div className="status-line">
            <i /> API workspace
          </div>
          <button className="logout" onClick={signOut}>
            <LogOut size={18} /> Sign out
          </button>
        </div>
      </aside>
      <main>
        <header className="topbar">
          <button
            className="icon-button mobile-only"
            onClick={() => setOpen(true)}
            aria-label="Open menu"
          >
            <Menu />
          </button>
          <span className="topbar-title">Microsoft Teams operations</span>
          <span className="environment">Production-ready</span>
        </header>
        {children}
      </main>
    </div>
  );
}
