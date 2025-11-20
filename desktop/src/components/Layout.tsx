import { Outlet, NavLink } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { MdDashboard, MdTableRestaurant, MdRestaurantMenu, MdBarChart, MdLogout, MdSettings, MdTableChart } from 'react-icons/md'
import './Layout.css'

export default function Layout() {
  const { profile, organization, signOut } = useAuthStore()

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-header">
          <div className="app-logo">
            <div className="logo-icon">O</div>
            <div className="logo-text">
              <h1 className="sidebar-title">{organization?.name || 'Ordevo'}</h1>
              <p className="sidebar-subtitle">{profile?.full_name}</p>
            </div>
          </div>
        </div>

        <nav className="sidebar-nav">
          <NavLink to="/" end className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            <MdDashboard className="nav-icon" />
            <span className="nav-text">Dashboard</span>
          </NavLink>
          <NavLink to="/tables" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            <MdTableRestaurant className="nav-icon" />
            <span className="nav-text">Masalar</span>
          </NavLink>
          
          <div style={{ 
            height: '1px', 
            background: 'var(--border)', 
            margin: '8px 20px' 
          }} />
          
          <NavLink to="/table-management" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            <MdSettings className="nav-icon" />
            <span className="nav-text">Masa Yönetimi</span>
          </NavLink>
          <NavLink to="/menu" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            <MdRestaurantMenu className="nav-icon" />
            <span className="nav-text">Menü Yönetimi</span>
          </NavLink>
          <NavLink to="/reports" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            <MdBarChart className="nav-icon" />
            <span className="nav-text">Raporlar</span>
          </NavLink>
          <NavLink to="/ml-export" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            <MdTableChart className="nav-icon" />
            <span className="nav-text">ML Veri Export</span>
          </NavLink>
          <NavLink to="/settings" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            <MdSettings className="nav-icon" />
            <span className="nav-text">Yazıcı Ayarları</span>
          </NavLink>
        </nav>

        <div className="sidebar-footer">
          <button onClick={signOut} className="btn btn-secondary btn-sm" style={{ width: '100%' }}>
            <MdLogout />
            <span>Çıkış Yap</span>
          </button>
        </div>
      </aside>

      <main className="main-content">
        <Outlet />
      </main>
    </div>
  )
}
