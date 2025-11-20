import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { supabase, MenuItem, MenuCategory } from '@/lib/supabase'
import { useAuthStore } from '@/store/authStore'
import { formatCurrency, formatDateTime, getElapsedTime } from '@/lib/utils'
import { MdArrowBack, MdDelete, MdAdd, MdRemove, MdPrint, MdPayment, MdAccessTime, MdPerson } from 'react-icons/md'
import './Dashboard.css'

export default function Orders() {
  const { tableId } = useParams()
  const navigate = useNavigate()
  const { profile } = useAuthStore()
  
  const [categories, setCategories] = useState<MenuCategory[]>([])
  const [menuItems, setMenuItems] = useState<MenuItem[]>([])
  const [selectedCategory, setSelectedCategory] = useState<string>('all')
  const [currentOrder, setCurrentOrder] = useState<any>(null)
  const [orderItems, setOrderItems] = useState<any[]>([])
  const [tableName, setTableName] = useState('')
  const [loading, setLoading] = useState(true)
  const [showPaymentModal, setShowPaymentModal] = useState(false)
  const [paymentMethod, setPaymentMethod] = useState<'cash' | 'credit_card' | 'online' | 'other'>('cash')
  const [, setTick] = useState(0) // Süreyi güncellemek için
  const [printerSettings, setPrinterSettings] = useState<any>(null)

  useEffect(() => {
    if (tableId) {
      loadOrderData()
      loadPrinterSettings()

      // Realtime subscription - order_items ve orders değişikliklerini dinle
      console.log('Realtime subscription başlatılıyor...')
      const channel = supabase
        .channel(`table-${tableId}-realtime`)
        .on(
          'postgres_changes',
          {
            event: '*',
            schema: 'public',
            table: 'order_items',
            filter: `organization_id=eq.${profile?.organization_id}`
          },
          (payload) => {
            console.log('Order items değişikliği:', payload)
            loadOrderData() // Adisyon verilerini yeniden yükle
          }
        )
        .on(
          'postgres_changes',
          {
            event: '*',
            schema: 'public',
            table: 'orders',
            filter: `organization_id=eq.${profile?.organization_id}`
          },
          (payload) => {
            console.log('Orders değişikliği:', payload)
            loadOrderData() // Adisyon verilerini yeniden yükle
          }
        )
        .subscribe((status) => {
          console.log('Realtime subscription durumu:', status)
        })

      return () => {
        console.log('Realtime subscription kapatılıyor...')
        channel.unsubscribe()
      }
    }
  }, [tableId, profile?.organization_id])

  // Süreyi her dakika güncelle
  useEffect(() => {
    const interval = setInterval(() => {
      setTick(prev => prev + 1)
    }, 60000) // 60 saniye

    return () => clearInterval(interval)
  }, [])

  const loadPrinterSettings = async () => {
    try {
      const { data, error } = await supabase
        .from('organization_settings')
        .select('printer_settings')
        .eq('organization_id', profile?.organization_id)
        .single()

      if (error && error.code !== 'PGRST116') {
        console.error('Yazıcı ayarları yüklenirken hata:', error)
        return
      }

      if (data?.printer_settings) {
        setPrinterSettings(data.printer_settings)
        console.log('Yazıcı ayarları yüklendi:', data.printer_settings)
      }
    } catch (error) {
      console.error('Yazıcı ayarları yüklenirken hata:', error)
    }
  }

  const loadOrderData = async () => {
    try {
      setLoading(true)

      console.log('=== ADİSYON YÜKLEME BAŞLADI ===')
      console.log('Table ID:', tableId)

      // Masa bilgisini al
      const { data: tableData, error: tableError } = await supabase
        .from('restaurant_tables')
        .select('name')
        .eq('organization_id', profile?.organization_id)
        .eq('id', tableId)
        .single()

      if (tableError) {
        console.error('Masa bilgisi yüklenirken hata:', tableError)
      }

      console.log('Masa adı:', tableData?.name)
      setTableName(tableData?.name || '')

      // Açık adisyonu bul
      console.log('Açık adisyon aranıyor...')
      const { data: orderData, error: orderError } = await supabase
        .from('orders')
        .select('*')
        .eq('organization_id', profile?.organization_id)
        .eq('table_id', tableId)
        .eq('status', 'open')
        .single()

      if (orderError) {
        if (orderError.code === 'PGRST116') {
          console.log('Bu masada açık adisyon yok')
        } else {
          console.error('Adisyon yüklenirken hata:', orderError)
        }
      } else {
        console.log('Açık adisyon bulundu:', orderData)
      }

      // Açan kullanıcı bilgisini ayrı çek
      if (orderData && orderData.opened_by_user_id) {
        const { data: profileData, error: profileError } = await supabase
          .from('profiles')
          .select('full_name')
          .eq('id', orderData.opened_by_user_id)
          .single()

        if (profileError) {
          console.error('Profil bilgisi yüklenirken hata:', profileError)
        } else {
          console.log('Açan kullanıcı:', profileData?.full_name)
          orderData.profiles = profileData
        }
      }

      setCurrentOrder(orderData)

      // Adisyon kalemlerini yükle
      if (orderData) {
        console.log('Adisyon kalemleri yükleniyor...')
        const { data: itemsData, error: itemsError } = await supabase
          .from('order_items')
          .select('*, menu_items(name, price, category_id, menu_categories(name))')
          .eq('order_id', orderData.id)
          .neq('status', 'cancelled')

        if (itemsError) {
          console.error('Adisyon kalemleri yüklenirken hata:', itemsError)
        } else {
          console.log('Adisyon kalemleri:', itemsData?.length, 'adet')
        }

        setOrderItems(itemsData || [])
      } else {
        console.log('Adisyon olmadığı için kalemler yüklenmiyor')
      }

      // Kategorileri yükle
      const { data: categoriesData } = await supabase
        .from('menu_categories')
        .select('*')
        .eq('organization_id', profile?.organization_id)
        .order('sort_order')

      setCategories(categoriesData || [])

      // Menü ürünlerini yükle
      const { data: menuData } = await supabase
        .from('menu_items')
        .select('*')
        .eq('organization_id', profile?.organization_id)
        .eq('is_active', true)
        .order('name')

      setMenuItems(menuData || [])
      
      console.log('=== ADİSYON YÜKLEME TAMAMLANDI ===')
    } catch (error) {
      console.error('Adisyon verisi yüklenirken hata:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleAddItem = async (item: MenuItem) => {
    if (!currentOrder) return

    try {
      // Aynı üründen var mı kontrol et
      const existingItem = orderItems.find(oi => oi.menu_item_id === item.id && oi.status !== 'cancelled')

      let isNewItem = false

      if (existingItem) {
        // Miktarı artır
        const newQuantity = existingItem.quantity + 1
        const newTotal = item.price * newQuantity

        const { error } = await supabase
          .from('order_items')
          .update({
            quantity: newQuantity,
            total_price: newTotal
          })
          .eq('id', existingItem.id)

        if (error) throw error
      } else {
        // Yeni ekle
        isNewItem = true
        const { error: insertError } = await supabase
          .from('order_items')
          .insert({
            organization_id: profile?.organization_id,
            order_id: currentOrder.id,
            menu_item_id: item.id,
            quantity: 1,
            unit_price: item.price,
            total_price: item.price,
            created_by_user_id: profile?.id,
            status: 'pending'
          })

        if (insertError) throw insertError
      }

      await loadOrderData()

      // Otomatik yazdırma kontrolü
      if (isNewItem) {
        // İlk ürün mü? (Sipariş yeni oluşturuldu)
        const isFirstItem = orderItems.length === 0
        
        if (isFirstItem && printerSettings?.auto_print_kitchen) {
          // İlk ürün eklendiğinde tüm mutfak fişini yazdır
          console.log('İlk ürün eklendi, mutfak fişi otomatik yazdırılıyor...')
          setTimeout(() => handlePrintKitchen(), 500) // Veri yüklenmesi için kısa gecikme
        } else if (!isFirstItem && printerSettings?.auto_print_on_new_order) {
          // Sonraki ürünlerde sadece yeni ürünü yazdır
          console.log('Yeni ürün eklendi, otomatik yazdırılıyor...')
          await autoPrintNewItem(item)
        }
      }
    } catch (error) {
      console.error('Ürün eklenirken hata:', error)
      alert('Ürün eklenirken bir hata oluştu')
    }
  }

  const handleUpdateQuantity = async (orderItem: any, delta: number) => {
    const newQuantity = orderItem.quantity + delta

    if (newQuantity <= 0) {
      handleRemoveItem(orderItem)
      return
    }

    try {
      const newTotal = orderItem.unit_price * newQuantity

      const { error } = await supabase
        .from('order_items')
        .update({
          quantity: newQuantity,
          total_price: newTotal
        })
        .eq('id', orderItem.id)

      if (error) throw error

      loadOrderData()
    } catch (error) {
      console.error('Miktar güncellenirken hata:', error)
      alert('Miktar güncellenirken bir hata oluştu')
    }
  }

  const handleRemoveItem = async (orderItem: any) => {
    if (!confirm('Bu ürünü silmek istediğinize emin misiniz?')) return

    try {
      const { error } = await supabase
        .from('order_items')
        .update({ status: 'cancelled' })
        .eq('id', orderItem.id)

      if (error) throw error

      loadOrderData()
    } catch (error) {
      console.error('Ürün silinirken hata:', error)
      alert('Ürün silinirken bir hata oluştu')
    }
  }

  // Otomatik yazdırma: Yeni ürün eklendiğinde
  const autoPrintNewItem = async (item: MenuItem) => {
    const html = `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { font-family: monospace; width: 300px; margin: 0; padding: 10px; font-size: 14px; }
          h2 { text-align: center; margin: 10px 0; font-size: 18px; font-weight: bold; }
          hr { border: 1px dashed #000; margin: 10px 0; }
          .center { text-align: center; }
          .item { margin: 10px 0; font-size: 16px; font-weight: bold; }
        </style>
      </head>
      <body>
        <h2>YENİ ÜRÜN</h2>
        <hr>
        <p class="center"><strong>MASA: ${tableName}</strong></p>
        <hr>
        <div class="item">
          <p>1x ${item.name}</p>
        </div>
        <hr>
        <p class="center">${new Date().toLocaleString('tr-TR')}</p>
      </body>
      </html>
    `

    if (window.electron) {
      try {
        // Kategori bazlı yazıcı seçimi
        let printerName = printerSettings?.printer_name
        
        // Eğer bu ürünün kategorisi için özel yazıcı tanımlanmışsa onu kullan
        if (item.category_id && printerSettings?.category_printers?.[item.category_id]) {
          printerName = printerSettings.category_printers[item.category_id]
          console.log(`Kategori bazlı yazıcı kullanılıyor: ${printerName}`)
        }
        
        await window.electron.ipcRenderer.invoke('print-receipt', html, printerName)
        console.log('Yeni ürün yazdırıldı')
      } catch (error) {
        console.error('Yazdırma hatası:', error)
      }
    }
  }

  const handlePrintKitchen = async () => {
    if (window.electron && printerSettings?.category_printers && Object.keys(printerSettings.category_printers).length > 0) {
      // Kategori bazlı yazdırma: Her kategori için ayrı fiş
      try {
        // Ürünleri kategorilere göre grupla
        const itemsByCategory: { [categoryId: string]: any[] } = {}
        
        orderItems.forEach(orderItem => {
          const categoryId = orderItem.menu_items?.category_id || 'uncategorized'
          if (!itemsByCategory[categoryId]) {
            itemsByCategory[categoryId] = []
          }
          itemsByCategory[categoryId].push(orderItem)
        })

        // Her kategori için ayrı fiş yazdır
        for (const [categoryId, items] of Object.entries(itemsByCategory)) {
          const html = generateKitchenReceiptForCategory(items, categoryId)
          const printerName = printerSettings.category_printers[categoryId] || printerSettings.printer_name
          
          console.log(`Kategori ${categoryId} için yazdırılıyor, yazıcı: ${printerName}`)
          await window.electron.ipcRenderer.invoke('print-receipt', html, printerName)
          
          // Yazıcılar arası kısa gecikme
          await new Promise(resolve => setTimeout(resolve, 500))
        }
      } catch (error) {
        console.error('Kategori bazlı yazdırma hatası:', error)
        alert('Yazdırma sırasında bir hata oluştu')
      }
    } else {
      // Normal yazdırma: Tek fiş
      const html = generateKitchenReceipt()
      if (window.electron) {
        try {
          const printerName = printerSettings?.printer_name
          window.electron.ipcRenderer.invoke('print-receipt', html, printerName)
        } catch (error) {
          console.error('Yazıcı ayarları alınamadı:', error)
          window.electron.ipcRenderer.invoke('print-receipt', html)
        }
      } else {
        const printWindow = window.open('', '', 'width=300,height=600')
        if (printWindow) {
          printWindow.document.write(html)
          printWindow.document.close()
          printWindow.print()
        }
      }
    }
  }

  const handlePrintBill = async () => {
    const html = generateBillReceipt()
    if (window.electron) {
      // Seçili yazıcıyı ayarlardan al
      try {
        const { data } = await supabase
          .from('organization_settings')
          .select('printer_settings')
          .eq('organization_id', profile?.organization_id)
          .single()
        
        const printerName = data?.printer_settings?.printer_name
        window.electron.ipcRenderer.invoke('print-receipt', html, printerName)
      } catch (error) {
        console.error('Yazıcı ayarları alınamadı:', error)
        window.electron.ipcRenderer.invoke('print-receipt', html)
      }
    } else {
      const printWindow = window.open('', '', 'width=300,height=600')
      if (printWindow) {
        printWindow.document.write(html)
        printWindow.document.close()
        printWindow.print()
      }
    }
  }

  const handleDeleteOrder = async () => {
    if (!currentOrder) return

    if (!confirm('Bu siparişi tamamen silmek istediğinize emin misiniz? Bu işlem geri alınamaz!')) return

    try {
      // 1. Önce sipariş kalemlerini sil
      const { error: itemsError } = await supabase
        .from('order_items')
        .delete()
        .eq('order_id', currentOrder.id)

      if (itemsError) throw itemsError

      // 2. Ödemeleri sil (varsa)
      const { error: paymentsError } = await supabase
        .from('payments')
        .delete()
        .eq('order_id', currentOrder.id)

      if (paymentsError) throw paymentsError

      // 3. Siparişi sil
      const { error: orderError } = await supabase
        .from('orders')
        .delete()
        .eq('id', currentOrder.id)

      if (orderError) throw orderError

      alert('Sipariş başarıyla silindi')
      navigate('/tables')
    } catch (error) {
      console.error('Sipariş silinirken hata:', error)
      alert('Sipariş silinirken bir hata oluştu')
    }
  }

  const handleCloseOrder = () => {
    if (orderItems.length === 0) {
      alert('Adisyonda ürün yok')
      return
    }
    setShowPaymentModal(true)
  }

  const handlePayment = async () => {
    if (!currentOrder) return

    try {
      const total = calculateTotal()

      // Ödeme kaydı oluştur
      const { error: paymentError } = await supabase
        .from('payments')
        .insert({
          organization_id: profile?.organization_id,
          order_id: currentOrder.id,
          amount: total,
          payment_method: paymentMethod,
          created_by_user_id: profile?.id
        })

      if (paymentError) throw paymentError

      // Adisyonu kapat
      const { error: orderError } = await supabase
        .from('orders')
        .update({
          status: 'closed',
          closed_at: new Date().toISOString(),
          closed_by_user_id: profile?.id
        })
        .eq('id', currentOrder.id)

      if (orderError) throw orderError

      alert('Ödeme alındı ve adisyon kapatıldı')
      navigate('/tables')
    } catch (error) {
      console.error('Ödeme alınırken hata:', error)
      alert('Ödeme alınırken bir hata oluştu')
    }
  }

  const calculateTotal = () => {
    return orderItems.reduce((sum, item) => sum + Number(item.total_price), 0)
  }

  const generateKitchenReceiptForCategory = (items: any[], _categoryId: string) => {
    // Kategori adını bul
    const categoryName = items[0]?.menu_items?.menu_categories?.name || 'Diğer'
    
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { font-family: monospace; width: 300px; margin: 20px; }
          h2 { text-align: center; margin: 10px 0; }
          .category { text-align: center; font-weight: bold; font-size: 16px; margin: 10px 0; }
          .item { margin: 5px 0; }
          hr { border: 1px dashed #000; }
        </style>
      </head>
      <body>
        <h2>MUTFAK FİŞİ</h2>
        <div class="category">${categoryName}</div>
        <hr>
        <p><strong>Masa:</strong> ${tableName}</p>
        <p><strong>Tarih:</strong> ${formatDateTime(new Date())}</p>
        <hr>
        ${items.map(item => `
          <div class="item">
            <strong>${item.quantity}x</strong> ${item.menu_items?.name}
          </div>
        `).join('')}
        <hr>
      </body>
      </html>
    `
  }

  const generateKitchenReceipt = () => {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { font-family: monospace; width: 300px; margin: 20px; }
          h2 { text-align: center; margin: 10px 0; }
          .item { margin: 5px 0; }
          hr { border: 1px dashed #000; }
        </style>
      </head>
      <body>
        <h2>MUTFAK FİŞİ</h2>
        <hr>
        <p><strong>Masa:</strong> ${tableName}</p>
        <p><strong>Tarih:</strong> ${formatDateTime(new Date())}</p>
        <hr>
        ${orderItems.map(item => `
          <div class="item">
            <strong>${item.quantity}x</strong> ${item.menu_items?.name}
          </div>
        `).join('')}
        <hr>
      </body>
      </html>
    `
  }

  const generateBillReceipt = () => {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { font-family: monospace; width: 300px; margin: 20px; }
          h2 { text-align: center; margin: 10px 0; }
          .item { display: flex; justify-content: space-between; margin: 5px 0; }
          .total { font-weight: bold; font-size: 1.2em; }
          hr { border: 1px dashed #000; }
        </style>
      </head>
      <body>
        <h2>HESAP FİŞİ</h2>
        <hr>
        <p><strong>Masa:</strong> ${tableName}</p>
        <p><strong>Tarih:</strong> ${formatDateTime(new Date())}</p>
        <hr>
        ${orderItems.map(item => `
          <div class="item">
            <span>${item.quantity}x ${item.menu_items?.name}</span>
            <span>${formatCurrency(item.total_price)}</span>
          </div>
        `).join('')}
        <hr>
        <div class="item total">
          <span>TOPLAM:</span>
          <span>${formatCurrency(calculateTotal())}</span>
        </div>
        <hr>
        <p style="text-align: center;">Teşekkür ederiz!</p>
      </body>
      </html>
    `
  }

  const filteredItems = selectedCategory === 'all'
    ? menuItems
    : menuItems.filter(item => item.category_id === selectedCategory)

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

  if (!currentOrder) {
    return (
      <div className="page-container">
        <div className="card">
          <h2>Bu masada açık adisyon yok</h2>
          <button onClick={() => navigate('/tables')} className="btn btn-primary mt-2">
            Masalara Dön
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="page-container">
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 className="page-title">{tableName} - Adisyon</h1>
          <div style={{ display: 'flex', gap: '24px', flexWrap: 'wrap', marginTop: '8px' }}>
            <p className="page-subtitle" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <MdAccessTime /> Açılış: {formatDateTime(currentOrder.opened_at)}
            </p>
            <p className="page-subtitle" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <MdAccessTime /> Süre: {getElapsedTime(currentOrder.opened_at)}
            </p>
            {currentOrder.profiles && (
              <p className="page-subtitle" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                <MdPerson /> Açan: {currentOrder.profiles.full_name}
              </p>
            )}
          </div>
        </div>
        <button onClick={() => navigate('/tables')} className="btn btn-secondary">
          <MdArrowBack /> Masalara Dön
        </button>
      </div>

      <div className="grid grid-2" style={{ gap: '2rem', alignItems: 'start' }}>
        {/* Sol taraf - Menü */}
        <div>
          <div className="card mb-2">
            <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
              <button
                onClick={() => setSelectedCategory('all')}
                className={`btn btn-sm ${selectedCategory === 'all' ? 'btn-primary' : 'btn-secondary'}`}
              >
                Tümü
              </button>
              {categories.map(cat => (
                <button
                  key={cat.id}
                  onClick={() => setSelectedCategory(cat.id)}
                  className={`btn btn-sm ${selectedCategory === cat.id ? 'btn-primary' : 'btn-secondary'}`}
                >
                  {cat.name}
                </button>
              ))}
            </div>
          </div>

          <div className="grid grid-2">
            {filteredItems.map(item => (
              <div
                key={item.id}
                className="card"
                style={{ cursor: 'pointer' }}
                onClick={() => handleAddItem(item)}
              >
                <h3 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '0.5rem' }}>
                  {item.name}
                </h3>
                {item.description && (
                  <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '0.5rem' }}>
                    {item.description}
                  </p>
                )}
                <p style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--primary)' }}>
                  {formatCurrency(item.price)}
                </p>
              </div>
            ))}
          </div>
        </div>

        {/* Sağ taraf - Adisyon */}
        <div className="card" style={{ position: 'sticky', top: '2rem' }}>
          <h2 className="card-title">Adisyon Detayı</h2>

          {orderItems.length === 0 ? (
            <p className="text-secondary">Henüz ürün eklenmedi</p>
          ) : (
            <>
              <div style={{ marginBottom: '1rem' }}>
                {orderItems.map(item => (
                  <div
                    key={item.id}
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      padding: '0.75rem',
                      background: 'var(--bg-hover)',
                      borderRadius: '0.5rem',
                      marginBottom: '0.5rem'
                    }}
                  >
                    <div style={{ flex: 1 }}>
                      <p style={{ fontWeight: 600 }}>{item.menu_items?.name}</p>
                      <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                        {formatCurrency(item.unit_price)} x {item.quantity}
                      </p>
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <button
                        onClick={() => handleUpdateQuantity(item, -1)}
                        className="btn btn-sm btn-secondary"
                        style={{ padding: '4px 8px' }}
                      >
                        <MdRemove />
                      </button>
                      <span style={{ fontWeight: 600, minWidth: '2rem', textAlign: 'center' }}>
                        {item.quantity}
                      </span>
                      <button
                        onClick={() => handleUpdateQuantity(item, 1)}
                        className="btn btn-sm btn-secondary"
                        style={{ padding: '4px 8px' }}
                      >
                        <MdAdd />
                      </button>
                      <button
                        onClick={() => handleRemoveItem(item)}
                        className="btn btn-sm btn-danger"
                        style={{ padding: '4px 8px' }}
                      >
                        <MdDelete />
                      </button>
                    </div>
                    <div style={{ minWidth: '80px', textAlign: 'right', fontWeight: 700 }}>
                      {formatCurrency(item.total_price)}
                    </div>
                  </div>
                ))}
              </div>

              <hr style={{ border: 'none', borderTop: '1px solid var(--border)', margin: '1rem 0' }} />

              <div style={{ marginBottom: '1rem' }}>
                <div style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  fontSize: '1.5rem',
                  fontWeight: 700,
                  color: 'var(--primary)'
                }}>
                  <span>TOPLAM:</span>
                  <span>{formatCurrency(calculateTotal())}</span>
                </div>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <button onClick={handlePrintKitchen} className="btn btn-secondary">
                  <MdPrint /> Mutfak Fişi Yazdır
                </button>
                <button onClick={handlePrintBill} className="btn btn-secondary">
                  <MdPrint /> Hesap Fişi Yazdır
                </button>
                <button onClick={handleCloseOrder} className="btn btn-success">
                  <MdPayment /> Ödeme Al ve Kapat
                </button>
                <button onClick={handleDeleteOrder} className="btn btn-danger">
                  <MdDelete /> Siparişi Sil
                </button>
              </div>
            </>
          )}
        </div>
      </div>

      {showPaymentModal && (
        <div className="modal-overlay" onClick={() => setShowPaymentModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">Ödeme Al</h2>
              <button onClick={() => setShowPaymentModal(false)} className="modal-close">×</button>
            </div>

            <div style={{ marginBottom: '1.5rem' }}>
              <p style={{ fontSize: '1.25rem', marginBottom: '0.5rem' }}>Toplam Tutar:</p>
              <p style={{ fontSize: '2rem', fontWeight: 700, color: 'var(--primary)' }}>
                {formatCurrency(calculateTotal())}
              </p>
            </div>

            <div className="form-group">
              <label className="form-label">Ödeme Yöntemi</label>
              <select
                className="form-select"
                value={paymentMethod}
                onChange={(e) => setPaymentMethod(e.target.value as any)}
              >
                <option value="cash">Nakit</option>
                <option value="credit_card">Kredi Kartı</option>
                <option value="online">Online</option>
                <option value="other">Diğer</option>
              </select>
            </div>

            <div className="modal-footer">
              <button onClick={() => setShowPaymentModal(false)} className="btn btn-secondary">
                İptal
              </button>
              <button onClick={handlePayment} className="btn btn-success">
                Ödemeyi Tamamla
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
