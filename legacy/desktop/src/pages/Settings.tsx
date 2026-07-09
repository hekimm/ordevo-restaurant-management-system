import { useEffect, useState } from 'react'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/store/authStore'
import { MdSave, MdPrint, MdSettings } from 'react-icons/md'
import './Dashboard.css'

interface PrinterSettings {
  auto_print_kitchen: boolean
  auto_print_on_new_order: boolean
  printer_type: 'usb' | 'network' | 'bluetooth'
  printer_ip?: string
  printer_name?: string
  category_printers?: { [categoryId: string]: string }
}

export default function Settings() {
  const { profile } = useAuthStore()
  const [settings, setSettings] = useState<PrinterSettings>({
    auto_print_kitchen: false,
    auto_print_on_new_order: false,
    printer_type: 'usb',
  })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [printers, setPrinters] = useState<string[]>([])
  const [categories, setCategories] = useState<any[]>([])

  useEffect(() => {
    if (profile?.organization_id) {
      loadSettings()
      loadPrinters()
      loadCategories()
    }
  }, [profile?.organization_id])

  const loadSettings = async () => {
    try {
      const { data, error } = await supabase
        .from('organization_settings')
        .select('printer_settings')
        .eq('organization_id', profile?.organization_id)
        .single()

      if (error && error.code !== 'PGRST116') throw error

      if (data?.printer_settings) {
        setSettings(data.printer_settings)
      }
    } catch (error) {
      console.error('Ayarlar yüklenirken hata:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadPrinters = async () => {
    if (window.electron) {
      try {
        const printerList = await window.electron.ipcRenderer.invoke('get-printers')
        setPrinters(printerList || [])
      } catch (error) {
        console.error('Yazıcılar yüklenirken hata:', error)
      }
    }
  }

  const loadCategories = async () => {
    try {
      console.log('[Settings] Loading categories for org:', profile?.organization_id)
      const { data, error } = await supabase
        .from('menu_categories')
        .select('id, name')
        .eq('organization_id', profile?.organization_id)
        .order('name')

      if (error) throw error
      console.log('[Settings] Categories loaded:', data?.length, 'categories')
      setCategories(data || [])
    } catch (error) {
      console.error('Kategoriler yüklenirken hata:', error)
    }
  }

  const handleSave = async () => {
    try {
      setSaving(true)

      if (!profile?.organization_id) {
        throw new Error('Organization ID bulunamadı')
      }

      console.log('Saving settings:', {
        organization_id: profile.organization_id,
        printer_settings: settings
      })

      const { data, error } = await supabase
        .from('organization_settings')
        .upsert({
          organization_id: profile.organization_id,
          printer_settings: settings,
          updated_at: new Date().toISOString(),
        }, {
          onConflict: 'organization_id'
        })
        .select()

      if (error) {
        console.error('Supabase error:', error)
        throw error
      }

      console.log('Settings saved successfully:', data)
      alert('Ayarlar başarıyla kaydedildi!')
    } catch (error: any) {
      console.error('Ayarlar kaydedilirken hata:', error)
      alert('Ayarlar kaydedilirken bir hata oluştu: ' + (error.message || error))
    } finally {
      setSaving(false)
    }
  }

  const handleTestPrint = async () => {
    const html = `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { 
            font-family: monospace; 
            width: 300px; 
            margin: 20px;
            font-size: 14px;
          }
          h2 { 
            text-align: center; 
            margin: 10px 0;
            font-size: 18px;
          }
          hr { 
            border: 1px dashed #000; 
            margin: 10px 0;
          }
          .center { text-align: center; }
        </style>
      </head>
      <body>
        <h2>TEST YAZDIR</h2>
        <hr>
        <p class="center">Bu bir test yazdırmasıdır</p>
        <p class="center">${new Date().toLocaleString('tr-TR')}</p>
        <hr>
        <p class="center">Yazıcı çalışıyor ✓</p>
      </body>
      </html>
    `

    if (window.electron) {
      const result = await window.electron.ipcRenderer.invoke('print-receipt', html, settings.printer_name)
      if (result.success) {
        alert('Yazdırma başarılı!')
      } else {
        alert('Yazdırma hatası: ' + result.error)
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
        <h1 className="page-title">
          <MdSettings /> Yazıcı Ayarları
        </h1>
        <p className="page-subtitle">Mutfak fişi yazdırma ayarlarını yapılandırın</p>
      </div>

      <div className="card" style={{ maxWidth: '800px' }}>
        <h2 className="card-title">Otomatik Yazdırma</h2>

        <div className="form-group">
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={settings.auto_print_kitchen}
              onChange={(e) =>
                setSettings({ ...settings, auto_print_kitchen: e.target.checked })
              }
            />
            <span>Mutfak fişini otomatik yazdır</span>
          </label>
          <p className="form-help">
            Sipariş oluşturulduğunda mutfak fişi otomatik olarak yazdırılır
          </p>
        </div>

        <div className="form-group">
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={settings.auto_print_on_new_order}
              onChange={(e) =>
                setSettings({ ...settings, auto_print_on_new_order: e.target.checked })
              }
            />
            <span>Yeni ürün eklendiğinde yazdır</span>
          </label>
          <p className="form-help">
            Mevcut siparişe yeni ürün eklendiğinde sadece yeni ürünleri yazdırır
          </p>
        </div>

        <hr style={{ margin: '2rem 0', border: 'none', borderTop: '1px solid var(--border)' }} />

        <h2 className="card-title">Yazıcı Yapılandırması</h2>

        <div className="form-group">
          <label className="form-label">Yazıcı Tipi</label>
          <select
            className="form-select"
            value={settings.printer_type}
            onChange={(e) =>
              setSettings({ ...settings, printer_type: e.target.value as any })
            }
          >
            <option value="usb">USB Yazıcı</option>
            <option value="network">Ağ Yazıcısı (IP)</option>
            <option value="bluetooth">Bluetooth Yazıcı</option>
          </select>
        </div>

        {settings.printer_type === 'usb' && printers.length > 0 && (
          <div className="form-group">
            <label className="form-label">Yazıcı Seç</label>
            <select
              className="form-select"
              value={settings.printer_name || ''}
              onChange={(e) =>
                setSettings({ ...settings, printer_name: e.target.value })
              }
            >
              <option value="">Varsayılan yazıcı</option>
              {printers.map((printer) => (
                <option key={printer} value={printer}>
                  {printer}
                </option>
              ))}
            </select>
          </div>
        )}

        {settings.printer_type === 'network' && (
          <div className="form-group">
            <label className="form-label">Yazıcı IP Adresi</label>
            <input
              type="text"
              className="form-input"
              value={settings.printer_ip || ''}
              onChange={(e) =>
                setSettings({ ...settings, printer_ip: e.target.value })
              }
              placeholder="192.168.1.100"
            />
            <p className="form-help">
              Yazıcının ağdaki IP adresini girin (örn: 192.168.1.100)
            </p>
          </div>
        )}

        {/* Kategori Bazlı Yazıcı Seçimi */}
        {settings.printer_type === 'usb' && printers.length > 0 && (
          <>
            <hr style={{ margin: '2rem 0', border: 'none', borderTop: '1px solid var(--border)' }} />
            <h3 style={{ fontSize: '1.1rem', marginBottom: '1rem' }}>
              Kategori Bazlı Yazıcı Ayarları
            </h3>
            <p style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
              Belirli kategorilerdeki ürünleri farklı yazıcılara yönlendirebilirsiniz.
              Örneğin: Mezeler bir yazıcıda, ana yemekler başka bir yazıcıda yazdırılabilir.
            </p>

            {categories.length === 0 && (
              <p style={{ color: 'var(--text-secondary)', fontStyle: 'italic' }}>
                Kategoriler yükleniyor... (Toplam: {categories.length})
              </p>
            )}

            {categories.map((category) => (
              <div key={category.id} className="form-group">
                <label className="form-label">{category.name}</label>
                <select
                  className="form-select"
                  value={(settings.category_printers && settings.category_printers[category.id]) || ''}
                  onChange={(e) => {
                    const newCategoryPrinters: { [key: string]: string } = {
                      ...(settings.category_printers || {}),
                      [category.id]: e.target.value
                    }
                    // Boş değerleri temizle
                    if (!e.target.value) {
                      delete newCategoryPrinters[category.id]
                    }
                    setSettings({
                      ...settings,
                      category_printers: newCategoryPrinters
                    })
                  }}
                >
                  <option value="">Varsayılan yazıcı kullan</option>
                  {printers.map((printer) => (
                    <option key={printer} value={printer}>
                      {printer}
                    </option>
                  ))}
                </select>
              </div>
            ))}
          </>
        )}

        <div style={{ display: 'flex', gap: '1rem', marginTop: '2rem' }}>
          <button
            onClick={handleSave}
            disabled={saving}
            className="btn btn-primary"
          >
            <MdSave /> {saving ? 'Kaydediliyor...' : 'Kaydet'}
          </button>

          <button onClick={handleTestPrint} className="btn btn-secondary">
            <MdPrint /> Test Yazdır
          </button>
        </div>
      </div>

      <div className="card" style={{ maxWidth: '800px', marginTop: '2rem' }}>
        <h2 className="card-title">Yazıcı Kurulum Rehberi</h2>
        
        <div style={{ lineHeight: '1.8' }}>
          <h3 style={{ fontSize: '1.1rem', marginTop: '1rem', marginBottom: '0.5rem' }}>
            USB Yazıcı
          </h3>
          <ol style={{ paddingLeft: '1.5rem' }}>
            <li>Yazıcıyı bilgisayara USB ile bağlayın</li>
            <li>Windows otomatik driver kuracak</li>
            <li>Yukarıdaki listeden yazıcınızı seçin</li>
            <li>"Test Yazdır" ile kontrol edin</li>
          </ol>

          <h3 style={{ fontSize: '1.1rem', marginTop: '1.5rem', marginBottom: '0.5rem' }}>
            Ağ Yazıcısı (Ethernet/WiFi)
          </h3>
          <ol style={{ paddingLeft: '1.5rem' }}>
            <li>Yazıcıyı router'a bağlayın</li>
            <li>Yazıcının IP adresini öğrenin (test sayfası yazdırın)</li>
            <li>IP adresini yukarıdaki alana girin</li>
            <li>"Test Yazdır" ile kontrol edin</li>
          </ol>

          <div
            style={{
              marginTop: '1.5rem',
              padding: '1rem',
              background: 'var(--bg-hover)',
              borderRadius: '0.5rem',
            }}
          >
            <p style={{ fontWeight: 600, marginBottom: '0.5rem' }}>
              💡 İpucu
            </p>
            <p style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
              Detaylı kurulum rehberi için <strong>MUTFAK-YAZICI-KURULUM.md</strong> dosyasına bakın.
              Önerilen yazıcı modelleri ve fiyatları hakkında bilgi bulabilirsiniz.
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
