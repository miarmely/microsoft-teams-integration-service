import { useCallback, useEffect, useState } from "react";
import { api } from "../api";
import type { Channel, Team, TeamWithChannels } from "../types";

// Module-level cache survives page navigation without persisting company data
// after a full browser refresh.
let cacheToken: string | null = null;
let teamsCache: Team[] | null = null;
const channelsCache = new Map<string, Channel[]>();
let teamsRequest: Promise<Team[]> | null = null;
const channelRequests = new Map<string, Promise<Channel[]>>();

/** Clears cached directory data when a different user token becomes active. */
function prepareCache(token: string) {
  if (cacheToken === token) return;

  cacheToken = token;
  teamsCache = null;
  channelsCache.clear();
  teamsRequest = null;
  channelRequests.clear();
}

/** Returns teams from memory or shares one in-flight API request. */
async function getTeams(token: string) {
  prepareCache(token);
  if (teamsCache) return teamsCache;

  teamsRequest ??= api.teams(token);

  try {
    const teams = await teamsRequest;

    // Do not populate a new user's cache with an older request's response.
    if (cacheToken === token) teamsCache = teams;
    return teams;
  } finally {
    teamsRequest = null;
  }
}

/** Returns cached channels or loads them for one selected team. */
async function getChannels(token: string, teamId: string) {
  prepareCache(token);
  const cached = channelsCache.get(teamId);
  if (cached) return cached;

  let request = channelRequests.get(teamId);
  if (!request) {
    request = api.channels(token, teamId).then((response) => response.channels);
    channelRequests.set(teamId, request);
  }

  try {
    const channels = await request;
    if (cacheToken === token) channelsCache.set(teamId, channels);
    return channels;
  } finally {
    channelRequests.delete(teamId);
  }
}

/** Combines cached teams and channels into the view model used by selectors. */
function createDirectory(teams: Team[]): TeamWithChannels[] {
  return teams.map((team) => ({
    team,
    channels: channelsCache.get(team.id) ?? [],
  }));
}

/**
 * Loads teams immediately and channels lazily after a team is selected.
 * Both results are cached and shared by every dashboard page.
 */
export function useDirectory(token: string) {
  const [directory, setDirectory] = useState<TeamWithChannels[]>([]);
  const [loading, setLoading] = useState(true);
  const [channelsLoading, setChannelsLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError("");

    getTeams(token)
      .then((teams) => {
        if (active) setDirectory(createDirectory(teams));
      })
      .catch((e) => {
        if (active) {
          setError(e instanceof Error ? e.message : "Could not load teams.");
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    // Ignore state updates if the page unmounts while Graph is responding.
    return () => {
      active = false;
    };
  }, [token]);

  /** Ensures the selected team's channels are available in the directory. */
  const loadChannels = useCallback(
    async (teamId: string) => {
      if (!teamId || channelsCache.has(teamId)) return;

      setChannelsLoading(true);
      setError("");

      try {
        await getChannels(token, teamId);
        setDirectory(createDirectory(teamsCache ?? []));
      } catch (e) {
        setError(
          e instanceof Error ? e.message : "Could not load channels.",
        );
      } finally {
        setChannelsLoading(false);
      }
    },
    [token],
  );

  return { directory, loading, channelsLoading, error, loadChannels };
}
