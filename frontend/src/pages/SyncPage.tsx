import { CheckCircle2, LoaderCircle, RefreshCw } from "lucide-react";
import { useState, type FormEvent } from "react";
import { api } from "../api";
import { useAuth } from "../auth";
import { WorkspaceControls } from "../components/WorkspaceControls";
import { useDirectory } from "../hooks/useDirectory";
import type { SyncResult } from "../types";

/** Provides a practical 30-day default synchronization window. */
const defaultDate = () => {
  const d = new Date();
  d.setDate(d.getDate() - 30);
  return d.toISOString().slice(0, 10);
};

/** Runs a channel synchronization and presents the resulting counters. */
export function SyncPage() {
  const { token } = useAuth();
  const {
    directory,
    loading: dirLoading,
    channelsLoading,
    error: dirError,
    loadChannels,
  } = useDirectory(token!);
  const [teamId, setTeamId] = useState("");
  const [channelId, setChannelId] = useState("");
  const [fromDate, setFromDate] = useState(defaultDate());
  const [toDate, setToDate] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [result, setResult] = useState<SyncResult | null>(null);

  /** Starts a new synchronization after clearing the previous result. */
  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");
    setResult(null);
    try {
      setResult(
        await api.sync(
          token!,
          teamId,
          channelId,
          new Date(fromDate).toISOString(),
          toDate ? new Date(toDate).toISOString() : undefined,
        ),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Synchronization failed.");
    } finally {
      setLoading(false);
    }
  };
  return (
    <div className="page narrow">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Controlled data pipeline</span>
          <h1>Synchronize a channel</h1>
          <p>Copy Teams messages and hosted media into PostgreSQL and MinIO.</p>
        </div>
        <div className="source-badge">
          <RefreshCw /> Manual job
        </div>
      </div>
      {(dirError || error) && (
        <div className="alert error">{dirError || error}</div>
      )}
      <section className="card form-card">
        <form onSubmit={submit}>
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
            disabled={dirLoading || channelsLoading || loading}
          />
          <div className="date-grid">
            <label>
              <span>From date</span>
              <input
                type="date"
                required
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
              />
            </label>
            <label>
              <span>
                To date <small>Optional</small>
              </span>
              <input
                type="date"
                min={fromDate}
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
              />
            </label>
          </div>
          <div className="info-box">
            <RefreshCw />
            <span>
              <strong>What happens next?</strong>Messages are fetched from
              Graph, changes are stored in PostgreSQL, and hosted media is
              uploaded to MinIO.
            </span>
          </div>
          <button
            className="primary"
            disabled={!teamId || !channelId || loading}
          >
            {loading ? <LoaderCircle className="spin" /> : <RefreshCw />}{" "}
            {loading ? "Synchronizing…" : "Start synchronization"}
          </button>
        </form>
      </section>
      {result && (
        <section className="success-panel">
          <div>
            <CheckCircle2 />
            <span>
              <strong>Synchronization complete</strong>
              <small>{new Date(result.synchronizedAt).toLocaleString()}</small>
            </span>
          </div>
          <div className="stats">
            <span>
              <strong>{result.receivedMessageCount}</strong>Received
            </span>
            <span>
              <strong>{result.insertedMessageCount}</strong>Inserted
            </span>
            <span>
              <strong>{result.updatedMessageCount}</strong>Updated
            </span>
            <span>
              <strong>{result.synchronizedMediaCount}</strong>Media
            </span>
            <span>
              <strong>{result.failedMessageCount}</strong>Failed
            </span>
          </div>
        </section>
      )}
    </div>
  );
}
