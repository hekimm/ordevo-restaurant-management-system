import { useEffect } from 'react'
import { HashRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './store/authStore'
import { useDailyArchiveStore } from './store/dailyArchiveStore'
import Login from './pages/Login'
import Register from './pages/Register'
import Dashboard from './pages/Dashboard'
import Tables from './pages/Tables'
import TableManagement from './pages/TableManagement'
import Orders from './pages/Orders'
import Menu from './pages/Menu'
import Reports from './pages/Reports'
import Settings from './pages/Settings'
import MLExport from './pages/MLExport'
import Layout from './components/Layout'
import './App.css'

function App() {
  const { user, loading, loadUserData } = useAuthStore()
  const { checkAndArchive } = useDailyArchiveStore()

  useEffect(() => {
    loadUserData()
  }, [])

  // Kullanıcı giriş yaptığında günlük arşivleme kontrolü yap
  useEffect(() => {
    if (user) {
      checkAndArchive()
      
      // Her 1 saatte bir kontrol et (gece yarısı geçişini yakalamak için)
      const interval = setInterval(() => {
        checkAndArchive()
      }, 60 * 60 * 1000) // 1 saat

      return () => clearInterval(interval)
    }
  }, [user])

  if (loading) {
    return (
      <div className="loading-screen">
        <div className="spinner"></div>
        <p>Yükleniyor...</p>
      </div>
    )
  }

  return (
    <HashRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <Routes>
        {!user ? (
          <>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="*" element={<Navigate to="/login" replace />} />
          </>
        ) : (
          <Route path="/" element={<Layout />}>
            <Route index element={<Dashboard />} />
            <Route path="tables" element={<Tables />} />
            <Route path="table-management" element={<TableManagement />} />
            <Route path="orders/:tableId?" element={<Orders />} />
            <Route path="menu" element={<Menu />} />
            <Route path="reports" element={<Reports />} />
            <Route path="settings" element={<Settings />} />
            <Route path="ml-export" element={<MLExport />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        )}
      </Routes>
    </HashRouter>
  )
}

export default App
