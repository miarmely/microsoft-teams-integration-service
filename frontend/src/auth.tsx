import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";

const TOKEN_KEY = "teams-integration.access-token";
type AuthContextValue = {
  token: string | null;
  signIn: (token: string) => void;
  signOut: () => void;
};
const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Owns the current authentication token and persists it across browser restarts.
 * Descendants access this state through the useAuth hook.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  // The initializer runs once and restores a previous authenticated session.
  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem(TOKEN_KEY),
  );
  const value = useMemo(
    () => ({
      token,
      signIn: (next: string) => {
        // Keep storage and React state synchronized.
        localStorage.setItem(TOKEN_KEY, next);
        setToken(next);
      },
      signOut: () => {
        localStorage.removeItem(TOKEN_KEY);
        setToken(null);
      },
    }),
    [token],
  );
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/** Returns authentication state and actions from the nearest AuthProvider. */
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
