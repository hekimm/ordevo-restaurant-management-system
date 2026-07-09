import { useEffect, useState } from 'react'
import { supabase, RestaurantTable } from '@/lib/supabase'
import { useAuthStore } from '@/store/authStore'
import { MdAdd, MdEdit, MdDelete, MdSettings } from 'react-icons/md'
import './Dashboard.css'

export default function TableManagement() {
  const { profile } = useAuthStore()
  const [tables, setTables] = useState<RestaurantTable[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editingTable, setEditingTable] = useState<RestaurantTable | null>(null)
  const [formData, setFormData] = useState({ name: '' })

  useEffect(() => {
    loadTables()
  }, [])

  const loadTables = async () => {
    try {
      setLoading(true)
      
      const { data: tablesData, error: tablesError } = await supabase
        .from('restaurant_tables')
        .select('*')
        .eq('organization_id', profile?.organization_id)
        .eq('is_active', true)
        .order('sort_order')

      if (tablesError) throw tablesError

      setTables(tablesData || [])
    } catch (error) {
      console.error('Masalar yüklenirken hata:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleAddTable = () => {
    setEditingTable(null)
    setFormData({ name: '' })
    setShowModal(true)
  }

  const handleEditTable = (table: RestaurantTable) => {
    setEditingTable(table)
    setFormData({ name: table.name })
    setShowModal(true)
  }

  const handleDeleteTable = async (table: RestaurantTable) => {
    if (!confirm(`${table.name} silinecek. Emin misiniz?`)) return

    try {
      const { error } = await supabase
        .from('restaurant_tables')
        .update({ is_active: false })
        .eq('id', table.id)

      if (error) throw error

      loadTables()
    } catch (error) {
      console.error('Masa silinirken hata:', error)
      alert('Masa silinirken bir hata oluştu')
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!formData.name) {
      alert('Masa adı boş olamaz')
      return
    }

    try {
      if (editingTable) {
        const { error } = await supabase
          .from('restaurant_tables')
          .update({
            name: formData.name
          })
          .eq('id', editingTable.id)

        if (error) throw error
      } else {
        const { error } = await supabase
          .from('restaurant_tables')
          .insert({
            organization_id: profile?.organization_id,
            name: formData.name,
            sort_order: tables.length + 1
          })

        if (error) throw error
      }

      setShowModal(false)
      loadTables()
    } catch (error) {
      console.error('Masa kaydedilirken hata:', error)
      alert('Masa kaydedilirken bir hata oluştu')
    }
  }

  if (loading) {
    return (
      <div className="page-container">
        <div className="loading-screen">
          <div className="spinner"></div>
          <p>Yükleniyor...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="page-container">
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 className="page-title" style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <MdSettings /> Masa Yönetimi
          </h1>
          <p className="page-subtitle">Masaları ekleyin, düzenleyin veya silin</p>
        </div>
        <button onClick={handleAddTable} className="btn btn-primary">
          <MdAdd /> Yeni Masa Ekle
        </button>
      </div>

      <div className="card">
        <div className="table-container">
          <table className="table">
            <thead>
              <tr>
                <th>Sıra</th>
                <th>Masa Adı</th>
                <th style={{ textAlign: 'right' }}>İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {tables.length === 0 ? (
                <tr>
                  <td colSpan={3} style={{ textAlign: 'center', padding: '40px', color: 'var(--text-secondary)' }}>
                    Henüz masa eklenmemiş. Yukarıdaki butona tıklayarak yeni masa ekleyin.
                  </td>
                </tr>
              ) : (
                tables.map((table, index) => (
                  <tr key={table.id}>
                    <td style={{ fontWeight: 600, color: 'var(--text-secondary)' }}>{index + 1}</td>
                    <td style={{ fontWeight: 600, fontSize: '15px' }}>{table.name}</td>
                    <td style={{ textAlign: 'right' }}>
                      <button
                        onClick={() => handleEditTable(table)}
                        className="btn btn-sm btn-secondary"
                        style={{ marginRight: '8px' }}
                      >
                        <MdEdit /> Düzenle
                      </button>
                      <button
                        onClick={() => handleDeleteTable(table)}
                        className="btn btn-sm btn-danger"
                      >
                        <MdDelete /> Sil
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">{editingTable ? 'Masa Düzenle' : 'Yeni Masa Ekle'}</h2>
              <button onClick={() => setShowModal(false)} className="modal-close">×</button>
            </div>

            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label className="form-label">Masa Adı *</label>
                <input
                  type="text"
                  className="form-input"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="Örn: Masa 1, Bahçe 3, Salon A"
                  autoFocus
                />
              </div>

              <div className="modal-footer">
                <button type="button" onClick={() => setShowModal(false)} className="btn btn-secondary">
                  İptal
                </button>
                <button type="submit" className="btn btn-primary">
                  <MdAdd /> {editingTable ? 'Güncelle' : 'Ekle'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
