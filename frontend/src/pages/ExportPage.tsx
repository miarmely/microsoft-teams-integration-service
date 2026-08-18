import {
  Archive,
  CalendarRange,
  CheckCircle2,
  Database,
  Download,
  FileJson,
  Images,
  LoaderCircle,
  ShieldCheck,
} from "lucide-react";
import { useState, type FormEvent } from "react";
import { api } from "../api";
import { useAuth } from "../auth";
import { WorkspaceControls } from "../components/WorkspaceControls";
import { useDirectory } from "../hooks/useDirectory";

const dateInputValue = (date: Date) => date.toISOString().slice(0, 10);

const defaultFromDate = () => {
  const date = new Date();
  date.setDate(date.getDate() - 30);
  return dateInputValue(date);
};

/** Downloads a blob using its API-provided filename and releases it afterwards. */
function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/** Creates downloadable reports from synchronized channel messages and images. */
export function ExportPage() {
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
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [downloadedFile, setDownloadedFile] = useState("");

  const invalidRange = Boolean(fromDate && toDate && fromDate > toDate);

  /** Requests the archive and starts a browser download after it is complete. */
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (invalidRange) return;

    setLoading(true);
    setError("");
    setDownloadedFile("");

    try {
      const from = fromDate
        ? new Date(`${fromDate}T00:00:00`).toISOString()
        : undefined;
      const to = toDate
        ? new Date(`${toDate}T23:59:59.999`).toISOString()
        : undefined;
      const result = await api.exportMessages(
        token!,
        teamId,
        channelId,
        from,
        to,
      );
      const fileName = result.fileName || "teams-message-export.zip";

      saveBlob(result.blob, fileName);
      setDownloadedFile(fileName);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Export could not be created.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page narrow">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Reporting and data portability</span>
          <h1>Export synchronized messages</h1>
          <p>Download a channel report with its message dataset and stored images.</p>
        </div>
        <div className="source-badge"><Archive /> ZIP archive</div>
      </div>

      {(directoryError || error) && (
        <div className="alert error">{directoryError || error}</div>
      )}
      {downloadedFile && (
        <div className="alert success">
          <CheckCircle2 /> Download started: {downloadedFile}
        </div>
      )}

      <div className="export-layout">
        <section className="card form-card export-form">
          <div className="section-title">
            <div>
              <h2>Export configuration</h2>
              <p>Choose the synchronized channel and reporting period.</p>
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
                void loadChannels(id);
              }}
              onChannel={setChannelId}
              disabled={directoryLoading || channelsLoading || loading}
            />

            <div className="date-grid">
              <label>
                <span>From date <small>Optional</small></span>
                <input
                  type="date"
                  value={fromDate}
                  max={toDate || undefined}
                  disabled={loading}
                  onChange={(event) => setFromDate(event.target.value)}
                />
              </label>
              <label>
                <span>To date <small>Optional</small></span>
                <input
                  type="date"
                  value={toDate}
                  min={fromDate || undefined}
                  disabled={loading}
                  onChange={(event) => setToDate(event.target.value)}
                />
              </label>
            </div>

            {invalidRange && (
              <div className="field-error">The From date cannot be later than the To date.</div>
            )}

            <div className="info-box">
              <CalendarRange />
              <span>
                <strong>Inclusive date range</strong>
                Messages created on both selected boundary dates are included.
                Clear both fields to export the complete synchronized history.
              </span>
            </div>

            <button
              className="primary export-button"
              disabled={!teamId || !channelId || invalidRange || loading}
            >
              {loading ? <LoaderCircle className="spin" /> : <Download />}
              {loading ? "Preparing archive..." : "Download ZIP export"}
            </button>
          </form>
        </section>

        <aside className="card export-summary">
          <span className="eyebrow">Archive contents</span>
          <h2>Portable by design</h2>
          <p>The report is generated from your synchronized PostgreSQL and MinIO data.</p>
          <div className="export-content-list">
            <div><FileJson /><span><strong>dataset.json</strong><small>Message time, sender, text, and image paths</small></span></div>
            <div><Images /><span><strong>images/</strong><small>Original synchronized message images</small></span></div>
            <div><Database /><span><strong>Stored records</strong><small>No live Teams messages are added implicitly</small></span></div>
            <div><ShieldCheck /><span><strong>Protected export</strong><small>Uses your authenticated dashboard session</small></span></div>
          </div>
        </aside>
      </div>
    </div>
  );
}
