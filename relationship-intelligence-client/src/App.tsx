import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { Layout } from "@/components/layout";
import { AuthProvider, useAuth } from "@/lib/auth";
import { CopilotProvider } from "@/lib/copilot";
import { Digest } from "@/pages/Digest";
import { Login } from "@/pages/Login";
import { MeetingDetail } from "@/pages/MeetingDetail";
import { Meetings } from "@/pages/Meetings";
import { Network } from "@/pages/Network";
import { Organizations } from "@/pages/Organizations";
import { Overview } from "@/pages/Overview";
import { People } from "@/pages/People";
import { PersonDetail } from "@/pages/PersonDetail";
import { PersonEdit } from "@/pages/PersonEdit";
import { PersonNew } from "@/pages/PersonNew";
import { Queue } from "@/pages/Queue";
import { Register } from "@/pages/Register";
import type { JSX } from "react";

function RequireAuth({ children }: { children: JSX.Element }) {
  const { user, ready } = useAuth();
  const location = useLocation();
  if (!ready) return null;
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return children;
}

function PublicOnly({ children }: { children: JSX.Element }) {
  const { user, ready } = useAuth();
  if (!ready) return null;
  if (user) return <Navigate to="/" replace />;
  return children;
}

export function App() {
  return (
    <AuthProvider>
      <CopilotProvider>
      <Routes>
        <Route
          path="/login"
          element={
            <PublicOnly>
              <Login />
            </PublicOnly>
          }
        />
        <Route
          path="/register"
          element={
            <PublicOnly>
              <Register />
            </PublicOnly>
          }
        />
        <Route
          element={
            <RequireAuth>
              <Layout />
            </RequireAuth>
          }
        >
          <Route index element={<Overview />} />
          <Route path="attention" element={<Queue />} />
          <Route path="people" element={<People />} />
          <Route path="people/new" element={<PersonNew />} />
          <Route path="people/:id" element={<PersonDetail />} />
          <Route path="people/:id/edit" element={<PersonEdit />} />
          <Route path="organizations" element={<Organizations />} />
          <Route path="meetings" element={<Meetings />} />
          <Route path="meetings/:id" element={<MeetingDetail />} />
          <Route path="network" element={<Network />} />
          <Route path="digest" element={<Digest />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      </CopilotProvider>
    </AuthProvider>
  );
}
