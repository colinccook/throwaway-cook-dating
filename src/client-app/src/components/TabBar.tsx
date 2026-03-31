import { NavLink } from 'react-router-dom';

export default function TabBar() {
  return (
    <nav className="tab-bar">
      <NavLink to="/profile" className={({ isActive }) => isActive ? 'active' : ''}>
        <span className="tab-icon">👤</span>
        <span>Profile</span>
      </NavLink>
      <NavLink to="/discover" className={({ isActive }) => isActive ? 'active' : ''}>
        <span className="tab-icon">❤️</span>
        <span>Discover</span>
      </NavLink>
      <NavLink to="/matches" className={({ isActive }) => isActive ? 'active' : ''}>
        <span className="tab-icon">💬</span>
        <span>Matches</span>
      </NavLink>
    </nav>
  );
}
