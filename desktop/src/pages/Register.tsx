import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import './Auth.css'

export default function Register() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [passwordConfirm, setPasswordConfirm] = useState('')
  const [fullName, setFullName] = useState('')
  const [restaurantName, setRestaurantName] = useState('')
  const [loading, setLoading] = useState(false)
  const { signUp, error, setError } = useAuthStore()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!email || !password || !passwordConfirm || !fullName || !restaurantName) {
      setError('Lütfen tüm alanları doldurun')
      return
    }

    if (password !== passwordConfirm) {
      setError('Şifreler eşleşmiyor')
      return
    }

    if (password.length < 6) {
      setError('Şifre en az 6 karakter olmalıdır')
      return
    }

    setLoading(true)
    try {
      await signUp(email, password, fullName, restaurantName)
    } catch (err) {
      // Hata authStore'da zaten set ediliyor
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-container">
      <div className="auth-card">
        <div className="auth-header">
          <h1 className="auth-title">Ordevo</h1>
          <p className="auth-subtitle">Yeni hesap oluşturun</p>
        </div>

        <form onSubmit={handleSubmit} className="auth-form">
          {error && (
            <div className="alert alert-error">
              {error}
            </div>
          )}

          <div className="form-group">
            <label className="form-label">Restoran Adı</label>
            <input
              type="text"
              className="form-input"
              value={restaurantName}
              onChange={(e) => setRestaurantName(e.target.value)}
              placeholder="Lezzet Durağı"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label className="form-label">Ad Soyad</label>
            <input
              type="text"
              className="form-input"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Ahmet Yılmaz"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label className="form-label">E-posta</label>
            <input
              type="email"
              className="form-input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="ornek@email.com"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label className="form-label">Şifre</label>
            <input
              type="password"
              className="form-input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label className="form-label">Şifre Tekrar</label>
            <input
              type="password"
              className="form-input"
              value={passwordConfirm}
              onChange={(e) => setPasswordConfirm(e.target.value)}
              placeholder="••••••••"
              disabled={loading}
            />
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: '100%' }} disabled={loading}>
            {loading ? 'Hesap oluşturuluyor...' : 'Hesap Oluştur'}
          </button>

          <p className="auth-footer">
            Zaten hesabınız var mı? <Link to="/login" className="auth-link">Giriş Yap</Link>
          </p>
        </form>
      </div>
    </div>
  )
}
