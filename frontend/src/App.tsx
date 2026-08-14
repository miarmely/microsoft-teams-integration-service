import { Navigate, Outlet, Route, Routes } from "react-router-dom";
import { useAuth } from "./auth";
import { AppShell } from "./components/AppShell";
import { LoginPage } from "./pages/LoginPage";
import { MessagesPage } from "./pages/MessagesPage";
import { SendPage } from "./pages/SendPage";
import { SyncPage } from "./pages/SyncPage";

/** Guards dashboard routes and renders them inside the authenticated shell. */
function Protected() {
  const { token } = useAuth();
  return token ? (
    <AppShell>
      <Outlet />
    </AppShell>
  ) : (
    <Navigate to="/login" replace />
  );
}

/** Defines all client-side routes and the default fallback destination. */
export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<Protected />}>
        <Route path="/messages/live" element={<MessagesPage mode="live" />} />
        <Route
          path="/messages/stored"
          element={<MessagesPage mode="stored" />}
        />
        <Route path="/synchronize" element={<SyncPage />} />
        <Route path="/send" element={<SendPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/messages/live" replace />} />
    </Routes>
  );
}
