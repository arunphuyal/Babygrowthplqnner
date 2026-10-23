import { Navigate, NavLink, Outlet, Route, Routes, useNavigate } from 'react-router-dom'
import { auth } from './api'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import Users from './pages/Users'
import Articles from './pages/Articles'
import Plans from './pages/Plans'
import Waitlist from './pages/Waitlist'

function Layout() {
  const nav = useNavigate()
  if (!auth.get()) return <Navigate to="/login" replace />
  return (
    <div className="shell">
      <aside>
        <h1>🌱 PottyTrain<small>Admin</small></h1>
        <NavLink to="/" end>Dashboard</NavLink>
        <NavLink to="/users">Users</NavLink>
        <NavLink to="/articles">Content</NavLink>
        <NavLink to="/plans">Plans &amp; pricing</NavLink>
        <NavLink to="/waitlist">Waitlist</NavLink>
        <button className="link" onClick={() => { auth.clear(); nav('/login') }}>Sign out</button>
      </aside>
      <main><Outlet /></main>
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="users" element={<Users />} />
        <Route path="articles" element={<Articles />} />
        <Route path="plans" element={<Plans />} />
        <Route path="waitlist" element={<Waitlist />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
