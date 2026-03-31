import { Routes, Route, Navigate, Outlet } from 'react-router-dom'
import './App.css'
import SignUp from './pages/SignUp'
import SignIn from './pages/SignIn'
import ProfileTab from './pages/ProfileTab'
import DiscoverTab from './pages/DiscoverTab'
import MatchesTab from './pages/MatchesTab'
import ChatView from './pages/ChatView'
import TabBar from './components/TabBar'

function TabLayout() {
  return (
    <div className="tab-layout">
      <div className="tab-content">
        <Outlet />
      </div>
      <TabBar />
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/signup" element={<SignUp />} />
      <Route path="/signin" element={<SignIn />} />
      <Route path="/" element={<TabLayout />}>
        <Route index element={<Navigate to="/discover" replace />} />
        <Route path="profile" element={<ProfileTab />} />
        <Route path="discover" element={<DiscoverTab />} />
        <Route path="matches" element={<MatchesTab />} />
        <Route path="chat/:matchId" element={<ChatView />} />
      </Route>
    </Routes>
  )
}
