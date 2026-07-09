import { useEffect, useState } from 'react'
import { supabase, MenuItem, MenuCategory } from '@/lib/supabase'
import { useAuthStore } from '@/store/authStore'
import { formatCurrency } from '@/lib/utils'
import { MdAdd, MdEdit, MdDelete, MdFolder, MdList } from 'react-icons/md'
import './Dashboard.css'

export default function Menu() {
  const { profile } = useAuthStore()
  const [categories, setCategories] = useState<MenuCategory[]>([])
  const [menuItems, setMenuItems] = useState<MenuItem[]>([])
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  
  // Modal states
  const [showCategoryModal, setShowCategoryModal] = useState(false)
  const [showItemModal, setShowItemModal] = useState(false)
  const [editingCategory, setEditingCategory] = useState<MenuCategory | null>(null)
  const [editingItem, setEditingItem] = useState<MenuItem | null>(null)
  
  // Form states
  const [categoryForm, setCategoryForm] = useState({ name: '' })
  const [itemForm, setItemForm] = useState({
    name: '',
    description: '',
    price: '',
    category_id: ''
  })

  useEffect(() => {
    loadData()

    // Realtime subscription - menu değişikliklerini dinle
    console.log('Menu realtime subscription başlatılıyor...')
    const channel = supabase
      .channel('menu-realtime')
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'menu_items',
          filter: `organization_id=eq.${profile?.organization_id}`
        },
        (payload) => {
          console.log('Menu items değişikliği:', payload)
          loadData()
        }
      )
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'menu_categories',
          filter: `organization_id=eq.${profile?.organization_id}`
        },
        (payload) => {
          console.log('Menu categories değişikliği:', payload)
          loadData()
        }
      )
      .subscribe((status) => {
        console.log('Menu realtime subscription durumu:', status)
      })

    return () => {
      console.log('Menu realtime subscription kapatılıyor...')
      channel.unsubscribe()
    }
  }, [profile?.organization_id])

  const loadData = async () => {
    try {
      setLoading(true)

      const { data: categoriesData } = await supabase
        .from('menu_categories')
        .select('*')
        .eq('organization_id', profile?.organization_id)
        .order('sort_order')

      const { data: itemsData } = await supabase
        .from('menu_items')
        .select('*')
        .eq('organization_id', profile?.organization_id)
        .order('created_at', { ascending: true })

      setCategories(categoriesData || [])
      setMenuItems(itemsData || [])
    } catch (error) {
      console.error('Menü verisi yüklenirken hata:', error)
    } finally {
      setLoading(false)
    }
  }

  // Kategori işlemleri
  const handleAddCategory = () => {
    setEditingCategory(null)
    setCategoryForm({ name: '' })
    setShowCategoryModal(true)
  }

  const handleEditCategory = (category: MenuCategory) => {
    setEditingCategory(category)
    setCategoryForm({ name: category.name })
    setShowCategoryModal(true)
  }

  const handleDeleteCategory = async (category: MenuCategory) => {
    if (!confirm(`${category.name} kategorisi silinecek. Emin misiniz?`)) return

    try {
      const { error } = await supabase
        .from('menu_categories')
        .delete()
        .eq('id', category.id)

      if (error) throw error

      loadData()
    } catch (error) {
      console.error('Kategori silinirken hata:', error)
      alert('Kategori silinirken bir hata oluştu. Bu kategoriye ait ürünler varsa önce onları silin.')
    }
  }

  const handleSaveCategory = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!categoryForm.name) {
      alert('Kategori adı boş olamaz')
      return
    }

    try {
      if (editingCategory) {
        const { error } = await supabase
          .from('menu_categories')
          .update({ name: categoryForm.name })
          .eq('id', editingCategory.id)

        if (error) throw error
      } else {
        const { error } = await supabase
          .from('menu_categories')
          .insert({
            organization_id: profile?.organization_id,
            name: categoryForm.name,
            sort_order: categories.length + 1
          })

        if (error) throw error
      }

      setShowCategoryModal(false)
      loadData()
    } catch (error) {
      console.error('Kategori kaydedilirken hata:', error)
      alert('Kategori kaydedilirken bir hata oluştu')
    }
  }

  // Ürün işlemleri
  const handleAddItem = () => {
    setEditingItem(null)
    setItemForm({
      name: '',
      description: '',
      price: '',
      category_id: selectedCategory || categories[0]?.id || ''
    })
    setShowItemModal(true)
  }

  const handleEditItem = (item: MenuItem) => {
    setEditingItem(item)
    setItemForm({
      name: item.name,
      description: item.description || '',
      price: String(item.price),
      category_id: item.category_id
    })
    setShowItemModal(true)
  }

  const handleDeleteItem = async (item: MenuItem) => {
    if (!confirm(`${item.name} silinecek. Emin misiniz?`)) return

    try {
      const { error } = await supabase
        .from('menu_items')
        .update({ is_active: false })
        .eq('id', item.id)

      if (error) throw error

      loadData()
    } catch (error) {
      console.error('Ürün silinirken hata:', error)
      alert('Ürün silinirken bir hata oluştu')
    }
  }

  const handleSaveItem = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!itemForm.name || !itemForm.price || !itemForm.category_id) {
      alert('Lütfen tüm zorunlu alanları doldurun')
      return
    }

    const price = parseFloat(itemForm.price)
    if (isNaN(price) || price <= 0) {
      alert('Geçerli bir fiyat girin')
      return
    }

    try {
      if (editingItem) {
        const { error } = await supabase
          .from('menu_items')
          .update({
            name: itemForm.name,
            description: itemForm.description || null,
            price: price,
            category_id: itemForm.category_id
          })
          .eq('id', editingItem.id)

        if (error) throw error
      } else {
        const { error } = await supabase
          .from('menu_items')
          .insert({
            organization_id: profile?.organization_id,
            name: itemForm.name,
            description: itemForm.description || null,
            price: price,
            category_id: itemForm.category_id,
            is_active: true
          })

        if (error) throw error
      }

      setShowItemModal(false)
      loadData()
    } catch (error) {
      console.error('Ürün kaydedilirken hata:', error)
      alert('Ürün kaydedilirken bir hata oluştu')
    }
  }

  // Ürünleri kategoriye göre gruplandır
  const filteredItems = selectedCategory
    ? menuItems.filter(item => item.category_id === selectedCategory && item.is_active)
    : menuItems.filter(item => item.is_active)

  // Kategoriye göre sırala (aynı kategoridekiler ekleme sırasına göre)
  const sortedItems = [...filteredItems].sort((a, b) => {
    // Kategorileri bul
    const catA = categories.find(c => c.id === a.category_id)
    const catB = categories.find(c => c.id === b.category_id)
    
    // Kategorisiz ürünler en sona
    if (!catA && catB) return 1
    if (catA && !catB) return -1
    if (!catA && !catB) return 0 // Kategorisizler kendi aralarında sıralanmaz
    
    // Sadece kategori sort_order'a göre sırala
    return catA!.sort_order - catB!.sort_order
  })

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
      <div className="page-header">
        <h1 className="page-title">Menü Yönetimi</h1>
        <p className="page-subtitle">Kategorileri ve ürünleri yönetin</p>
      </div>

      <div className="grid grid-2" style={{ gap: '2rem', alignItems: 'start' }}>
        {/* Sol taraf - Kategoriler */}
        <div>
          <div className="card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h2 className="card-title" style={{ marginBottom: 0 }}>Kategoriler</h2>
              <button onClick={handleAddCategory} className="btn btn-sm btn-primary">
                <MdAdd /> Kategori Ekle
              </button>
            </div>

            {categories.length === 0 ? (
              <p className="text-secondary">Henüz kategori yok</p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                <button
                  onClick={() => setSelectedCategory(null)}
                  className={`btn ${!selectedCategory ? 'btn-primary' : 'btn-secondary'}`}
                  style={{ justifyContent: 'flex-start' }}
                >
                  <MdList /> Tüm Ürünler ({menuItems.filter(i => i.is_active).length})
                </button>
                {categories.map(category => {
                  const count = menuItems.filter(i => i.category_id === category.id && i.is_active).length
                  return (
                    <div key={category.id} style={{ display: 'flex', gap: '0.5rem' }}>
                      <button
                        onClick={() => setSelectedCategory(category.id)}
                        className={`btn ${selectedCategory === category.id ? 'btn-primary' : 'btn-secondary'}`}
                        style={{ flex: 1, justifyContent: 'flex-start' }}
                      >
                        <MdFolder /> {category.name} ({count})
                      </button>
                      <button
                        onClick={() => handleEditCategory(category)}
                        className="btn btn-sm btn-secondary"
                      >
                        <MdEdit />
                      </button>
                      <button
                        onClick={() => handleDeleteCategory(category)}
                        className="btn btn-sm btn-danger"
                      >
                        <MdDelete />
                      </button>
                    </div>
                  )
                })}
              </div>
            )}
          </div>
        </div>

        {/* Sağ taraf - Ürünler */}
        <div>
          <div className="card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h2 className="card-title" style={{ marginBottom: 0 }}>
                {selectedCategory
                  ? categories.find(c => c.id === selectedCategory)?.name
                  : 'Tüm Ürünler'}
              </h2>
              <button onClick={handleAddItem} className="btn btn-sm btn-primary">
                <MdAdd /> Ürün Ekle
              </button>
            </div>

            {sortedItems.length === 0 ? (
              <p className="text-secondary">Bu kategoride ürün yok</p>
            ) : (
              <div className="table-container" style={{ maxHeight: '600px', overflowY: 'auto' }}>
                <table className="table">
                  <thead>
                    <tr>
                      <th>Ürün Adı</th>
                      <th>Kategori</th>
                      <th>Fiyat</th>
                      <th style={{ textAlign: 'right' }}>İşlemler</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedItems.map(item => {
                      const category = categories.find(c => c.id === item.category_id)
                      return (
                        <tr key={item.id}>
                          <td>
                            <div>
                              <p style={{ fontWeight: 600 }}>{item.name}</p>
                              {item.description && (
                                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                                  {item.description}
                                </p>
                              )}
                            </div>
                          </td>
                          <td>
                            <span style={{ 
                              padding: '4px 8px', 
                              background: 'var(--bg-hover)', 
                              borderRadius: '4px',
                              fontSize: '0.875rem',
                              fontWeight: 500
                            }}>
                              {category?.name || 'Kategorisiz'}
                            </span>
                          </td>
                          <td>{formatCurrency(item.price)}</td>
                          <td style={{ textAlign: 'right' }}>
                            <button
                              onClick={() => handleEditItem(item)}
                              className="btn btn-sm btn-secondary"
                              style={{ marginRight: '8px' }}
                            >
                              <MdEdit />
                            </button>
                            <button
                              onClick={() => handleDeleteItem(item)}
                              className="btn btn-sm btn-danger"
                            >
                              <MdDelete />
                            </button>
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Kategori Modal */}
      {showCategoryModal && (
        <div className="modal-overlay" onClick={() => setShowCategoryModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">{editingCategory ? 'Kategori Düzenle' : 'Yeni Kategori'}</h2>
              <button onClick={() => setShowCategoryModal(false)} className="modal-close">×</button>
            </div>

            <form onSubmit={handleSaveCategory}>
              <div className="form-group">
                <label className="form-label">Kategori Adı</label>
                <input
                  type="text"
                  className="form-input"
                  value={categoryForm.name}
                  onChange={(e) => setCategoryForm({ name: e.target.value })}
                  placeholder="Kebaplar"
                  autoFocus
                />
              </div>

              <div className="modal-footer">
                <button type="button" onClick={() => setShowCategoryModal(false)} className="btn btn-secondary">
                  İptal
                </button>
                <button type="submit" className="btn btn-primary">
                  Kaydet
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Ürün Modal */}
      {showItemModal && (
        <div className="modal-overlay" onClick={() => setShowItemModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">{editingItem ? 'Ürün Düzenle' : 'Yeni Ürün'}</h2>
              <button onClick={() => setShowItemModal(false)} className="modal-close">×</button>
            </div>

            <form onSubmit={handleSaveItem}>
              <div className="form-group">
                <label className="form-label">Ürün Adı</label>
                <input
                  type="text"
                  className="form-input"
                  value={itemForm.name}
                  onChange={(e) => setItemForm({ ...itemForm, name: e.target.value })}
                  placeholder="Adana Kebap"
                  autoFocus
                />
              </div>

              <div className="form-group">
                <label className="form-label">Açıklama (opsiyonel)</label>
                <textarea
                  className="form-textarea"
                  value={itemForm.description}
                  onChange={(e) => setItemForm({ ...itemForm, description: e.target.value })}
                  placeholder="Ürün açıklaması..."
                  rows={3}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Kategori</label>
                <select
                  className="form-select"
                  value={itemForm.category_id}
                  onChange={(e) => setItemForm({ ...itemForm, category_id: e.target.value })}
                >
                  <option value="">Kategori seçin</option>
                  {categories.map(cat => (
                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Fiyat (₺)</label>
                <input
                  type="number"
                  step="0.01"
                  className="form-input"
                  value={itemForm.price}
                  onChange={(e) => setItemForm({ ...itemForm, price: e.target.value })}
                  placeholder="0.00"
                />
              </div>

              <div className="modal-footer">
                <button type="button" onClick={() => setShowItemModal(false)} className="btn btn-secondary">
                  İptal
                </button>
                <button type="submit" className="btn btn-primary">
                  Kaydet
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
