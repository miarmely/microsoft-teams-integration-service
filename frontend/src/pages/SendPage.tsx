import { CheckCircle2, Image, LoaderCircle, Send } from "lucide-react";
import { useState, type FormEvent } from "react";
import { api } from "../api";
import { useAuth } from "../auth";
import { WorkspaceControls } from "../components/WorkspaceControls";
import { useDirectory } from "../hooks/useDirectory";

/** Builds and sends an Adaptive Card message to a selected Teams channel. */
export function SendPage() {
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
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [imageUrl, setImageUrl] = useState("");
  const [imageAlt, setImageAlt] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  /** Converts form values into the message structure expected by the API. */
  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");
    setSuccess("");
    try {
      // Blank lines separate the paragraphs used by the Adaptive Card body.
      const lines = content
        .split(/\n\s*\n/)
        .map((x) => x.trim())
        .filter(Boolean);
      const result = await api.send(
        token!,
        teamId,
        channelId,
        title,
        lines,
        imageUrl || undefined,
        imageAlt || undefined,
      );
      setSuccess(
        `${result.messagesSendedSuccessfull} message sent successfully${result.messagesFailedWhenSending ? `; ${result.messagesFailedWhenSending} failed` : ""}.`,
      );

      // Keep the draft when delivery partially fails so the user can retry it.
      if (!result.messagesFailedWhenSending) {
        setTitle("");
        setContent("");
        setImageUrl("");
        setImageAlt("");
      }
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Message could not be sent.",
      );
    } finally {
      setLoading(false);
    }
  };
  return (
    <div className="page narrow">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Adaptive Card delivery</span>
          <h1>Send a channel message</h1>
          <p>
            Publish a structured notification through your configured Teams
            workflow.
          </p>
        </div>
        <div className="source-badge live">
          <Send /> Teams workflow
        </div>
      </div>
      {(dirError || error) && (
        <div className="alert error">{dirError || error}</div>
      )}
      {success && (
        <div className="alert success">
          <CheckCircle2 />
          {success}
        </div>
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
          <label>
            <span>
              Message title <small>Optional</small>
            </span>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={200}
              placeholder="Weekly operations update"
            />
          </label>
          <label>
            <span>Message content</span>
            <textarea
              required
              rows={7}
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder={
                "Write your message here.\n\nSeparate paragraphs with an empty line."
              }
            />
          </label>
          <details>
            <summary>
              <Image /> Add an image <small>Optional</small>
            </summary>
            <div className="date-grid">
              <label>
                <span>Public image URL</span>
                <input
                  type="url"
                  value={imageUrl}
                  onChange={(e) => setImageUrl(e.target.value)}
                  placeholder="https://…"
                />
              </label>
              <label>
                <span>Alternative text</span>
                <input
                  value={imageAlt}
                  onChange={(e) => setImageAlt(e.target.value)}
                  placeholder="Image description"
                />
              </label>
            </div>
          </details>
          <div className="send-actions">
            <span className="muted">
              This sends one Adaptive Card to the selected channel.
            </span>
            <button
              className="primary"
              disabled={!teamId || !channelId || !content.trim() || loading}
            >
              {loading ? <LoaderCircle className="spin" /> : <Send />}{" "}
              {loading ? "Sending…" : "Send message"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
