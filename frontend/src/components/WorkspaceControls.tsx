import type { Channel, TeamWithChannels } from "../types";

export function WorkspaceControls({
  directory,
  teamId,
  channelId,
  onTeam,
  onChannel,
  disabled,
}: {
  directory: TeamWithChannels[];
  teamId: string;
  channelId: string;
  onTeam: (id: string) => void;
  onChannel: (id: string) => void;
  disabled?: boolean;
}) {
  const channels: Channel[] = directory
    .find((x) => x.team?.id === teamId)?.channels
    ?? [];

  return (
    <div className="workspace-controls">
      <label>
        <span>Team</span>
        <select
          value={teamId}
          disabled={disabled}
          onChange={(e) => onTeam(e.target.value)}
        >
          <option value="">Select a team</option>
          {directory.map(
            (x) =>
              x.team && (
                <option key={x.team.id} value={x.team.id}>
                  {x.team.displayName || x.team.id}
                </option>
              ),
          )}
        </select>
      </label>
      <label>
        <span>Channel</span>
        <select
          value={channelId}
          disabled={disabled || !teamId}
          onChange={(e) => onChannel(e.target.value)}
        >
          <option value="">Select a channel</option>
          {channels.map((x) => (
            <option key={x.id} value={x.id}>
              {x.displayName || x.id}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
