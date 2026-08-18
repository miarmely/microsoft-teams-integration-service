import {
  AlertTriangle,
  CheckCircle2,
  Database,
  HardDrive,
  LoaderCircle,
  ShieldAlert,
  Trash2,
} from "lucide-react";
import { useState, type FormEvent } from "react";
import { api, ApiResponseError } from "../api";
import { useAuth } from "../auth";
import { WorkspaceControls } from "../components/WorkspaceControls";
import { useDirectory } from "../hooks/useDirectory";
import type { MessageDeletionResult } from "../types";

const dateInputValue = (date: Date) => date.toISOString().slice(0, 10);

const defaultFromDate = () => {
  const date = new Date();
  date.setDate(date.getDate() - 30);
  return dateInputValue(date);
};

/** Permanently deletes synchronized messages after explicit user confirmation. */
export function DeleteMessagesPage() {
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
  const [fromDate, setFromDate] = useState(defaultFromDate());
  const [toDate, setToDate] = useState(dateInputValue(new Date()));
  const [acknowledged, setAcknowledged] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [result, setResult] = useState<MessageDeletionResult | null>(null);

  const invalidRange = Boolean(fromDate && toDate && fromDate > toDate);
  const selectedTeam = directory.find((item) => item.team?.id === teamId);
  const selectedChannel = selectedTeam?.channels.find(
    (channel) => channel.id === channelId,
  );

  /** Converts date-only inputs into inclusive local-day ISO boundaries. */
  const getDateRange = () => ({
    from: new Date(`${fromDate}T00:00:00`).toISOString(),
    to: new Date(`${toDate}T23:59:59.999`).toISOString(),
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!teamId || !channelId || !fromDate || !toDate || invalidRange) return;

    const teamName = selectedTeam?.team?.displayName || teamId;
    const channelName = selectedChannel?.displayName || channelId;
    const confirmed = window.confirm(
      `Permanently delete synchronized messages from ${teamName} / ${channelName} between ${fromDate} and ${toDate}? This cannot be undone.`,
    );
    if (!confirmed) return;

    setLoading(true);
    setError("");
    setResult(null);

    try {
      const range = getDateRange();
      const deletionResult = await api.deleteSynchronizedMessages(
        token!,
        teamId,
        channelId,
        range.from,
        range.to,
      );
      setResult(deletionResult);
      setAcknowledged(false);
    } catch (err) {
      // A 207 response contains the successful and failed deletion counts.
      if (err instanceof ApiResponseError) {
        setError(err.message);
        setResult((err.data as MessageDeletionResult | undefined) ?? null);
      } else {
        setError(
          err instanceof Error
            ? err.message
            : "Synchronized messages could not be deleted.",
        );
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page narrow">
      <div className="page-heading">
        <div>
          <span className="eyebrow danger-eyebrow">Data lifecycle management</span>
          <h1>Delete synchronized messages</h1>
          <p>Remove a selected channel's synchronized records and stored media.</p>
        </div>
        <div className="source-badge danger-badge"><Trash2 /> Permanent deletion</div>
      </div>

      {(directoryError || error) && (
        <div className="alert error"><AlertTriangle /> {directoryError || error}</div>
      )}

      {result && (
        <section className={`deletion-result ${result.failedMessageCount ? "partial" : "complete"}`}>
          <div>
            {result.failedMessageCount ? <AlertTriangle /> : <CheckCircle2 />}
            <span>
              <strong>{result.failedMessageCount ? "Deletion partially completed" : "Deletion completed"}</strong>
              <small>{result.deletedMessageCount} message records and {result.deletedMediaCount} media objects deleted.</small>
            </span>
          </div>
          <div className="deletion-stats">
            <span><strong>{result.matchedMessageCount}</strong>Matched</span>
            <span><strong>{result.deletedMessageCount}</strong>Messages deleted</span>
            <span><strong>{result.deletedMediaCount}</strong>Media deleted</span>
            <span><strong>{result.failedMessageCount}</strong>Retained</span>
          </div>
        </section>
      )}

      <div className="deletion-layout">
        <section className="card form-card deletion-form">
          <div className="section-title">
            <div>
              <h2>Deletion scope</h2>
              <p>Select exactly which stored channel history should be removed.</p>
            </div>
          </div>

          <form onSubmit={submit}>
            <WorkspaceControls
              directory={directory}
              teamId={teamId}
              channelId={channelId}
              onTeam={(id) => {
                setTeamId(id);
                setChannelId("");
                setAcknowledged(false);
                void loadChannels(id);
              }}
              onChannel={(id) => {
                setChannelId(id);
                setAcknowledged(false);
              }}
              disabled={directoryLoading || channelsLoading || loading}
            />

            <div className="date-grid">
              <label>
                <span>From date <small>Required</small></span>
                <input
                  required
                  type="date"
                  value={fromDate}
                  max={toDate || undefined}
                  disabled={loading}
                  onChange={(event) => {
                    setFromDate(event.target.value);
                    setAcknowledged(false);
                  }}
                />
              </label>
              <label>
                <span>To date <small>Required</small></span>
                <input
                  required
                  type="date"
                  value={toDate}
                  min={fromDate || undefined}
                  disabled={loading}
                  onChange={(event) => {
                    setToDate(event.target.value);
                    setAcknowledged(false);
                  }}
                />
              </label>
            </div>

            {invalidRange && (
              <div className="field-error">The From date cannot be later than the To date.</div>
            )}

            <label className="deletion-acknowledgement">
              <input
                type="checkbox"
                checked={acknowledged}
                disabled={loading}
                onChange={(event) => setAcknowledged(event.target.checked)}
              />
              <span>
                <strong>I understand this operation is permanent.</strong>
                PostgreSQL records and associated MinIO media cannot be recovered from this dashboard.
              </span>
            </label>

            <button
              className="danger-button"
              disabled={
                !teamId ||
                !channelId ||
                !fromDate ||
                !toDate ||
                invalidRange ||
                !acknowledged ||
                loading
              }
            >
              {loading ? <LoaderCircle className="spin" /> : <Trash2 />}
              {loading ? "Deleting messages..." : "Delete synchronized messages"}
            </button>
          </form>
        </section>

        <aside className="card deletion-warning">
          <ShieldAlert />
          <span className="eyebrow danger-eyebrow">Before you continue</span>
          <h2>Permanent operation</h2>
          <p>Only synchronized data is affected. Messages in Microsoft Teams are not deleted.</p>
          <div className="deletion-impact-list">
            <div><Database /><span><strong>PostgreSQL</strong><small>Matching message and media metadata</small></span></div>
            <div><HardDrive /><span><strong>MinIO</strong><small>Images attached to matching messages</small></span></div>
          </div>
        </aside>
      </div>
    </div>
  );
}
