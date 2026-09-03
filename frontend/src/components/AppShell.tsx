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
  MessagesSquare,
  X,
  ContactRound,
  ChevronDown,
  LayoutDashboard,
  MessageCircleMore,
  RefreshCcwDot,
  Settings2,
} from "lucide-react";
import { useEffect, useState, type ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "../auth";
import { Brand } from "./Brand";

const navGroups = [
  {
    id: "teams", label: "Teams", icon: LayoutDashboard,
    items: [
      { to: "/connect-teams", label: "Connect account", icon: PlugZap },
      { to: "/users", label: "People", icon: ContactRound },
      { to: "/channels", label: "Channels", icon: Rows3 },
    ],
  },
  {
    id: "messaging", label: "Messaging", icon: MessageCircleMore,
    items: [
      { to: "/users/message", label: "Message people", icon: MessagesSquare },
      { to: "/send", label: "Send channel card", icon: Send },
      { to: "/messages/live", label: "Live messages", icon: Radio },
    ],
  },
  {
    id: "synchronization", label: "Synchronization", icon: RefreshCcwDot,
    items: [
      { to: "/messages/stored", label: "Synchronized messages", icon: Database },
      { to: "/synchronize", label: "Sync messages", icon: RefreshCw },
      { to: "/export", label: "Export messages", icon: Archive },
      { to: "/messages/delete", label: "Delete messages", icon: Trash2 },
    ],
  },
  {
    id: "system", label: "System", icon: Settings2,
    items: [{ to: "/logs", label: "Application logs", icon: Activity }],
  },
];

/** Provides authenticated pages with responsive navigation and sign-out. */
export function AppShell({ children }: { children: ReactNode }) {
  const { signOut } = useAuth();
  const [open, setOpen] = useState(false);
  const { pathname } = useLocation();
  const activeGroup = navGroups.find((group) =>
    group.items.some((item) => item.to === pathname),
  )?.id;
  const [expandedGroups, setExpandedGroups] = useState<string[]>(
    activeGroup ? [activeGroup] : ["teams"],
  );

  useEffect(() => {
    if (activeGroup) {
      setExpandedGroups((groups) =>
        groups.includes(activeGroup) ? groups : [...groups, activeGroup],
      );
    }
  }, [activeGroup]);

  const toggleGroup = (id: string) => {
    setExpandedGroups((groups) =>
      groups.includes(id) ? groups.filter((group) => group !== id) : [...groups, id],
    );
  };
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
        <nav className="sidebar-nav" aria-label="Main navigation">
          {navGroups.map(({ id, label, icon: GroupIcon, items }) => {
            const expanded = expandedGroups.includes(id);
            const containsActiveRoute = id === activeGroup;
            return (
              <div className={`nav-group ${expanded ? "expanded" : ""}`} key={id}>
                <button
                  type="button"
                  className={`nav-group-trigger ${containsActiveRoute ? "contains-active" : ""}`}
                  onClick={() => toggleGroup(id)}
                  aria-expanded={expanded}
                  aria-controls={`nav-group-${id}`}
                >
                  <GroupIcon />
                  <span>{label}</span>
                  {containsActiveRoute && <i />}
                  <ChevronDown className="nav-chevron" />
                </button>
                <div className="nav-group-items" id={`nav-group-${id}`}>
                  <div>
                    {items.map(({ to, label: itemLabel, icon: Icon }) => (
                      <NavLink key={to} to={to} onClick={() => setOpen(false)}>
                        <Icon />
                        <span>{itemLabel}</span>
                      </NavLink>
                    ))}
                  </div>
                </div>
              </div>
            );
          })}
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
