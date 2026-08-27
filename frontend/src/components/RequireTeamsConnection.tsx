import { AlertTriangle, Link2, LoaderCircle } from "lucide-react";
import { Link, Outlet, useLocation } from "react-router-dom";
import { useTeamsConnection } from "../teamsConnection";

/** Prevents Graph-dependent pages from loading until Teams is connected. */
export function RequireTeamsConnection() {
  const { status, loading, error } = useTeamsConnection();
  const location = useLocation();

  if (loading) {
    return (
      <div className="page connection-gate-loading">
        <LoaderCircle className="spin" /> Checking Microsoft Teams connection...
      </div>
    );
  }

  if (status?.isConnected) return <Outlet />;

  return (
    <div className="page narrow">
      <section className="card connection-required">
        <div className="connection-required-icon">
          {error ? <AlertTriangle /> : <Link2 />}
        </div>
        <span className="eyebrow">Microsoft Teams connection required</span>
        <h1>Connect Teams to use this page</h1>
        <p>
          This page needs access to your Microsoft Teams directory. Connect a
          Microsoft work account, then return here to continue.
        </p>
        {error && <div className="alert error">{error}</div>}
        <Link
          className="primary connection-link"
          to="/connect-teams"
          state={{ from: `${location.pathname}${location.search}` }}
        >
          <Link2 /> Connect to Teams
        </Link>
      </section>
    </div>
  );
}
