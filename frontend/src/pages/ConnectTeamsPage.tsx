import { CheckCircle2, Link2, LoaderCircle, LogOut, PlugZap } from "lucide-react";
import { useMemo } from "react";
import { Link, useLocation } from "react-router-dom";
import { useTeamsConnection } from "../teamsConnection";

/** Provides a dedicated place to connect and manage the Microsoft account. */
export function ConnectTeamsPage() {
  const { status, loading, error, connect, disconnect } = useTeamsConnection();
  const location = useLocation();
  const result = new URLSearchParams(location.search).get("microsoftGraph");
  const returnTo = useMemo(() => {
    const candidate = (location.state as { from?: string } | null)?.from;
    return candidate?.startsWith("/") ? candidate : "/teams";
  }, [location.state]);
  const connected = status?.isConnected === true;

  return (
    <div className="page narrow">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Microsoft Graph account</span>
          <h1>Connect to Microsoft Teams</h1>
          <p>Manage the work account used for Teams directory and message operations.</p>
        </div>
        <div className="source-badge live"><PlugZap /> Teams connection</div>
      </div>

      {result === "connected" && connected && (
        <div className="alert success"><CheckCircle2 /> Microsoft Teams connected successfully.</div>
      )}
      {result === "error" && (
        <div className="alert error">Microsoft Teams login could not be completed.</div>
      )}
      {error && <div className="alert error">{error}</div>}

      <section className="card form-card graph-connection connection-page-card">
        <div>
          <span className="eyebrow">Connection status</span>
          <h2>{loading ? "Checking connection" : connected ? "Connected" : "Not connected"}</h2>
          <p className="muted">
            {connected
              ? status.username || "A Microsoft work account is connected."
              : "Connect a work account that can access the required teams and channels."}
          </p>
        </div>
        {connected ? (
          <button type="button" onClick={() => void disconnect()} disabled={loading}>
            {loading ? <LoaderCircle className="spin" /> : <LogOut />} Disconnect
          </button>
        ) : (
          <button type="button" className="primary" onClick={() => void connect()} disabled={loading}>
            {loading ? <LoaderCircle className="spin" /> : <Link2 />} Connect Microsoft Teams
          </button>
        )}
      </section>

      {connected && (
        <Link className="secondary-button connection-return" to={returnTo}>
          Continue to Teams operations
        </Link>
      )}
    </div>
  );
}
