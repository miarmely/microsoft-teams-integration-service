import {
  AlertCircle,
  Check,
  Copy,
  Grid2X2,
  List,
  LoaderCircle,
  Mail,
  MessageSquareText,
  RefreshCw,
  Search,
  ShieldCheck,
  UserRoundCheck,
  UsersRound,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import type { TeamsUser } from "../types";

const emailOf = (user: TeamsUser) => user.mail || user.userPrincipalName || "";
const nameOf = (user: TeamsUser) =>
  user.displayName || [user.givenName, user.surname].filter(Boolean).join(" ") || "Unnamed user";
const initialsOf = (user: TeamsUser) =>
  nameOf(user).split(/\s+/).slice(0, 2).map((part) => part[0]).join("").toUpperCase() || "?";

export function UsersPage() {
  const { token } = useAuth();
  const [users, setUsers] = useState<TeamsUser[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [view, setView] = useState<"grid" | "list">("grid");
  const [copied, setCopied] = useState("");
  const searchRef = useRef<HTMLInputElement>(null);

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await api.users(token!);
      setUsers([...response.users].sort((a, b) => nameOf(a).localeCompare(nameOf(b))));
    } catch (err) {
      setError(err instanceof Error ? err.message : "The Teams directory could not be loaded.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [token]);
  useEffect(() => {
    const focusSearch = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        searchRef.current?.focus();
      }
    };
    window.addEventListener("keydown", focusSearch);
    return () => window.removeEventListener("keydown", focusSearch);
  }, []);

  const filtered = useMemo(() => {
    const value = search.trim().toLowerCase();
    return users.filter((user) =>
      !value || `${nameOf(user)} ${emailOf(user)} ${user.givenName ?? ""} ${user.surname ?? ""} ${user.id}`.toLowerCase().includes(value),
    );
  }, [users, search]);

  const emailCount = users.filter((user) => Boolean(user.mail)).length;
  const principalCount = users.filter((user) => Boolean(user.userPrincipalName)).length;

  const copyEmail = async (email: string) => {
    await navigator.clipboard.writeText(email);
    setCopied(email);
    window.setTimeout(() => setCopied((current) => current === email ? "" : current), 1600);
  };

  return (
    <div className="page users-page">
      <div className="users-hero">
        <div className="users-hero-copy">
          <span className="eyebrow">Microsoft Entra directory</span>
          <h1>People, at a glance.</h1>
          <p>Explore the people available to your connected Teams account and find the right person without leaving your workspace.</p>
          <div className="users-hero-actions">
            <Link className="primary" to="/users/message"><MessageSquareText /> Message people</Link>
            <button className="users-refresh" type="button" onClick={() => void load()} disabled={loading}><RefreshCw className={loading ? "spin" : ""} /> Refresh directory</button>
          </div>
        </div>
        <div className="directory-orbit" aria-hidden="true">
          <div className="orbit-ring"><span className="orbit-person one">AK</span><span className="orbit-person two">SD</span><span className="orbit-person three">ME</span></div>
          <div className="orbit-core"><UsersRound /><strong>{users.length}</strong><small>people</small></div>
        </div>
      </div>

      {error && <div className="alert error"><AlertCircle /> {error}</div>}

      <div className="user-insights">
        <div className="card insight-card"><span className="blue"><UsersRound /></span><div><small>Directory size</small><strong>{users.length}</strong><p>Visible accounts</p></div></div>
        <div className="card insight-card"><span className="green"><Mail /></span><div><small>Mail enabled</small><strong>{emailCount}</strong><p>Primary email available</p></div></div>
        <div className="card insight-card"><span className="violet"><ShieldCheck /></span><div><small>Principal IDs</small><strong>{principalCount}</strong><p>Entra sign-in identities</p></div></div>
      </div>

      <section className="card people-directory">
        <div className="people-toolbar">
          <div><h2>All people</h2><span>{filtered.length === users.length ? `${users.length} directory members` : `${filtered.length} of ${users.length} people`}</span></div>
          <div className="people-toolbar-actions">
            <label className="people-search"><Search /><input ref={searchRef} value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search name, email or ID..." /><kbd>⌘ K</kbd></label>
            <div className="view-switcher"><button type="button" className={view === "grid" ? "active" : ""} onClick={() => setView("grid")} aria-label="Grid view"><Grid2X2 /></button><button type="button" className={view === "list" ? "active" : ""} onClick={() => setView("list")} aria-label="List view"><List /></button></div>
          </div>
        </div>

        {loading ? (
          <div className="people-loading"><LoaderCircle className="spin" /><strong>Bringing your directory together</strong><span>Fetching the latest people from Microsoft Graph...</span></div>
        ) : filtered.length === 0 ? (
          <div className="empty people-empty"><UsersRound /><h3>No people found</h3><p>Try a different name, email address, or user ID.</p></div>
        ) : (
          <div className={`people-collection ${view}`}>
            {filtered.map((user, index) => {
              const email = emailOf(user);
              return (
                <article className="person-card" key={user.id}>
                  <div className="person-card-top"><span className={`directory-avatar palette-${index % 6}`}>{initialsOf(user)}<i /></span><span className="account-chip"><UserRoundCheck /> Active</span></div>
                  <div className="person-identity"><h3>{nameOf(user)}</h3><span>{user.givenName && user.surname ? `${user.givenName} ${user.surname}` : "Microsoft Teams user"}</span></div>
                  <div className="person-details">
                    <div><Mail /><span><small>EMAIL</small><strong>{email || "Not available"}</strong></span>{email && <button type="button" onClick={() => void copyEmail(email)} aria-label="Copy email">{copied === email ? <Check /> : <Copy />}</button>}</div>
                    <div><ShieldCheck /><span><small>USER PRINCIPAL NAME</small><strong>{user.userPrincipalName || "Not available"}</strong></span></div>
                  </div>
                  <div className="person-card-foot"><span title={user.id}>ID · {user.id}</span><Link to="/users/message"><MessageSquareText /> Message</Link></div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
