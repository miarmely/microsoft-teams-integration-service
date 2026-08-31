import { CheckCircle2, Image, LoaderCircle, Send } from "lucide-react";
import { useState, type FormEvent } from "react";
import { api } from "../api";
import { useAuth } from "../auth";
import { WorkspaceControls } from "../components/WorkspaceControls";
import { useDirectory } from "../hooks/useDirectory";

/** Sends an Adaptive Card with hosted image bytes through Microsoft Graph. */
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
  const [description, setDescription] = useState("");
  const [images, setImages] = useState<File[]>([]);
  const [fileInputKey, setFileInputKey] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (images.length === 0) return;
    setLoading(true);
    setError("");
    setSuccess("");
    try {
      await api.sendHostedAdaptiveCard(
        token!, teamId, channelId, title, description, images,
      );
      setSuccess("Adaptive Card sent successfully through Microsoft Graph.");
      setTitle("");
      setDescription("");
      setImages([]);
      setFileInputKey((key) => key + 1);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Message could not be sent.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page narrow">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Microsoft Graph delivery</span>
          <h1>Send a hosted-image card</h1>
          <p>
            Send an Adaptive Card as the connected Microsoft Teams user. Image
            bytes are stored as Teams hosted content.
          </p>
        </div>
        <div className="source-badge live"><Send /> Microsoft Graph</div>
      </div>

      {(dirError || error) && (
        <div className="alert error">{error || dirError}</div>
      )}
      {success && (
        <div className="alert success"><CheckCircle2 /> {success}</div>
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
              <span>Message title</span>
              <input
                required
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                maxLength={200}
                placeholder="Weekly operations update"
              />
            </label>
            <label>
              <span>Description <small>Optional</small></span>
              <textarea
                rows={6}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                placeholder="Write the card description."
              />
            </label>
            <label>
              <span>Hosted images</span>
              <input
                key={fileInputKey}
                required
                type="file"
                multiple
                accept="image/png,image/jpeg,image/webp"
                onChange={(event) => setImages(Array.from(event.target.files ?? []))}
              />
              {images.length > 0 && (
                <small>{images.length} image{images.length === 1 ? "" : "s"} selected</small>
              )}
            </label>
            <div className="send-actions">
              <span className="muted"><Image /> Images are embedded as Teams hosted content.</span>
              <button
                className="primary"
                disabled={!teamId || !channelId || !title.trim() || images.length === 0 || loading}
              >
                {loading ? <LoaderCircle className="spin" /> : <Send />}
                {loading ? "Sending..." : "Send message"}
              </button>
            </div>
          </form>
      </section>
    </div>
  );
}
