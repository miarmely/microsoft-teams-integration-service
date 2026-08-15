import {
  CheckCircle2,
  KeyRound,
  LoaderCircle,
  Pencil,
  Plus,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { api } from "../api";
import { useAuth } from "../auth";
import { WorkspaceControls } from "../components/WorkspaceControls";
import { useDirectory } from "../hooks/useDirectory";
import type { WebhookUrl } from "../types";

/** Manages the workflow webhook assigned to each Teams channel. */
export function WebhooksPage() {
  const { token } = useAuth();
  const {
    directory,
    loading: directoryLoading,
    channelsLoading,
    error: directoryError,
    loadChannels,
  } = useDirectory(token!);

  const [webhooks, setWebhooks] = useState<WebhookUrl[]>([]);
  const [teamId, setTeamId] = useState("");
  const [channelId, setChannelId] = useState("");
  const [url, setUrl] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  /** Reloads the table after mutations so it reflects database state. */
  const loadWebhooks = async () => {
    setLoading(true);
    setError("");

    try {
      setWebhooks(await api.webhooks(token!));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load webhooks.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadWebhooks();
  }, [token]);

  /** Clears the form and exits edit mode. */
  const resetForm = () => {
    setEditingId(null);
    setTeamId("");
    setChannelId("");
    setUrl("");
  };

  /** Creates a new assignment or saves changes to the selected assignment. */
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    setSuccess("");

    try {
      if (editingId) {
        await api.updateWebhook(token!, editingId, teamId, channelId, url);
        setSuccess("Webhook URL updated successfully.");
      } else {
        await api.createWebhook(token!, teamId, channelId, url);
        setSuccess("Webhook URL created successfully.");
      }

      resetForm();
      await loadWebhooks();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Webhook could not be saved.");
    } finally {
      setSaving(false);
    }
  };

  /** Loads a table row into the form for editing. */
  const edit = (webhook: WebhookUrl) => {
    setEditingId(webhook.id);
    setTeamId(webhook.teamId);
    setChannelId(webhook.channelId);
    setUrl(webhook.url);
    setError("");
    setSuccess("");
    void loadChannels(webhook.teamId);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  /** Confirms and permanently deletes one webhook assignment. */
  const remove = async (webhook: WebhookUrl) => {
    if (!window.confirm("Delete this channel webhook URL?")) return;

    setError("");
    setSuccess("");
    try {
      await api.deleteWebhook(token!, webhook.id);
      if (editingId === webhook.id) resetForm();
      setSuccess("Webhook URL deleted successfully.");
      await loadWebhooks();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Webhook could not be deleted.");
    }
  };

  const teamName = (id: string) =>
    directory.find((item) => item.team?.id === id)?.team?.displayName || id;

  return (
    <div className="page narrow">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Teams workflow routing</span>
          <h1>Webhook URLs</h1>
          <p>Assign one Microsoft Teams workflow webhook to each channel.</p>
        </div>
        <div className="source-badge live">
          <KeyRound /> Database managed
        </div>
      </div>

      {(directoryError || error) && (
        <div className="alert error">{directoryError || error}</div>
      )}
      {success && (
        <div className="alert success">
          <CheckCircle2 /> {success}
        </div>
      )}

      <section className="card form-card webhook-form">
        <div className="section-title">
          <div>
            <h2>{editingId ? "Update webhook" : "Add webhook"}</h2>
            <p>Select a channel and paste its Teams Workflows HTTPS URL.</p>
          </div>
          {editingId && (
            <button className="secondary-button" type="button" onClick={resetForm}>
              <X /> Cancel editing
            </button>
          )}
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
            disabled={directoryLoading || channelsLoading || saving}
          />
          <label>
            <span>Teams Workflows webhook URL</span>
            <input
              type="url"
              value={url}
              onChange={(event) => setUrl(event.target.value)}
              placeholder="https://...environment.api.powerplatform.com/..."
              required
            />
          </label>
          <button
            className="primary"
            disabled={!teamId || !channelId || !url || saving}
          >
            {saving ? <LoaderCircle className="spin" /> : editingId ? <Pencil /> : <Plus />}
            {saving ? "Saving..." : editingId ? "Save changes" : "Add webhook"}
          </button>
        </form>
      </section>

      <section className="webhook-list-section">
        <div className="section-title">
          <div>
            <h2>Configured channels</h2>
            <p>{webhooks.length} webhook assignment{webhooks.length === 1 ? "" : "s"}</p>
          </div>
        </div>

        {loading ? (
          <div className="empty quiet"><LoaderCircle className="spin" /></div>
        ) : webhooks.length === 0 ? (
          <div className="empty quiet">
            <KeyRound />
            <h3>No webhooks configured</h3>
            <p>Add a channel workflow URL using the form above.</p>
          </div>
        ) : (
          <div className="webhook-list">
            {webhooks.map((webhook) => (
              <article className="webhook-card" key={webhook.id}>
                <div className="webhook-icon"><KeyRound /></div>
                <div className="webhook-details">
                  <strong>{teamName(webhook.teamId)}</strong>
                  <span className="webhook-channel">Channel: {webhook.channelId}</span>
                  <code>{webhook.url}</code>
                  <small>Updated {new Date(webhook.updatedAt).toLocaleString()}</small>
                </div>
                <div className="webhook-actions">
                  <button onClick={() => edit(webhook)} aria-label="Edit webhook"><Pencil /></button>
                  <button className="danger" onClick={() => void remove(webhook)} aria-label="Delete webhook"><Trash2 /></button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
