import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { supabase, RestaurantTable } from '@/lib/supabase'
import { useAuthStore } from '@/store/authStore'
import { getElapsedTimeShort } from '@/lib/utils'
import { MdTableRestaurant, MdAccessTime } from 'react-icons/md'
import './Dashboard.css'

export default function Tables() {
  const navigate = useNavigate()
  const { profile } = useAuthStore()
  const [tables, setTables] = useState<RestaurantTable[]>([])
  const [openOrders, setOpenOrders] = useState<Map<string, any>>(new Map())
  const [loading, setLoading] = useState(true)
  const [, setTick] = useState(0) // Süreyi güncellemek için

  useEffect(() => {
    loadTables()

    // Realtime subscription - orders tablosundaki değişiklikleri dinle
    console.log('Realtime subscription başlatılıyor...')
    const ordersChannel = supabase
      .channel('orders-changes')
      .on(
        'postgres_changes',
        {
          event: '*', // INSERT, UPDATE, DELETE
          schema: 'public',
          table: 'orders',
          filter: `organization_id=eq.${profile?.organization_id}`
        },
        (payload) => {
          console.log('Orders değişikliği algılandı:', payload)
          loadTables() // Masaları yeniden yükle
        }
      )
      .subscribe()

    // order_items tablosundaki değişiklikleri dinle
    const orderItemsChannel = supabase
      .channel('order-items-changes')
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'order_items',
          filter: `organization_id=eq.${profile?.organization_id}`
        },
        (payload) => {
          console.log('Order items değişikliği algılandı:', payload)
          loadTables() // Masaları yeniden yükle
        }
      )
      .subscribe()

    return () => {
      console.log('Realtime subscription kapatılıyor...')
      ordersChannel.unsubscribe()
      orderItemsChannel.unsubscribe()
    }
  }, [profile?.organization_id])

  // Süreyi her dakika güncelle
  useEffect(() => {
    const interval = setInterval(() => {
      setTick(prev => prev + 1)
    }, 60000) // 60 saniye

    return () => clearInterval(interval)
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

      if (tablesError) {
        console.error('Masalar yüklenirken hata:', tablesError)
        throw tablesError
      }

      // Açık adisyonları ve sipariş sayılarını al
      const { data: ordersData, error: ordersError } = await supabase
        .from('orders')
        .select('id, table_id, opened_at')
        .eq('organization_id', profile?.organization_id)
        .eq('status', 'open')

      if (ordersError) {
        console.error('Adisyonlar yüklenirken hata:', ordersError)
      }

      const ordersMap = new Map()
      
      // Her adisyon için sipariş sayısını kontrol et
      if (ordersData && ordersData.length > 0) {
        console.log('Açık adisyonlar:', ordersData)
        
        for (const order of ordersData) {
          const { count, error: countError } = await supabase
            .from('order_items')
            .select('*', { count: 'exact', head: true })
            .eq('order_id', order.id)
            .neq('status', 'cancelled')

          if (countError) {
            console.error(`Adisyon ${order.id} için ürünler yüklenirken hata:`, countError)
          }

          console.log(`Adisyon ${order.id} - Masa ${order.table_id}: ${count} ürün`)

          // SADECE ürünü olan adisyonları göster (mobil ile aynı mantık)
          if (count && count > 0) {
            ordersMap.set(order.table_id, { ...order, itemCount: count })
          }
        }
      }

      console.log('Yüklenen masalar:', tablesData?.length)
      console.log('Açık adisyonlar map:', ordersMap)

      setTables(tablesData || [])
      setOpenOrders(ordersMap)
    } catch (error) {
      console.error('Masalar yüklenirken hata:', error)
      alert('Masalar yüklenirken bir hata oluştu. Lütfen sayfayı yenileyin.')
    } finally {
      setLoading(false)
    }
  }

  const handleTableClick = async (table: RestaurantTable) => {
    try {
      // Önce bu masada açık adisyon var mı kontrol et
      const { data: existingOrder } = await supabase
        .from('orders')
        .select('id')
        .eq('organization_id', profile?.organization_id)
        .eq('table_id', table.id)
        .eq('status', 'open')
        .single()

      if (existingOrder) {
        // Varsa direkt git
        navigate(`/orders/${table.id}`)
      } else {
        // Yoksa yeni adisyon aç
        const { error } = await supabase
          .from('orders')
          .insert({
            organization_id: profile?.organization_id,
            table_id: table.id,
            opened_by_user_id: profile?.id,
            status: 'open'
          })
          .select()

        if (error) throw error

        navigate(`/orders/${table.id}`)
      }
    } catch (error) {
      console.error('Adisyon açılırken hata:', error)
      alert('Adisyon açılırken bir hata oluştu')
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
      <div className="page-header">
        <h1 className="page-title">Masalar</h1>
        <p className="page-subtitle">Masaya tıklayarak adisyon açın veya devam edin</p>
      </div>

      {tables.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', padding: '60px 20px' }}>
          <MdTableRestaurant style={{ fontSize: '64px', color: 'var(--text-secondary)', marginBottom: '16px' }} />
          <h3 style={{ marginBottom: '8px', color: 'var(--text-primary)' }}>Henüz Masa Eklenmemiş</h3>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '24px' }}>
            Masa eklemek için Masa Yönetimi sayfasına gidin
          </p>
          <button onClick={() => navigate('/table-management')} className="btn btn-primary">
            Masa Yönetimine Git
          </button>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: '12px' }}>
          {tables.map(table => {
            const orderInfo = openOrders.get(table.id)
            const hasOrder = !!orderInfo
            return (
              <div
                key={table.id}
                className="table-card"
                style={{
                  cursor: 'pointer',
                  background: hasOrder ? 'linear-gradient(135deg, #d4af37 0%, #c5a028 100%)' : 'white',
                  border: hasOrder ? '2px solid #d4af37' : '1px solid var(--border)',
                  borderRadius: '6px',
                  padding: '16px 12px',
                  textAlign: 'center',
                  transition: 'all 0.2s ease',
                  boxShadow: hasOrder ? 'var(--shadow-md)' : 'var(--shadow-sm)',
                  minHeight: '140px',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'space-between',
                  gap: '8px'
                }}
                onClick={() => handleTableClick(table)}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-2px)'
                  e.currentTarget.style.boxShadow = 'var(--shadow-md)'
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)'
                  e.currentTarget.style.boxShadow = hasOrder ? 'var(--shadow-md)' : 'var(--shadow-sm)'
                }}
              >
                <div>
                  <MdTableRestaurant style={{ 
                    fontSize: '32px', 
                    color: hasOrder ? '#1a1a1a' : 'var(--primary)',
                    marginBottom: '6px'
                  }} />
                  <h3 style={{ 
                    fontSize: '16px', 
                    fontWeight: 700,
                    marginBottom: '4px',
                    color: hasOrder ? '#1a1a1a' : 'var(--text-primary)'
                  }}>
                    {table.name}
                  </h3>
                  {hasOrder && orderInfo.opened_at && (
                    <p style={{ 
                      color: '#3a3a3a', 
                      fontSize: '11px', 
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      gap: '3px',
                      fontWeight: 600
                    }}>
                      <MdAccessTime style={{ fontSize: '12px' }} /> {getElapsedTimeShort(orderInfo.opened_at)}
                    </p>
                  )}
                </div>
                
                <div style={{
                  padding: '6px 12px',
                  borderRadius: '4px',
                  textAlign: 'center',
                  fontWeight: 700,
                  fontSize: '12px',
                  background: hasOrder ? '#1a1a1a' : '#107c10',
                  color: 'white',
                  letterSpacing: '0.5px'
                }}>
                  {hasOrder ? 'DOLU' : 'BOŞ'}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
