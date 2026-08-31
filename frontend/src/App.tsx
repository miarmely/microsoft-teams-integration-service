import { Navigate, Outlet, Route, Routes } from "react-router-dom";
import { useAuth } from "./auth";
import { AppShell } from "./components/AppShell";
import { LoginPage } from "./pages/LoginPage";
import { MessagesPage } from "./pages/MessagesPage";
import { SendPage } from "./pages/SendPage";
import { SyncPage } from "./pages/SyncPage";
import { TeamsPage } from "./pages/TeamsPage";
import { ChannelsPage } from "./pages/ChannelsPage";
import { ExportPage } from "./pages/ExportPage";
import { DeleteMessagesPage } from "./pages/DeleteMessagesPage";
import { ConnectTeamsPage } from "./pages/ConnectTeamsPage";
import { LogsPage } from "./pages/LogsPage";
import { RequireTeamsConnection } from "./components/RequireTeamsConnection";
import { TeamsConnectionProvider } from "./teamsConnection";

/** Guards dashboard routes and renders them inside the authenticated shell. */
function Protected() {
  const { token } = useAuth();
  return token ? (
    <TeamsConnectionProvider>
      <AppShell>
        <Outlet />
      </AppShell>
    </TeamsConnectionProvider>
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
        <Route path="/connect-teams" element={<ConnectTeamsPage />} />
        <Route path="/logs" element={<LogsPage />} />
        <Route element={<RequireTeamsConnection />}>
          <Route path="/teams" element={<TeamsPage />} />
          <Route path="/channels" element={<ChannelsPage />} />
          <Route path="/messages/live" element={<MessagesPage mode="live" />} />
          <Route path="/messages/stored" element={<MessagesPage mode="stored" />} />
          <Route path="/synchronize" element={<SyncPage />} />
          <Route path="/export" element={<ExportPage />} />
          <Route path="/messages/delete" element={<DeleteMessagesPage />} />
          <Route path="/send" element={<SendPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/messages/live" replace />} />
    </Routes>
  );
}
