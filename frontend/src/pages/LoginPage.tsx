import {
  ArrowRight,
  Eye,
  EyeOff,
  LockKeyhole,
  ShieldCheck,
} from "lucide-react";
import { useState, type FormEvent } from "react";
import { Navigate } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import { Brand } from "../components/Brand";

/** Authenticates a user through AccessHub and starts a persisted session. */
export function LoginPage() {
  const { token, signIn } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Authenticated users should never return to the login form.
  if (token) return <Navigate to="/messages/live" replace />;

  /** Submits credentials and stores the JWT returned by the API. */
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    setLoading(true);
    try {
      const data = await api.login(username, password);
      signIn(data.accessToken);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Sign in failed.");
    } finally {
      setLoading(false);
    }
  };
  return (
    <div className="login-page">
      <section className="login-story">
        <Brand />
        <div className="story-content">
          <span className="eyebrow">Secure operations workspace</span>
          <h1>
            Your Teams data,
            <br />
            under control.
          </h1>
          <p>
            Synchronize, inspect, and send channel messages from one protected
            enterprise workspace.
          </p>
          <div className="trust-row">
            <ShieldCheck />
            <span>
              <strong>AccessHub protected</strong>
              <small>
                Permission-aware access and secure API communication
              </small>
            </span>
          </div>
        </div>
        <small className="copyright">Enterprise Integration Platform</small>
      </section>
      <section className="login-panel">
        <form onSubmit={submit}>
          <div className="login-icon">
            <LockKeyhole />
          </div>
          <span className="eyebrow">Welcome back</span>
          <h2>Sign in to your workspace</h2>
          <p className="muted">Use your corporate AccessHub credentials.</p>
          {error && <div className="alert error">{error}</div>}
          <label>
            <span>Username</span>
            <input
              autoFocus
              autoComplete="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="name@company.com"
              required
            />
          </label>
          <label>
            <span>Password</span>
            <div className="password-field">
              <input
                type={showPassword ? "text" : "password"}
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                required
              />
              <button
                type="button"
                onClick={() => setShowPassword((x) => !x)}
                aria-label="Toggle password visibility"
              >
                {showPassword ? <EyeOff /> : <Eye />}
              </button>
            </div>
          </label>
          <button className="primary wide" disabled={loading}>
            {loading ? (
              "Signing in…"
            ) : (
              <>
                Sign in securely <ArrowRight size={18} />
              </>
            )}
          </button>
          <p className="security-note">
            Your credentials are sent directly to your organization’s
            authentication service.
          </p>
        </form>
      </section>
    </div>
  );
}
