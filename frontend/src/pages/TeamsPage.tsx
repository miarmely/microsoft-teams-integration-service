import { Search, Users } from "lucide-react";
import { useMemo, useState } from "react";
import { useAuth } from "../auth";
import { CopyableId } from "../components/CopyableId";
import { useDirectory } from "../hooks/useDirectory";

/** Displays every accessible Microsoft Teams team and its copyable Graph ID. */
export function TeamsPage() {
  const { token } = useAuth();
  const { directory, loading, error } = useDirectory(token!);
  const [search, setSearch] = useState("");

  const teams = useMemo(
    () =>
      directory
        .map((entry) => entry.team)
        .filter((team) => team !== undefined)
        .filter((team) =>
          `${team.displayName ?? ""} ${team.description ?? ""} ${team.id}`
            .toLowerCase()
            .includes(search.toLowerCase()),
        ),
    [directory, search],
  );

  return (
    <div className="page">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Microsoft Graph directory</span>
          <h1>Teams</h1>
          <p>View every accessible team and copy its Microsoft Graph identifier.</p>
        </div>
        <div className="source-badge live"><Users /> Live directory</div>
      </div>

      {error && <div className="alert error">{error}</div>}

      <section className="card directory-card">
        <div className="directory-toolbar">
          <span><strong>{teams.length}</strong> teams</span>
          <label className="search">
            <Search />
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search teams..." />
          </label>
        </div>

        <div className="table-scroll">
          <table className="directory-table">
            <thead><tr><th>Team</th><th>Description</th><th>Team ID</th></tr></thead>
            <tbody>
              {teams.map((team) => (
                <tr key={team.id}>
                  <td><strong>{team.displayName || "Unnamed team"}</strong></td>
                  <td className="description-cell">{team.description || "No description"}</td>
                  <td><CopyableId value={team.id} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {!loading && teams.length === 0 && (
          <div className="empty"><Users /><h3>No teams found</h3><p>Try another search term.</p></div>
        )}
        {loading && <div className="table-loading">Loading teams...</div>}
      </section>
    </div>
  );
}
