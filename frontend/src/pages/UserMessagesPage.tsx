import {
  AlertCircle,
  Check,
  CheckCircle2,
  ChevronDown,
  LoaderCircle,
  Mail,
  MessageSquareText,
  RefreshCw,
  Search,
  Send,
  UserRound,
  UsersRound,
  X,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import { api } from "../api";
import { useAuth } from "../auth";
import type { SendMultipleUserMessageResult, TeamsUser } from "../types";

const emailOf = (user: TeamsUser) => user.mail || user.userPrincipalName || "";
const nameOf = (user: TeamsUser) =>
  user.displayName || [user.givenName, user.surname].filter(Boolean).join(" ") || emailOf(user);
const initialsOf = (user: TeamsUser) =>
  nameOf(user).split(/\s+/).slice(0, 2).map((part) => part[0]).join("").toUpperCase();

export function UserMessagesPage() {
  const { token } = useAuth();
  const [users, setUsers] = useState<TeamsUser[]>([]);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [message, setMessage] = useState("");
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [loadingUsers, setLoadingUsers] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const [result, setResult] = useState<SendMultipleUserMessageResult | null>(null);
  const [singleSuccess, setSingleSuccess] = useState(false);
  const pickerRef = useRef<HTMLDivElement>(null);

  const loadUsers = async () => {
    setLoadingUsers(true);
    setError("");
    try {
      const response = await api.users(token!);
      setUsers(response.users.filter((user) => Boolean(emailOf(user))));
    } catch (err) {
      setError(err instanceof Error ? err.message : "The Teams directory could not be loaded.");
    } finally {
      setLoadingUsers(false);
    }
  };

  useEffect(() => { void loadUsers(); }, [token]);
  useEffect(() => {
    const close = (event: MouseEvent) => {
      if (!pickerRef.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  const selected = useMemo(
    () => selectedIds.map((id) => users.find((user) => user.id === id)).filter(Boolean) as TeamsUser[],
    [selectedIds, users],
  );
  const filtered = useMemo(() => {
    const value = query.trim().toLowerCase();
    return users.filter((user) =>
      !selectedIds.includes(user.id) &&
      (!value || `${nameOf(user)} ${emailOf(user)}`.toLowerCase().includes(value)),
    ).slice(0, 60);
  }, [users, selectedIds, query]);

  const toggle = (id: string) => {
    setSelectedIds((current) => current.includes(id) ? current.filter((item) => item !== id) : [...current, id]);
    setResult(null);
    setSingleSuccess(false);
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const emails = selected.map(emailOf);
    if (!emails.length || !message.trim()) return;
    setSending(true);
    setError("");
    setResult(null);
    setSingleSuccess(false);
    try {
      if (emails.length === 1) {
        await api.sendUserMessage(token!, emails[0], message.trim());
        setSingleSuccess(true);
      } else {
        setResult(await api.sendMultipleUserMessage(token!, emails, message.trim()));
      }
      setMessage("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "The message could not be sent.");
    } finally {
      setSending(false);
    }
  };

  const delivered = singleSuccess || result?.isAllDelivered;

  return (
    <div className="page people-message-page">
      <div className="people-message-hero">
        <div>
          <span className="eyebrow">Direct conversations</span>
          <h1>Message your people</h1>
          <p>Choose one person or a group. Each recipient receives a private Teams conversation from your connected account.</p>
        </div>
        <div className="delivery-badge"><span><Send /></span><div><strong>Microsoft Teams</strong><small>Secure direct delivery</small></div></div>
      </div>

      {error && <div className="alert error"><AlertCircle /> {error}</div>}
      {(singleSuccess || result) && (
        <div className={`delivery-result ${delivered ? "complete" : "partial"}`}>
          {delivered ? <CheckCircle2 /> : <AlertCircle />}
          <div>
            <strong>{delivered ? "Message delivered" : "Delivery partially completed"}</strong>
            <span>{singleSuccess ? `Sent privately to ${emailOf(selected[0])}.` : `${result!.deliveredCount} of ${result!.targetCount} messages delivered.`}</span>
            {result && result.failedEmails.length > 0 && <small>Not delivered: {result.failedEmails.join(", ")}</small>}
          </div>
        </div>
      )}

      <div className="people-message-layout">
        <form className="card composer-card" onSubmit={submit}>
          <div className="composer-title">
            <span><MessageSquareText /></span>
            <div><h2>New direct message</h2><p>Start a 1:1 or send separately to multiple people.</p></div>
          </div>

          <label className="field-label">Recipients <small>{selected.length ? `${selected.length} selected` : "Required"}</small></label>
          <div className={`recipient-picker ${open ? "open" : ""}`} ref={pickerRef}>
            <button type="button" className="recipient-trigger" onClick={() => setOpen(!open)} aria-expanded={open}>
              <div className="recipient-trigger-content">
                {selected.length === 0 ? <span className="recipient-placeholder"><UsersRound /> Search people by name or email</span> : selected.slice(0, 3).map((user) => (
                  <span className="recipient-chip" key={user.id}><i>{initialsOf(user)}</i>{emailOf(user)}<X onClick={(event) => { event.stopPropagation(); toggle(user.id); }} /></span>
                ))}
                {selected.length > 3 && <span className="recipient-overflow">+{selected.length - 3} more</span>}
              </div>
              <ChevronDown />
            </button>
            {open && (
              <div className="recipient-menu">
                <div className="recipient-search"><Search /><input autoFocus value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Type a name or email..." /></div>
                <div className="recipient-options">
                  {loadingUsers ? <div className="picker-state"><LoaderCircle className="spin" /> Loading your directory...</div> : filtered.length === 0 ? <div className="picker-state">No matching people found.</div> : filtered.map((user) => (
                    <button type="button" key={user.id} className="recipient-option" onClick={() => toggle(user.id)}>
                      <span className="person-avatar">{initialsOf(user)}</span>
                      <span><strong>{nameOf(user)}</strong><small><Mail /> {emailOf(user)}</small></span>
                      <i><Check /></i>
                    </button>
                  ))}
                </div>
                <div className="picker-footer"><span>{users.length} people available</span><button type="button" onClick={() => { setSelectedIds(users.map((user) => user.id)); setOpen(false); }}>Select all</button></div>
              </div>
            )}
          </div>

          <label className="field-label message-label">Message <small>{message.length}/4000</small></label>
          <div className="message-editor">
            <textarea value={message} onChange={(e) => setMessage(e.target.value)} maxLength={4000} rows={9} placeholder="Write a clear, thoughtful message..." />
            <div className="editor-note"><MessageSquareText /> Sent as a private Teams chat message</div>
          </div>
          <div className="composer-actions">
            <span>{selected.length === 1 ? <><UserRound /> 1:1 conversation</> : <><UsersRound /> {selected.length || "No"} recipients</>}</span>
            <button className="primary send-people-button" disabled={!selected.length || !message.trim() || sending || loadingUsers}>
              {sending ? <LoaderCircle className="spin" /> : <Send />}{sending ? "Sending..." : selected.length > 1 ? `Send to ${selected.length} people` : "Send message"}
            </button>
          </div>
        </form>

        <aside className="card recipients-panel">
          <div className="recipients-panel-head"><div><span className="eyebrow">Delivery list</span><h2>Recipients</h2></div><span>{selected.length}</span></div>
          <div className="recipients-list">
            {selected.length === 0 ? <div className="recipients-empty"><UsersRound /><strong>Nobody selected yet</strong><p>Use the people picker to build your delivery list.</p></div> : selected.map((user, index) => (
              <div className="selected-person" key={user.id}><span className={`person-avatar tone-${index % 4}`}>{initialsOf(user)}</span><div><strong>{nameOf(user)}</strong><small>{emailOf(user)}</small></div><button type="button" onClick={() => toggle(user.id)} aria-label={`Remove ${nameOf(user)}`}><X /></button></div>
            ))}
          </div>
          <div className="directory-status"><span><i /> Directory connected</span><button type="button" onClick={() => void loadUsers()} disabled={loadingUsers}><RefreshCw className={loadingUsers ? "spin" : ""} /> Refresh</button></div>
        </aside>
      </div>
    </div>
  );
}
