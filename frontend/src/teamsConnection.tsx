import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { api } from "./api";
import { useAuth } from "./auth";
import type { MicrosoftGraphOAuthStatus } from "./types";

type TeamsConnectionContextValue = {
  status: MicrosoftGraphOAuthStatus | null;
  loading: boolean;
  error: string;
  refresh: () => Promise<void>;
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
};

const TeamsConnectionContext =
  createContext<TeamsConnectionContextValue | null>(null);

/** Shares the current Microsoft Graph connection across dashboard pages. */
export function TeamsConnectionProvider({ children }: { children: ReactNode }) {
  const { token } = useAuth();
  const [status, setStatus] = useState<MicrosoftGraphOAuthStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const refresh = useCallback(async () => {
    if (!token) return;
    setLoading(true);
    setError("");
    try {
      setStatus(await api.microsoftGraphStatus(token));
    } catch (err) {
      setStatus(null);
      setError(
        err instanceof Error
          ? err.message
          : "Could not read the Microsoft Teams connection status.",
      );
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const connect = useCallback(async () => {
    if (!token) return;
    setLoading(true);
    setError("");
    try {
      const result = await api.microsoftGraphAuthorizationUrl(token);
      window.location.assign(result.authorizationUrl);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Microsoft login could not start.");
      setLoading(false);
    }
  }, [token]);

  const disconnect = useCallback(async () => {
    if (!token) return;
    setLoading(true);
    setError("");
    try {
      await api.disconnectMicrosoftGraph(token);
      setStatus({ isConnected: false });
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Microsoft Teams could not be disconnected.",
      );
    } finally {
      setLoading(false);
    }
  }, [token]);

  const value = useMemo(
    () => ({ status, loading, error, refresh, connect, disconnect }),
    [status, loading, error, refresh, connect, disconnect],
  );

  return (
    <TeamsConnectionContext.Provider value={value}>
      {children}
    </TeamsConnectionContext.Provider>
  );
}

export function useTeamsConnection() {
  const context = useContext(TeamsConnectionContext);
  if (!context) {
    throw new Error("useTeamsConnection must be used within TeamsConnectionProvider");
  }
  return context;
}
