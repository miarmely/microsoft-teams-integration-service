import { CheckCircle2, Image, Link2, LoaderCircle, LogOut, Send } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { api } from "../api";
import { useAuth } from "../auth";
import { WorkspaceControls } from "../components/WorkspaceControls";
import { useDirectory } from "../hooks/useDirectory";
import type { MicrosoftGraphOAuthStatus } from "../types";

/** Connects Microsoft Graph and sends an Adaptive Card with hosted image bytes. */
export function SendPage() {
  const { token } = useAuth();
  const {
    directory,
    loading: dirLoading,
    channelsLoading,
    error: dirError,
    loadChannels,
  } = useDirectory(token!);
  const [graphStatus, setGraphStatus] = useState<MicrosoftGraphOAuthStatus | null>(null);
  const [graphLoading, setGraphLoading] = useState(true);
  const [teamId, setTeamId] = useState("");
  const [channelId, setChannelId] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [image, setImage] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  useEffect(() => {
    const result = new URLSearchParams(window.location.search).get("microsoftGraph");
    if (result === "connected") setSuccess("Microsoft Teams account connected.");
    if (result === "error") setError("Microsoft Teams login could not be completed.");

    api
      .microsoftGraphStatus(token!)
      .then(setGraphStatus)
      .catch((err) =>
        setError(err instanceof Error ? err.message : "Could not read Microsoft Graph status."),
      )
      .finally(() => setGraphLoading(false));
  }, [token]);

  const connectMicrosoft = async () => {
    setGraphLoading(true);
    setError("");
    try {
      const result = await api.microsoftGraphAuthorizationUrl(token!);
      window.location.assign(result.authorizationUrl);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Microsoft login could not start.");
      setGraphLoading(false);
    }
  };

  const disconnectMicrosoft = async () => {
    setGraphLoading(true);
    setError("");
    try {
      await api.disconnectMicrosoftGraph(token!);
      setGraphStatus({ isConnected: false });
      setSuccess("Microsoft Teams account disconnected.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Microsoft account could not be disconnected.");
    } finally {
      setGraphLoading(false);
    }
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!image) return;
    setLoading(true);
    setError("");
    setSuccess("");
    try {
      await api.sendHostedAdaptiveCard(
        token!, teamId, channelId, title, description, image,
      );
      setSuccess("Adaptive Card sent successfully through Microsoft Graph.");
      setTitle("");
      setDescription("");
      setImage(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Message could not be sent.");
    } finally {
      setLoading(false);
    }
  };

  const connected = graphStatus?.isConnected === true;

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

      {((connected && dirError) || error) && (
        <div className="alert error">{error || dirError}</div>
      )}
      {success && (
        <div className="alert success"><CheckCircle2 /> {success}</div>
      )}

      <section className="card form-card graph-connection">
        <div>
          <span className="eyebrow">Microsoft Teams account</span>
          <h2>{connected ? "Connected" : "Login required"}</h2>
          <p className="muted">
            {connected
              ? graphStatus.username || "A Microsoft work account is connected."
              : "Connect a work account that can post to the target channel."}
          </p>
        </div>
        {connected ? (
          <button type="button" onClick={disconnectMicrosoft} disabled={graphLoading}>
            <LogOut /> Disconnect
          </button>
        ) : (
          <button
            type="button"
            className="primary"
            onClick={connectMicrosoft}
            disabled={graphLoading}
          >
            {graphLoading ? <LoaderCircle className="spin" /> : <Link2 />}
            Connect Microsoft Teams
          </button>
        )}
      </section>

      {connected && (
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
              <span>Hosted image</span>
              <input
                required
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={(event) => setImage(event.target.files?.[0] ?? null)}
              />
            </label>
            <div className="send-actions">
              <span className="muted"><Image /> The image is embedded as Teams hosted content.</span>
              <button
                className="primary"
                disabled={!teamId || !channelId || !title.trim() || !image || loading}
              >
                {loading ? <LoaderCircle className="spin" /> : <Send />}
                {loading ? "Sending..." : "Send message"}
              </button>
            </div>
          </form>
        </section>
      )}
    </div>
  );
}
