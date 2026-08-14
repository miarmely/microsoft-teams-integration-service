import {
  ChevronLeft,
  ChevronRight,
  Database,
  ExternalLink,
  Inbox,
  LoaderCircle,
  Radio,
  Search,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useAuth } from "../auth";
import { api } from "../api";
import { WorkspaceControls } from "../components/WorkspaceControls";
import {
  MessageMedia,
  type MessageMediaSource,
} from "../components/MessageMedia";
import { useDirectory } from "../hooks/useDirectory";
import type { GraphMessage, StoredMessage } from "../types";
import { sanitizeHtml } from "../sanitize";

/** Returns an ISO timestamp a specified number of days before today. */
const daysAgo = (days: number) => {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString();
};

/** Formats API timestamps with the user's browser locale. */
const formatDate = (value?: string) =>
  value ?
    new Intl.DateTimeFormat(
      undefined,
      {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(value))
    : "Unknown date";

/** Displays either "live Graph messages" or "messages stored in PostgreSQL". */
export function MessagesPage({ mode }: { mode: "live" | "stored" }) {
  const { token } = useAuth();
  const {
    directory,
    loading: directoryLoading,
    channelsLoading,
    error: directoryError,
    loadChannels,
  } = useDirectory(token!);
  const [teamId, setTeamId] = useState("");
  const [channelId, setChannelId] = useState("");
  const [fromDate, setFromDate] = useState(daysAgo(30).slice(0, 10));
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [items, setItems] = useState<(GraphMessage | StoredMessage)[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [loaded, setLoaded] = useState(false);
  const messagesTopRef = useRef<HTMLDivElement>(null);

  // A source or workspace change invalidates messages from the prior selection.
  useEffect(() => {
    setItems([]);
    setLoaded(false);
    setPageNumber(1);
  }, [mode, teamId, channelId, fromDate, pageSize]);

  /** Fetches from the endpoint associated with the current page mode. */
  const fetchMessages = async (targetPage = 1, scrollToMessages = false) => {
    if (!teamId || !channelId) return;

    setLoading(true);
    setError("");

    try {
      const data = mode === "live" ?
        await api.liveMessages(
          token!,
          teamId,
          channelId,
          new Date(fromDate).toISOString(),
          undefined,
          targetPage,
          pageSize,
        )
        : await api.storedMessages(
          token!,
          teamId,
          channelId,
          targetPage,
          pageSize,
        );

      // Keep the current page visible if Next reaches beyond the final page.
      if (targetPage > 1 && data.length === 0) return;

      setItems(data ?? []);
      setPageNumber(targetPage);
      setLoaded(true);

      // Move pagination users back to the beginning of the new result page.
      if (scrollToMessages) {
        requestAnimationFrame(() => {
          messagesTopRef.current?.scrollIntoView({
            behavior: "smooth",
            block: "start",
          });
        });
      }
    }
    catch (e) {
      setError(e instanceof Error ?
        e.message
        : "Could not retrieve messages.");
    }
    finally {
      setLoading(false);
    }
  };

  // Filtering is local so typing in the search field does not trigger API calls.
  const filtered = useMemo(
    () =>
      items.filter((item) => {
        const stored = item as StoredMessage;
        const graph = item as GraphMessage;

        return `${stored.subject || graph.subject || ""} ${stored.senderDisplayName || graph.from?.user?.displayName || ""} ${stored.htmlContent || graph.body?.content || ""}`
          .toLowerCase()
          .includes(search.toLowerCase());
      }),
    [items, search],
  );
  const live = mode === "live";

  return (
    <div className="page">
      <div className="page-heading">
        <div>
          <span className="eyebrow">
            {live ? "Microsoft Graph" : "PostgreSQL archive"}
          </span>
          <h1>{live ? "Live Teams messages" : "Synchronized messages"}</h1>
          <p>
            {live
              ? "Fetch the latest channel activity directly from Microsoft Teams."
              : "Review messages safely stored by your synchronization jobs."}
          </p>
        </div>
        <div className={`source-badge ${live ? "live" : ""}`}>
          {live ? <Radio /> : <Database />}{" "}
          {live ? "Live source" : "Stored source"}
        </div>
      </div>
      {(directoryError || error) && (
        <div className="alert error">{directoryError || error}</div>
      )}
      <section className="card filters">
        <WorkspaceControls
          directory={directory}
          teamId={teamId}
          channelId={channelId}
          onTeam={(id) => {
            setTeamId(id);
            setChannelId("");
            void loadChannels(id);
          }}
          onChannel={setChannelId}
          disabled={directoryLoading || channelsLoading}
        />
        {live && (
          <label>
            <span>From date</span>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
            />
          </label>
        )}
        <label className="page-size-field">
          <span>Page size</span>
          <select
            value={pageSize}
            disabled={loading}
            onChange={(e) => setPageSize(Number(e.target.value))}
          >
            {[10, 25, 50, 100].map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>
        <button
          className="primary"
          disabled={!teamId || !channelId || loading}
          onClick={() => void fetchMessages(1)}
        >
          {loading ? (
            <LoaderCircle className="spin" />
          ) : live ? (
            <Radio />
          ) : (
            <Database />
          )}{" "}
          {loading ? "Fetching…" : "Fetch messages"}
        </button>
      </section>
      <div ref={messagesTopRef} className="messages-top-anchor" />
      {loaded && (
        <div className="results-toolbar">
          <span>
            <strong>{filtered.length}</strong> messages
          </span>
          <label className="search">
            <Search />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search messages…"
            />
          </label>
        </div>
      )}
      <div className="message-list">
        {filtered.map((item, index) => {
          // Both response types are normalized here for a shared message layout.
          const stored = item as StoredMessage;
          const graph = item as GraphMessage;
          const content = stored.htmlContent || graph.body?.content;
          const sender =
            stored.senderDisplayName ||
            graph.from?.user?.displayName ||
            graph.from?.application?.displayName ||
            "Unknown sender";
          const date = stored.messageCreatedAt || graph.createdDateTime;
          const webUrl = stored.webUrl || graph.webUrl;
          const mediaSources: MessageMediaSource[] = live ?
            (graph.hostedContents ?? [])
              .filter(
                (media): media is typeof media & { id: string } => !!media.id,
              )
              .map((media) => ({
                id: `live-${graph.id}-${media.id}`,
                contentType: media.contentType,
                download: () =>
                  api.liveMedia(
                    token!,
                    teamId,
                    channelId,
                    graph.id!,
                    media.id,
                  ),
              }))
            : (stored.media ?? []).map((media) => ({
              id: `stored-${media.id}`,
              contentType: media.contentType,
              fileName: media.objectName.split("/").pop(),
              download: () => api.storedMedia(token!, media.id),
            }));

          return (
            <article
              className="message-card"
              key={stored.id || graph.id || index}
            >
              <div className="avatar">{sender.slice(0, 2).toUpperCase()}</div>
              <div className="message-body">
                <div className="message-meta">
                  <strong>{sender}</strong>
                  <span>{formatDate(date)}</span>
                  {webUrl && (
                    <a
                      href={webUrl}
                      target="_blank"
                      rel="noreferrer"
                      aria-label="Open in Teams"
                    >
                      <ExternalLink />
                    </a>
                  )}
                </div>
                {(stored.subject || graph.subject) && (
                  <h3>{stored.subject || graph.subject}</h3>
                )}
                <div
                  className="html-content"
                  dangerouslySetInnerHTML={{ __html: sanitizeHtml(content) }}
                />
                <MessageMedia sources={mediaSources} />
                {stored.media?.length > 0 && (
                  <span className="media-chip">
                    {stored.media.length} stored attachment
                    {stored.media.length === 1 ? "" : "s"}
                  </span>
                )}
              </div>
            </article>
          );
        })}
      </div>
      {loaded && !loading && filtered.length === 0 && (
        <div className="empty">
          <Inbox />
          <h3>No messages found</h3>
          <p>Try another channel, date range, or search term.</p>
        </div>
      )}
      {loaded && items.length > 0 && (
        <nav className="pagination" aria-label="Message pages">
          <button
            disabled={pageNumber === 1 || loading}
            onClick={() => void fetchMessages(pageNumber - 1, true)}
          >
            <ChevronLeft /> Previous
          </button>
          <span>
            Page <strong>{pageNumber}</strong>
          </span>
          <button
            disabled={items.length < pageSize || loading}
            onClick={() => void fetchMessages(pageNumber + 1, true)}
          >
            Next <ChevronRight />
          </button>
        </nav>
      )}
      {!loaded && (
        <div className="empty quiet">
          <Inbox />
          <h3>Select a team and channel</h3>
          <p>Your messages will appear here after you fetch them.</p>
        </div>
      )}
    </div>
  );
}
