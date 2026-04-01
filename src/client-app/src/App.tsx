import { Routes, Route, Navigate, Outlet } from 'react-router-dom'
import './App.css'
import SignUp from './pages/SignUp'
import SignIn from './pages/SignIn'
import ProfileTab from './pages/ProfileTab'
import DiscoverTab from './pages/DiscoverTab'
import MatchesTab from './pages/MatchesTab'
import ChatView from './pages/ChatView'
import TabBar from './components/TabBar'
import { AuthProvider, useAuth } from './hooks/useAuth'
import { TenantProvider, useTenant } from './hooks/useTenant'

function TenantHeader() {
  const { tenantName } = useTenant()
  if (!tenantName) return null
  return <header className="tenant-header">{tenantName}</header>
}

function TabLayout() {
  return (
    <div className="tab-layout">
      <TenantHeader />
      <div className="tab-content">
        <Outlet />
      </div>
      <TabBar />
    </div>
  )
}

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()
  if (isLoading) return null
  if (!isAuthenticated) return <Navigate to="/signin" replace />
  return <>{children}</>
}

function GuestRoute({ children }: { children: React.ReactNode }) {
  const { isLoading } = useAuth()
  if (isLoading) return null
  return <>{children}</>
}

export default function App() {
  return (
    <TenantProvider>
      <AuthProvider>
        <Routes>
          <Route path="/signup" element={<GuestRoute><SignUp /></GuestRoute>} />
          <Route path="/signin" element={<GuestRoute><SignIn /></GuestRoute>} />
          <Route path="/" element={<ProtectedRoute><TabLayout /></ProtectedRoute>}>
            <Route index element={<Navigate to="/discover" replace />} />
            <Route path="profile" element={<ProfileTab />} />
            <Route path="discover" element={<DiscoverTab />} />
            <Route path="matches" element={<MatchesTab />} />
            <Route path="chat/:conversationId" element={<ChatView />} />
          </Route>
        </Routes>
      </AuthProvider>
    </TenantProvider>
  )
}
