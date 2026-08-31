import {
  Activity,
  AlertOctagon,
  AlertTriangle,
  Bug,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  Clock3,
  Copy,
  Info,
  LoaderCircle,
  RefreshCw,
  Search,
  Server,
  TerminalSquare,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { useAuth } from "../auth";
import type { ApplicationLog, PagedResponse } from "../types";

const levels = ["All", "Critical", "Error", "Warning", "Information", "Debug", "Trace"] as const;

const levelIcon = (level: string) => {
  switch (level.toLowerCase()) {
    case "critical": return AlertOctagon;
    case "error": return CircleAlert;
    case "warning": return AlertTriangle;
    case "debug": return Bug;
    case "trace": return TerminalSquare;
    default: return Info;
  }
};

const shortCategory = (category: string) => category.split(".").at(-1) ?? category;

const formatTime = (value: string) => new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "medium",
}).format(new Date(value));

const readableProperties = (value: string | null) => {
  if (!value) return null;
  try { return JSON.stringify(JSON.parse(value), null, 2); }
  catch { return value; }
};

/** Operational log explorer with severity lanes and expandable request context. */
export function LogsPage() {
  const { token } = useAuth();
  const [data, setData] = useState<PagedResponse<ApplicationLog> | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [level, setLevel] = useState<(typeof levels)[number]>("All");
  const [search, setSearch] = useState("");
  const [expanded, setExpanded] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const load = useCallback(async (nextPage: number, size = pageSize) => {
    setLoading(true);
    setError("");
    try {
      const result = await api.logs(token!, nextPage, size);
      setData(result);
      setPage(result.pageNumber);
      setExpanded(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Application logs could not be loaded.");
    } finally {
      setLoading(false);
    }
  }, [pageSize, token]);

  useEffect(() => { void load(1); }, [load]);

  const counts = useMemo(() => (data?.items ?? []).reduce<Record<string, number>>((result, log) => {
    result[log.level] = (result[log.level] ?? 0) + 1;
    return result;
  }, {}), [data]);

  const visible = useMemo(() => {
    const needle = search.trim().toLowerCase();
    return (data?.items ?? []).filter((log) => {
      if (level !== "All" && log.level.toLowerCase() !== level.toLowerCase()) return false;
      if (!needle) return true;
      return [log.message, log.category, log.traceId, log.requestPath, log.exceptionMessage]
        .some((value) => value?.toLowerCase().includes(needle));
    });
  }, [data, level, search]);

  const errorCount = (counts.Critical ?? 0) + (counts.Error ?? 0);

  return (
    <div className="page logs-page">
      <div className="page-heading logs-heading">
        <div>
          <span className="eyebrow">System observability</span>
          <h1>Application logs</h1>
          <p>Inspect service activity, follow request traces, and investigate failures.</p>
        </div>
        <button className="secondary-button" onClick={() => void load(page)} disabled={loading}>
          <RefreshCw className={loading ? "spin" : ""} /> Refresh
        </button>
      </div>

      {error && <div className="alert error">{error}</div>}

      <section className="log-overview">
        <article><span className="log-stat-icon blue"><Activity /></span><div><strong>{data?.totalCount.toLocaleString() ?? "—"}</strong><small>Total events</small></div></article>
        <article><span className="log-stat-icon red"><CircleAlert /></span><div><strong>{errorCount}</strong><small>Errors on this page</small></div></article>
        <article><span className="log-stat-icon amber"><AlertTriangle /></span><div><strong>{counts.Warning ?? 0}</strong><small>Warnings on this page</small></div></article>
        <article><span className="log-stat-icon violet"><Server /></span><div><strong>{new Set((data?.items ?? []).map((log) => log.machineName)).size}</strong><small>Active instances</small></div></article>
      </section>

      <section className="card log-console">
        <div className="log-toolbar">
          <div className="log-levels" role="tablist" aria-label="Log levels">
            {levels.map((item) => {
              const Icon = item === "All" ? Activity : levelIcon(item);
              return <button key={item} className={`level-filter ${item.toLowerCase()} ${level === item ? "active" : ""}`} onClick={() => setLevel(item)}><Icon />{item}<i>{item === "All" ? data?.items.length ?? 0 : counts[item] ?? 0}</i></button>;
            })}
          </div>
          <div className="log-search"><Search /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search message, path, trace…" /></div>
        </div>

        <div className="log-list" aria-busy={loading}>
          {loading && <div className="log-loading"><LoaderCircle className="spin" /> Loading telemetry…</div>}
          {!loading && visible.map((log) => {
            const Icon = levelIcon(log.level);
            const isOpen = expanded === log.id;
            const properties = readableProperties(log.propertiesJson);
            return (
              <article key={log.id} className={`log-entry ${log.level.toLowerCase()} ${isOpen ? "open" : ""}`}>
                <button className="log-summary" onClick={() => setExpanded(isOpen ? null : log.id)} aria-expanded={isOpen}>
                  <span className="log-level-icon"><Icon /></span>
                  <span className="log-time"><Clock3 />{formatTime(log.createdAt)}</span>
                  <span className="log-main"><span className="log-category">{shortCategory(log.category)}</span><strong>{log.message || log.exceptionMessage || "No message"}</strong><small>{log.httpMethod && <b>{log.httpMethod}</b>}{log.requestPath || log.category}</small></span>
                  <span className={`log-level-badge ${log.level.toLowerCase()}`}>{log.level}</span>
                  <ChevronDown className="log-chevron" />
                </button>
                {isOpen && (
                  <div className="log-detail">
                    <div className="log-detail-grid">
                      <div><small>Category</small><code>{log.category}</code></div>
                      <div><small>Runtime</small><code>{log.environment} · {log.machineName}</code></div>
                      <div><small>Trace ID</small><code>{log.traceId ?? "—"}</code></div>
                      <div><small>Span / Event</small><code>{log.spanId ?? "—"} · {log.eventName ?? log.eventId}</code></div>
                    </div>
                    {log.exceptionMessage && <div className="log-exception"><strong>{log.exceptionType ?? "Exception"}</strong><p>{log.exceptionMessage}</p>{log.stackTrace && <pre>{log.stackTrace}</pre>}</div>}
                    {properties && <div className="log-properties"><div><span>Structured properties</span><button title="Copy properties" onClick={() => void navigator.clipboard.writeText(properties)}><Copy /></button></div><pre>{properties}</pre></div>}
                  </div>
                )}
              </article>
            );
          })}
          {!loading && visible.length === 0 && <div className="empty log-empty"><Search /><h3>No matching events</h3><p>Try another level or search phrase on this page.</p></div>}
        </div>

        {data && data.totalPages > 0 && <footer className="log-pagination">
          <span>Showing page <strong>{data.pageNumber}</strong> of <strong>{data.totalPages}</strong></span>
          <div>
            <label>Rows <select value={pageSize} onChange={(event) => { const size = Number(event.target.value); setPageSize(size); void load(1, size); }}>{[25, 50, 100].map((size) => <option key={size}>{size}</option>)}</select></label>
            <button disabled={!data.hasPreviousPage || loading} onClick={() => void load(page - 1)} aria-label="Previous page"><ChevronLeft /></button>
            <span>{page}</span>
            <button disabled={!data.hasNextPage || loading} onClick={() => void load(page + 1)} aria-label="Next page"><ChevronRight /></button>
          </div>
        </footer>}
      </section>
    </div>
  );
}
