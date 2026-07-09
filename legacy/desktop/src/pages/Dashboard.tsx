import { useEffect, useState } from 'react'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/store/authStore'
import { formatCurrency, getDateRange } from '@/lib/utils'
import { MdToday, MdDateRange, MdCalendarMonth, MdCalendarToday, MdReceipt } from 'react-icons/md'
import WeatherCard from '@/components/WeatherCard'
import './Dashboard.css'

interface Stats {
  todayRevenue: number
  weekRevenue: number
  monthRevenue: number
  yearRevenue: number
  openOrders: number
}

interface TopProduct {
  name: string
  quantity: number
  revenue: number
}

export default function Dashboard() {
  const { profile } = useAuthStore()
  const [stats, setStats] = useState<Stats>({
    todayRevenue: 0,
    weekRevenue: 0,
    monthRevenue: 0,
    yearRevenue: 0,
    openOrders: 0
  })
  const [topProducts, setTopProducts] = useState<TopProduct[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadDashboardData()
  }, [])

  const loadDashboardData = async () => {
    try {
      setLoading(true)
      await Promise.all([
        loadStats(),
        loadTopProducts()
      ])
    } catch (error) {
      console.error('Dashboard verisi yüklenirken hata:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadStats = async () => {
    // Bugünkü ciro (sadece aktif - bugün henüz arşivlenmedi)
    const todayRange = getDateRange('today')
    const { data: todayPayments } = await supabase
      .from('payments')
      .select('amount')
      .eq('organization_id', profile?.organization_id)
      .gte('created_at', todayRange.start.toISOString())
      .lte('created_at', todayRange.end.toISOString())

    const todayRevenue = todayPayments?.reduce((sum, p) => sum + Number(p.amount), 0) || 0

    // Bu haftaki ciro (aktif + arşiv)
    const weekRange = getDateRange('week')
    const weekStart = weekRange.start.toISOString().split('T')[0]
    const weekEnd = weekRange.end.toISOString().split('T')[0]

    const { data: weekPayments } = await supabase
      .from('payments')
      .select('amount')
      .eq('organization_id', profile?.organization_id)
      .gte('created_at', weekRange.start.toISOString())
      .lte('created_at', weekRange.end.toISOString())

    const { data: weekArchive } = await supabase
      .from('daily_sales_archive')
      .select('total_revenue')
      .eq('organization_id', profile?.organization_id)
      .gte('business_date', weekStart)
      .lte('business_date', weekEnd)

    const weekActiveRevenue = weekPayments?.reduce((sum, p) => sum + Number(p.amount), 0) || 0
    const weekArchivedRevenue = weekArchive?.reduce((sum, s) => sum + Number(s.total_revenue), 0) || 0
    const weekRevenue = weekActiveRevenue + weekArchivedRevenue

    // Bu ayki ciro (aktif + arşiv)
    const monthRange = getDateRange('month')
    const monthStart = monthRange.start.toISOString().split('T')[0]
    const monthEnd = monthRange.end.toISOString().split('T')[0]

    const { data: monthPayments } = await supabase
      .from('payments')
      .select('amount')
      .eq('organization_id', profile?.organization_id)
      .gte('created_at', monthRange.start.toISOString())
      .lte('created_at', monthRange.end.toISOString())

    const { data: monthArchive } = await supabase
      .from('daily_sales_archive')
      .select('total_revenue')
      .eq('organization_id', profile?.organization_id)
      .gte('business_date', monthStart)
      .lte('business_date', monthEnd)

    const monthActiveRevenue = monthPayments?.reduce((sum, p) => sum + Number(p.amount), 0) || 0
    const monthArchivedRevenue = monthArchive?.reduce((sum, s) => sum + Number(s.total_revenue), 0) || 0
    const monthRevenue = monthActiveRevenue + monthArchivedRevenue

    // Bu yılki ciro (aktif + arşiv)
    const yearRange = getDateRange('year')
    const yearStart = yearRange.start.toISOString().split('T')[0]
    const yearEnd = yearRange.end.toISOString().split('T')[0]

    const { data: yearPayments } = await supabase
      .from('payments')
      .select('amount')
      .eq('organization_id', profile?.organization_id)
      .gte('created_at', yearRange.start.toISOString())
      .lte('created_at', yearRange.end.toISOString())

    const { data: yearArchive } = await supabase
      .from('daily_sales_archive')
      .select('total_revenue')
      .eq('organization_id', profile?.organization_id)
      .gte('business_date', yearStart)
      .lte('business_date', yearEnd)

    const yearActiveRevenue = yearPayments?.reduce((sum, p) => sum + Number(p.amount), 0) || 0
    const yearArchivedRevenue = yearArchive?.reduce((sum, s) => sum + Number(s.total_revenue), 0) || 0
    const yearRevenue = yearActiveRevenue + yearArchivedRevenue

    // Açık adisyon sayısı
    const { count: openOrders } = await supabase
      .from('orders')
      .select('*', { count: 'exact', head: true })
      .eq('organization_id', profile?.organization_id)
      .eq('status', 'open')

    setStats({
      todayRevenue,
      weekRevenue,
      monthRevenue,
      yearRevenue,
      openOrders: openOrders || 0
    })
  }

  const loadTopProducts = async () => {
    const weekRange = getDateRange('week')
    const weekStart = weekRange.start.toISOString().split('T')[0]
    const weekEnd = weekRange.end.toISOString().split('T')[0]

    const productMap = new Map<string, TopProduct>()
    
    // Aktif sipariş kalemleri
    const { data: activeItems } = await supabase
      .from('order_items')
      .select('menu_item_id, quantity, total_price, menu_items(name)')
      .eq('organization_id', profile?.organization_id)
      .gte('created_at', weekRange.start.toISOString())
      .lte('created_at', weekRange.end.toISOString())
      .neq('status', 'cancelled')

    if (activeItems) {
      activeItems.forEach((item: any) => {
        const name = item.menu_items?.name || 'Bilinmeyen'
        const existing = productMap.get(name)
        
        if (existing) {
          existing.quantity += item.quantity
          existing.revenue += Number(item.total_price)
        } else {
          productMap.set(name, {
            name,
            quantity: item.quantity,
            revenue: Number(item.total_price)
          })
        }
      })
    }

    // Arşivlenmiş sipariş kalemleri
    const { data: archivedItems } = await supabase
      .from('archived_order_items')
      .select('menu_item_name, quantity, total_price')
      .eq('organization_id', profile?.organization_id)
      .gte('business_date', weekStart)
      .lte('business_date', weekEnd)

    if (archivedItems) {
      archivedItems.forEach((item: any) => {
        const name = item.menu_item_name
        const existing = productMap.get(name)
        
        if (existing) {
          existing.quantity += item.quantity
          existing.revenue += Number(item.total_price)
        } else {
          productMap.set(name, {
            name,
            quantity: item.quantity,
            revenue: Number(item.total_price)
          })
        }
      })
    }

    // En çok satanları sırala
    const sorted = Array.from(productMap.values())
      .sort((a, b) => b.quantity - a.quantity)
      .slice(0, 10)

    setTopProducts(sorted)
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
        <h1 className="page-title">Dashboard</h1>
        <p className="page-subtitle">Hoş geldiniz, {profile?.full_name}</p>
      </div>

      <div className="grid grid-4 mb-4">
        <div className="stat-card">
          <div className="stat-icon"><MdToday /></div>
          <div className="stat-content">
            <p className="stat-label">Bugünkü Ciro</p>
            <p className="stat-value">{formatCurrency(stats.todayRevenue)}</p>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon"><MdDateRange /></div>
          <div className="stat-content">
            <p className="stat-label">Bu Haftaki Ciro</p>
            <p className="stat-value">{formatCurrency(stats.weekRevenue)}</p>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon"><MdCalendarMonth /></div>
          <div className="stat-content">
            <p className="stat-label">Bu Ayki Ciro</p>
            <p className="stat-value">{formatCurrency(stats.monthRevenue)}</p>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon"><MdCalendarToday /></div>
          <div className="stat-content">
            <p className="stat-label">Bu Yılki Ciro</p>
            <p className="stat-value">{formatCurrency(stats.yearRevenue)}</p>
          </div>
        </div>
      </div>

      {/* Hava Durumu Kartı */}
      <div className="mb-4">
        <WeatherCard />
      </div>

      <div className="grid grid-2">
        <div className="card">
          <h2 className="card-title">
            <MdReceipt style={{ verticalAlign: 'middle', marginRight: '8px' }} />
            Açık Adisyonlar
          </h2>
          <div className="stat-big">
            <span className="stat-big-value">{stats.openOrders}</span>
            <span className="stat-big-label">Açık Adisyon</span>
          </div>
        </div>

        <div className="card">
          <h2 className="card-title">En Çok Satan Ürünler (Son 7 Gün)</h2>
          {topProducts.length === 0 ? (
            <p className="text-secondary">Henüz veri yok</p>
          ) : (
            <div className="top-products">
              {topProducts.map((product, index) => (
                <div key={index} className="top-product-item">
                  <div className="top-product-rank">{index + 1}</div>
                  <div className="top-product-info">
                    <p className="top-product-name">{product.name}</p>
                    <p className="top-product-stats">
                      {product.quantity} adet • {formatCurrency(product.revenue)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
