import { useEffect, useState } from 'react'
import { supabase } from '@/lib/supabase'
import { formatCurrency, formatDate, getDateRange, getPaymentMethodLabel, getRoleLabel } from '@/lib/utils'
import { MdToday, MdDateRange, MdCalendarMonth, MdCalendarToday, MdAttachMoney, MdReceipt, MdBarChart, MdArchive } from 'react-icons/md'
import { useDailyArchiveStore } from '@/store/dailyArchiveStore'
import { useAuthStore } from '@/store/authStore'
import './Dashboard.css'

interface ReportStats {
  totalRevenue: number
  totalOrders: number
  averageOrder: number
}

interface TopProduct {
  name: string
  quantity: number
  revenue: number
}

interface PaymentMethodStat {
  method: string
  count: number
  amount: number
}

interface WaiterPerformance {
  name: string
  role: string
  ordersOpened: number
  itemsCreated: number
  totalRevenue: number
  averageOrder: number
}

export default function Reports() {
  const { profile } = useAuthStore()
  const [period, setPeriod] = useState<'today' | 'week' | 'month' | 'year'>('today')
  const [stats, setStats] = useState<ReportStats>({ totalRevenue: 0, totalOrders: 0, averageOrder: 0 })
  const [topProducts, setTopProducts] = useState<TopProduct[]>([])
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethodStat[]>([])
  const [waiterPerformance, setWaiterPerformance] = useState<WaiterPerformance[]>([])
  const [loading, setLoading] = useState(true)
  const [showArchive, setShowArchive] = useState(false)
  const [archiveHistory, setArchiveHistory] = useState<any[]>([])
  const { getArchiveHistory, archiveDay, isArchiving } = useDailyArchiveStore()

  useEffect(() => {
    loadReports()
  }, [period])

  useEffect(() => {
    if (showArchive) {
      loadArchiveHistory()
    }
  }, [showArchive])

  const loadArchiveHistory = async () => {
    const endDate = new Date().toISOString().split('T')[0]
    const startDate = new Date()
    startDate.setDate(startDate.getDate() - 30) // Son 30 gün
    const startDateStr = startDate.toISOString().split('T')[0]
    
    const history = await getArchiveHistory(startDateStr, endDate)
    setArchiveHistory(history)
  }

  const handleManualArchive = async () => {
    if (confirm('Dünün verilerini arşivlemek istediğinize emin misiniz?')) {
      try {
        const yesterday = new Date()
        yesterday.setDate(yesterday.getDate() - 1)
        const yesterdayStr = yesterday.toISOString().split('T')[0]
        
        await archiveDay(yesterdayStr)
        alert('Arşivleme başarıyla tamamlandı!')
        loadArchiveHistory()
      } catch (error) {
        alert('Arşivleme sırasında bir hata oluştu!')
      }
    }
  }

  const loadReports = async () => {
    try {
      setLoading(true)
      const range = getDateRange(period)

      await Promise.all([
        loadStats(range),
        loadTopProducts(range),
        loadPaymentMethods(range),
        loadWaiterPerformance(range)
      ])
    } catch (error) {
      console.error('Raporlar yüklenirken hata:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadStats = async (range: { start: Date; end: Date }) => {
    const startDate = range.start.toISOString().split('T')[0]
    const endDate = range.end.toISOString().split('T')[0]
    const today = new Date().toISOString().split('T')[0]

    // Aktif ödemeler (henüz arşivlenmemiş)
    const { data: activePayments } = await supabase
      .from('payments')
      .select('amount')
      .eq('organization_id', profile?.organization_id)
      .gte('created_at', range.start.toISOString())
      .lte('created_at', range.end.toISOString())

    // Aktif siparişler
    const { count: activeOrders } = await supabase
      .from('orders')
      .select('*', { count: 'exact', head: true })
      .eq('organization_id', profile?.organization_id)
      .eq('status', 'closed')
      .gte('closed_at', range.start.toISOString())
      .lte('closed_at', range.end.toISOString())

    const activeRevenue = activePayments?.reduce((sum, p) => sum + Number(p.amount), 0) || 0

    // Bugün için arşiv verisi ekleme (bugün henüz arşivlenmedi)
    let archivedRevenue = 0
    let archivedOrderCount = 0

    if (period !== 'today') {
      // Arşivlenmiş günlük özetler (bugün hariç)
      const { data: archivedSales } = await supabase
        .from('daily_sales_archive')
        .select('total_orders, total_revenue')
        .eq('organization_id', profile?.organization_id)
        .gte('business_date', startDate)
        .lt('business_date', today) // Bugünden önceki günler
        .lte('business_date', endDate)

      archivedRevenue = archivedSales?.reduce((sum, s) => sum + Number(s.total_revenue), 0) || 0
      archivedOrderCount = archivedSales?.reduce((sum, s) => sum + Number(s.total_orders), 0) || 0
    }

    // Toplamları hesapla
    const totalRevenue = activeRevenue + archivedRevenue
    const totalOrders = (activeOrders || 0) + archivedOrderCount
    const averageOrder = totalOrders > 0 ? totalRevenue / totalOrders : 0

    setStats({
      totalRevenue,
      totalOrders,
      averageOrder
    })
  }

  const loadTopProducts = async (range: { start: Date; end: Date }) => {
    const startDate = range.start.toISOString().split('T')[0]
    const endDate = range.end.toISOString().split('T')[0]
    const today = new Date().toISOString().split('T')[0]

    const productMap = new Map<string, TopProduct>()

    // Aktif sipariş kalemleri
    const { data: activeItems } = await supabase
      .from('order_items')
      .select('menu_item_id, quantity, total_price, menu_items(name)')
      .eq('organization_id', profile?.organization_id)
      .gte('created_at', range.start.toISOString())
      .lte('created_at', range.end.toISOString())
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

    // Arşivlenmiş sipariş kalemleri (bugün hariç)
    if (period !== 'today') {
      const { data: archivedItems } = await supabase
        .from('archived_order_items')
        .select('menu_item_name, quantity, total_price')
        .eq('organization_id', profile?.organization_id)
        .gte('business_date', startDate)
        .lt('business_date', today)
        .lte('business_date', endDate)

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
    }

    const sorted = Array.from(productMap.values())
      .sort((a, b) => b.quantity - a.quantity)
      .slice(0, 10)

    setTopProducts(sorted)
  }

  const loadPaymentMethods = async (range: { start: Date; end: Date }) => {
    const startDate = range.start.toISOString().split('T')[0]
    const endDate = range.end.toISOString().split('T')[0]
    const today = new Date().toISOString().split('T')[0]

    const methodMap = new Map<string, PaymentMethodStat>()

    // Aktif ödemeler
    const { data: activePayments } = await supabase
      .from('payments')
      .select('payment_method, amount')
      .eq('organization_id', profile?.organization_id)
      .gte('created_at', range.start.toISOString())
      .lte('created_at', range.end.toISOString())

    if (activePayments) {
      activePayments.forEach((payment: any) => {
        const method = payment.payment_method
        const existing = methodMap.get(method)
        
        if (existing) {
          existing.count += 1
          existing.amount += Number(payment.amount)
        } else {
          methodMap.set(method, {
            method,
            count: 1,
            amount: Number(payment.amount)
          })
        }
      })
    }

    // Arşivlenmiş ödemeler - günlük özetlerden (bugün hariç)
    if (period !== 'today') {
      const { data: archivedSales } = await supabase
        .from('daily_sales_archive')
        .select('total_cash, total_credit_card, total_online, total_other')
        .eq('organization_id', profile?.organization_id)
        .gte('business_date', startDate)
        .lt('business_date', today)
        .lte('business_date', endDate)

      if (archivedSales) {
        archivedSales.forEach((sale: any) => {
          // Nakit
          if (sale.total_cash > 0) {
            const existing = methodMap.get('cash')
            if (existing) {
              existing.count += 1
              existing.amount += Number(sale.total_cash)
            } else {
              methodMap.set('cash', {
                method: 'cash',
                count: 1,
                amount: Number(sale.total_cash)
              })
            }
          }

          // Kredi Kartı
          if (sale.total_credit_card > 0) {
            const existing = methodMap.get('credit_card')
            if (existing) {
              existing.count += 1
              existing.amount += Number(sale.total_credit_card)
            } else {
              methodMap.set('credit_card', {
                method: 'credit_card',
                count: 1,
                amount: Number(sale.total_credit_card)
              })
            }
          }

          // Online
          if (sale.total_online > 0) {
            const existing = methodMap.get('online')
            if (existing) {
              existing.count += 1
              existing.amount += Number(sale.total_online)
            } else {
              methodMap.set('online', {
                method: 'online',
                count: 1,
                amount: Number(sale.total_online)
              })
            }
          }

          // Diğer
          if (sale.total_other > 0) {
            const existing = methodMap.get('other')
            if (existing) {
              existing.count += 1
              existing.amount += Number(sale.total_other)
            } else {
              methodMap.set('other', {
                method: 'other',
                count: 1,
                amount: Number(sale.total_other)
              })
            }
          }
        })
      }
    }

    setPaymentMethods(Array.from(methodMap.values()))
  }

  const loadWaiterPerformance = async (range: { start: Date; end: Date }) => {
    const startDate = range.start.toISOString().split('T')[0]
    const endDate = range.end.toISOString().split('T')[0]
    const today = new Date().toISOString().split('T')[0]

    // Tüm kullanıcıları al (sadece kendi organizasyonundan)
    const { data: profiles } = await supabase
      .from('profiles')
      .select('id, full_name, role')
      .eq('organization_id', profile?.organization_id)

    if (!profiles) return

    const performance: WaiterPerformance[] = []

    for (const userProfile of profiles) {
      // Aktif siparişler - Açtığı adisyon sayısı
      const { count: activeOrdersOpened } = await supabase
        .from('orders')
        .select('*', { count: 'exact', head: true })
        .eq('organization_id', profile?.organization_id)
        .eq('opened_by_user_id', userProfile.id)
        .gte('opened_at', range.start.toISOString())
        .lte('opened_at', range.end.toISOString())

      // Aktif sipariş kalemleri
      const { count: activeItemsCreated } = await supabase
        .from('order_items')
        .select('*', { count: 'exact', head: true })
        .eq('organization_id', profile?.organization_id)
        .eq('created_by_user_id', userProfile.id)
        .gte('created_at', range.start.toISOString())
        .lte('created_at', range.end.toISOString())

      // Aktif siparişlerden toplam satış
      const { data: activeOrders } = await supabase
        .from('orders')
        .select('id')
        .eq('organization_id', profile?.organization_id)
        .eq('opened_by_user_id', userProfile.id)
        .eq('status', 'closed')
        .gte('opened_at', range.start.toISOString())
        .lte('opened_at', range.end.toISOString())

      let activeRevenue = 0
      if (activeOrders && activeOrders.length > 0) {
        const orderIds = activeOrders.map(o => o.id)
        const { data: payments } = await supabase
          .from('payments')
          .select('amount')
          .eq('organization_id', profile?.organization_id)
          .in('order_id', orderIds)

        activeRevenue = payments?.reduce((sum, p) => sum + Number(p.amount), 0) || 0
      }

      // Arşivlenmiş siparişler (bugün hariç)
      let archivedOrdersOpened = 0
      let archivedRevenue = 0

      if (period !== 'today') {
        const { count: archivedCount } = await supabase
          .from('archived_orders')
          .select('*', { count: 'exact', head: true })
          .eq('organization_id', profile?.organization_id)
          .eq('opened_by_user_id', userProfile.id)
          .gte('business_date', startDate)
          .lt('business_date', today)
          .lte('business_date', endDate)

        archivedOrdersOpened = archivedCount || 0

        const { data: archivedOrders } = await supabase
          .from('archived_orders')
          .select('total_amount')
          .eq('organization_id', profile?.organization_id)
          .eq('opened_by_user_id', userProfile.id)
          .gte('business_date', startDate)
          .lt('business_date', today)
          .lte('business_date', endDate)

        archivedRevenue = archivedOrders?.reduce((sum, o) => sum + Number(o.total_amount), 0) || 0
      }

      // Toplamlar
      const totalOrdersOpened = (activeOrdersOpened || 0) + archivedOrdersOpened
      const totalItemsCreated = activeItemsCreated || 0 // Arşivde created_by bilgisi yok
      const totalRevenue = activeRevenue + archivedRevenue
      const averageOrder = totalOrdersOpened > 0 ? totalRevenue / totalOrdersOpened : 0

      performance.push({
        name: userProfile.full_name,
        role: userProfile.role,
        ordersOpened: totalOrdersOpened,
        itemsCreated: totalItemsCreated,
        totalRevenue,
        averageOrder
      })
    }

    // Toplam satışa göre sırala
    performance.sort((a, b) => b.totalRevenue - a.totalRevenue)
    setWaiterPerformance(performance)
  }

  const getPeriodLabel = () => {
    const labels = {
      today: 'Bugün',
      week: 'Bu Hafta',
      month: 'Bu Ay',
      year: 'Bu Yıl'
    }
    return labels[period]
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
        <h1 className="page-title">Raporlar</h1>
        <p className="page-subtitle">Satış ve performans raporları</p>
      </div>

      {/* Filtreler */}
      <div className="card mb-4">
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
          <button
            onClick={() => setPeriod('today')}
            className={`btn ${period === 'today' ? 'btn-primary' : 'btn-secondary'}`}
          >
            <MdToday /> Bugün
          </button>
          <button
            onClick={() => setPeriod('week')}
            className={`btn ${period === 'week' ? 'btn-primary' : 'btn-secondary'}`}
          >
            <MdDateRange /> Bu Hafta
          </button>
          <button
            onClick={() => setPeriod('month')}
            className={`btn ${period === 'month' ? 'btn-primary' : 'btn-secondary'}`}
          >
            <MdCalendarMonth /> Bu Ay
          </button>
          <button
            onClick={() => setPeriod('year')}
            className={`btn ${period === 'year' ? 'btn-primary' : 'btn-secondary'}`}
          >
            <MdCalendarToday /> Bu Yıl
          </button>
        </div>
      </div>

      {/* Genel İstatistikler */}
      <div className="grid grid-3 mb-4">
        <div className="stat-card">
          <div className="stat-icon"><MdAttachMoney /></div>
          <div className="stat-content">
            <p className="stat-label">Toplam Ciro ({getPeriodLabel()})</p>
            <p className="stat-value">{formatCurrency(stats.totalRevenue)}</p>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon"><MdReceipt /></div>
          <div className="stat-content">
            <p className="stat-label">Toplam Sipariş</p>
            <p className="stat-value">{stats.totalOrders}</p>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon"><MdBarChart /></div>
          <div className="stat-content">
            <p className="stat-label">Ortalama Adisyon</p>
            <p className="stat-value">{formatCurrency(stats.averageOrder)}</p>
          </div>
        </div>
      </div>

      <div className="grid grid-2 mb-4">
        {/* En Çok Satan Ürünler */}
        <div className="card">
          <h2 className="card-title">En Çok Satan Ürünler</h2>
          {topProducts.length === 0 ? (
            <p className="text-secondary">Bu dönemde veri yok</p>
          ) : (
            <div className="table-container">
              <table className="table">
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Ürün</th>
                    <th>Adet</th>
                    <th>Ciro</th>
                  </tr>
                </thead>
                <tbody>
                  {topProducts.map((product, index) => (
                    <tr key={index}>
                      <td>
                        <div className="top-product-rank">{index + 1}</div>
                      </td>
                      <td>{product.name}</td>
                      <td>{product.quantity}</td>
                      <td>{formatCurrency(product.revenue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Ödeme Yöntemleri */}
        <div className="card">
          <h2 className="card-title">Ödeme Yöntemleri</h2>
          {paymentMethods.length === 0 ? (
            <p className="text-secondary">Bu dönemde veri yok</p>
          ) : (
            <div className="table-container">
              <table className="table">
                <thead>
                  <tr>
                    <th>Yöntem</th>
                    <th>İşlem Sayısı</th>
                    <th>Toplam</th>
                  </tr>
                </thead>
                <tbody>
                  {paymentMethods.map((method, index) => (
                    <tr key={index}>
                      <td>{getPaymentMethodLabel(method.method)}</td>
                      <td>{method.count}</td>
                      <td>{formatCurrency(method.amount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Garson Performansı */}
      <div className="card">
        <h2 className="card-title">Personel Performansı</h2>
        {waiterPerformance.length === 0 ? (
          <p className="text-secondary">Bu dönemde veri yok</p>
        ) : (
          <div className="table-container">
            <table className="table">
              <thead>
                <tr>
                  <th>Personel</th>
                  <th>Rol</th>
                  <th>Açılan Adisyon</th>
                  <th>Eklenen Ürün</th>
                  <th>Toplam Satış</th>
                  <th>Ort. Adisyon</th>
                </tr>
              </thead>
              <tbody>
                {waiterPerformance.map((waiter, index) => (
                  <tr key={index}>
                    <td style={{ fontWeight: 600 }}>{waiter.name}</td>
                    <td>{getRoleLabel(waiter.role)}</td>
                    <td>{waiter.ordersOpened}</td>
                    <td>{waiter.itemsCreated}</td>
                    <td style={{ fontWeight: 600, color: 'var(--primary)' }}>
                      {formatCurrency(waiter.totalRevenue)}
                    </td>
                    <td>{formatCurrency(waiter.averageOrder)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Günlük Arşiv */}
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 className="card-title" style={{ margin: 0 }}>
            <MdArchive style={{ marginRight: '8px' }} />
            Günlük Arşiv
          </h2>
          <div style={{ display: 'flex', gap: '8px' }}>
            <button
              onClick={handleManualArchive}
              className="btn btn-secondary"
              disabled={isArchiving}
            >
              {isArchiving ? 'Arşivleniyor...' : 'Manuel Arşivle'}
            </button>
            <button
              onClick={() => setShowArchive(!showArchive)}
              className="btn btn-primary"
            >
              {showArchive ? 'Gizle' : 'Geçmişi Göster'}
            </button>
          </div>
        </div>

        {showArchive && (
          <>
            <p className="text-secondary" style={{ marginBottom: '16px' }}>
              Sistem her gün otomatik olarak önceki günün verilerini arşivler. 
              Arşivlenen veriler silinir ve raporlarda görünmez hale gelir.
            </p>
            
            {archiveHistory.length === 0 ? (
              <p className="text-secondary">Henüz arşivlenmiş veri yok</p>
            ) : (
              <div className="table-container">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Tarih</th>
                      <th>Sipariş Sayısı</th>
                      <th>Toplam Ciro</th>
                      <th>Nakit</th>
                      <th>Kredi Kartı</th>
                      <th>Online</th>
                      <th>Diğer</th>
                      <th>Arşivlenme</th>
                    </tr>
                  </thead>
                  <tbody>
                    {archiveHistory.map((archive) => (
                      <tr key={archive.id}>
                        <td style={{ fontWeight: 600 }}>
                          {formatDate(archive.business_date)}
                        </td>
                        <td>{archive.total_orders}</td>
                        <td style={{ fontWeight: 600, color: 'var(--primary)' }}>
                          {formatCurrency(archive.total_revenue)}
                        </td>
                        <td>{formatCurrency(archive.total_cash)}</td>
                        <td>{formatCurrency(archive.total_credit_card)}</td>
                        <td>{formatCurrency(archive.total_online)}</td>
                        <td>{formatCurrency(archive.total_other)}</td>
                        <td className="text-secondary">
                          {formatDate(archive.archived_at)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
