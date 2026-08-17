import { Rows3, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { useAuth } from "../auth";
import { CopyableId } from "../components/CopyableId";
import { useDirectory } from "../hooks/useDirectory";

/** Displays all channels for one selected team with copyable Graph IDs. */
export function ChannelsPage() {
  const { token } = useAuth();
  const { directory, loading, channelsLoading, error, loadChannels } = useDirectory(token!);
  const [teamId, setTeamId] = useState("");
  const [search, setSearch] = useState("");

  const selectedTeam = directory.find((entry) => entry.team?.id === teamId);
  const channels = useMemo(
    () =>
      (selectedTeam?.channels ?? []).filter((channel) =>
        `${channel.displayName ?? ""} ${channel.description ?? ""} ${channel.id}`
          .toLowerCase()
          .includes(search.toLowerCase()),
      ),
    [selectedTeam, search],
  );

  const selectTeam = (id: string) => {
    setTeamId(id);
    setSearch("");
    void loadChannels(id);
  };

  return (
    <div className="page">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Microsoft Graph directory</span>
          <h1>Channels</h1>
          <p>Select a team to view all of its channels and copy their identifiers.</p>
        </div>
        <div className="source-badge live"><Rows3 /> Live directory</div>
      </div>

      {error && <div className="alert error">{error}</div>}

      <section className="card directory-card">
        <div className="directory-toolbar channel-toolbar">
          <label>
            <span>Team</span>
            <select value={teamId} disabled={loading || channelsLoading} onChange={(event) => selectTeam(event.target.value)}>
              <option value="">Select a team</option>
              {directory.map((entry) => entry.team && (
                <option key={entry.team.id} value={entry.team.id}>{entry.team.displayName || entry.team.id}</option>
              ))}
            </select>
          </label>
          {teamId && (
            <>
              <span className="directory-count"><strong>{channels.length}</strong> channels</span>
              <label className="search">
                <Search />
                <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search channels..." />
              </label>
            </>
          )}
        </div>

        {teamId && !channelsLoading && (
          <div className="table-scroll">
            <table className="directory-table">
              <thead><tr><th>Channel</th><th>Type</th><th>Description</th><th>Channel ID</th></tr></thead>
              <tbody>
                {channels.map((channel) => (
                  <tr key={channel.id}>
                    <td><strong>{channel.displayName || "Unnamed channel"}</strong></td>
                    <td><span className="type-chip">{channel.membershipType || "Unknown"}</span></td>
                    <td className="description-cell">{channel.description || "No description"}</td>
                    <td><CopyableId value={channel.id} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {!teamId && <div className="empty"><Rows3 /><h3>Select a team</h3><p>Its channels will be loaded on demand.</p></div>}
        {channelsLoading && <div className="table-loading">Loading channels...</div>}
        {teamId && !channelsLoading && channels.length === 0 && (
          <div className="empty"><Rows3 /><h3>No channels found</h3><p>This team has no matching channels.</p></div>
        )}
      </section>
    </div>
  );
}
