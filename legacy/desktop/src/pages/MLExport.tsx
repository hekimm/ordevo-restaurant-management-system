import { useState } from 'react'
import { useMLExport } from '@/hooks/useMLExport'
import { MdDownload, MdDateRange, MdTableChart, MdShoppingCart, MdInfo } from 'react-icons/md'
import './Dashboard.css'

export default function MLExport() {
  const { loading, error, exportMasterCSV, exportProductsCSV } = useMLExport()
  const [selectedDate, setSelectedDate] = useState(() => {
    const today = new Date()
    return today.toISOString().split('T')[0]
  })
  const [bucketMinutes, setBucketMinutes] = useState(60)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const handleExportMaster = async () => {
    setSuccessMessage(null)
    const result = await exportMasterCSV(selectedDate, bucketMinutes)

    if (result.success) {
      setSuccessMessage(
        `Master CSV başarıyla dışa aktarıldı! (${result.rowCount} satır)\nDosya: ${result.filePath}`
      )
    }
  }

  const handleExportProducts = async () => {
    setSuccessMessage(null)
    const result = await exportProductsCSV(selectedDate, bucketMinutes)

    if (result.success) {
      setSuccessMessage(
        `Ürün CSV başarıyla dışa aktarıldı! (${result.rowCount} satır)\nDosya: ${result.filePath}`
      )
    }
  }

  return (
    <div className="page-container">
      <div className="page-header">
        <h1 className="page-title">
          <MdTableChart style={{ verticalAlign: 'middle', marginRight: '8px' }} />
          ML Veri Dışa Aktarma
        </h1>
        <p className="page-subtitle">
          Makine öğrenmesi analizi için satış ve hava durumu verilerini CSV formatında dışa aktarın
        </p>
      </div>

      {/* Info Card */}
      <div className="card mb-4" style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', color: 'white' }}>
        <div style={{ display: 'flex', alignItems: 'flex-start', gap: '16px' }}>
          <MdInfo style={{ fontSize: '32px', flexShrink: 0, marginTop: '4px' }} />
          <div>
            <h3 style={{ margin: '0 0 8px 0', fontSize: '1.1rem' }}>ML Analizi İçin Veri Hazırlama</h3>
            <p style={{ margin: 0, opacity: 0.9, lineHeight: '1.6' }}>
              Bu araç, satış tahminleri, ürün talebi analizi ve masa doluluk tahminleri gibi makine öğrenmesi 
              projeleri için gerekli verileri hazırlar. Veriler zaman dilimlerine (time buckets) bölünerek 
              hava durumu bilgileriyle birleştirilir.
            </p>
          </div>
        </div>
      </div>

      <div className="grid grid-2" style={{ gap: '2rem', alignItems: 'start' }}>
        {/* Settings Card */}
        <div className="card">
          <h2 className="card-title">Export Ayarları</h2>

          <div className="form-group">
            <label className="form-label">
              <MdDateRange style={{ verticalAlign: 'middle', marginRight: '4px' }} />
              Tarih Seç
            </label>
            <input
              type="date"
              className="form-input"
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              max={new Date().toISOString().split('T')[0]}
            />
            <p className="form-help">
              Dışa aktarılacak günü seçin. Geçmiş tarihler için veri mevcutsa export edilir.
            </p>
          </div>

          <div className="form-group">
            <label className="form-label">Zaman Dilimi (Time Bucket)</label>
            <select
              className="form-select"
              value={bucketMinutes}
              onChange={(e) => setBucketMinutes(Number(e.target.value))}
            >
              <option value={30}>30 Dakika</option>
              <option value={60}>60 Dakika (Önerilen)</option>
              <option value={120}>120 Dakika</option>
            </select>
            <p className="form-help">
              Veriler bu süre dilimlerine bölünerek aggregate edilir. 60 dakika çoğu ML modeli için idealdir.
            </p>
          </div>

          {error && (
            <div style={{
              padding: '12px',
              background: 'rgba(239, 68, 68, 0.1)',
              border: '1px solid rgba(239, 68, 68, 0.3)',
              borderRadius: '8px',
              color: '#EF4444',
              marginTop: '16px'
            }}>
              <strong>Hata:</strong> {error}
            </div>
          )}

          {successMessage && (
            <div style={{
              padding: '12px',
              background: 'rgba(34, 197, 94, 0.1)',
              border: '1px solid rgba(34, 197, 94, 0.3)',
              borderRadius: '8px',
              color: '#22C55E',
              marginTop: '16px',
              whiteSpace: 'pre-line'
            }}>
              <strong>Başarılı!</strong><br />
              {successMessage}
            </div>
          )}
        </div>

        {/* Export Actions Card */}
        <div className="card">
          <h2 className="card-title">Dışa Aktarma İşlemleri</h2>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            {/* Master CSV */}
            <div style={{
              padding: '20px',
              background: 'var(--bg-hover)',
              borderRadius: '12px',
              border: '2px solid var(--border)'
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '12px' }}>
                <MdTableChart style={{ fontSize: '32px', color: 'var(--primary)' }} />
                <div>
                  <h3 style={{ margin: 0, fontSize: '1.1rem' }}>Master CSV</h3>
                  <p style={{ margin: '4px 0 0 0', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                    Satış metrikleri + Hava durumu
                  </p>
                </div>
              </div>

              <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '16px' }}>
                Her satır bir zaman dilimini temsil eder. Satış metrikleri (ciro, sipariş sayısı, masa doluluk) 
                ve hava durumu bilgileri içerir.
              </p>

              <button
                onClick={handleExportMaster}
                disabled={loading}
                className="btn btn-primary"
                style={{ width: '100%' }}
              >
                <MdDownload style={{ marginRight: '8px' }} />
                {loading ? 'Dışa Aktarılıyor...' : 'Master CSV İndir'}
              </button>

              <div style={{ marginTop: '12px', fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                <strong>Kolonlar:</strong> date, time_bucket, total_orders, total_revenue, 
                avg_order_value, temp_c, humidity, weather_main, is_rain, is_weekend, vb.
              </div>
            </div>

            {/* Products CSV */}
            <div style={{
              padding: '20px',
              background: 'var(--bg-hover)',
              borderRadius: '12px',
              border: '2px solid var(--border)'
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '12px' }}>
                <MdShoppingCart style={{ fontSize: '32px', color: '#10B981' }} />
                <div>
                  <h3 style={{ margin: 0, fontSize: '1.1rem' }}>Ürün Bazlı CSV</h3>
                  <p style={{ margin: '4px 0 0 0', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                    Ürün satış detayları
                  </p>
                </div>
              </div>

              <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '16px' }}>
                Her satır bir ürünün belirli zaman dilimindeki satış performansını gösterir. 
                Ürün bazlı talep tahmini için kullanılır.
              </p>

              <button
                onClick={handleExportProducts}
                disabled={loading}
                className="btn btn-success"
                style={{ width: '100%' }}
              >
                <MdDownload style={{ marginRight: '8px' }} />
                {loading ? 'Dışa Aktarılıyor...' : 'Ürün CSV İndir'}
              </button>

              <div style={{ marginTop: '12px', fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                <strong>Kolonlar:</strong> date, time_bucket, product_id, product_name, 
                category, qty_sold, revenue
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Usage Guide */}
      <div className="card mt-4">
        <h2 className="card-title">Kullanım Rehberi</h2>
        
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '20px' }}>
          <div>
            <h3 style={{ fontSize: '1rem', marginBottom: '8px', color: 'var(--primary)' }}>
              1. Tarih Seçin
            </h3>
            <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', margin: 0 }}>
              Analiz etmek istediğiniz günü seçin. Birden fazla gün için ayrı ayrı export yapabilirsiniz.
            </p>
          </div>

          <div>
            <h3 style={{ fontSize: '1rem', marginBottom: '8px', color: 'var(--primary)' }}>
              2. CSV'leri İndirin
            </h3>
            <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', margin: 0 }}>
              Master CSV ve Ürün CSV'lerini indirin. Dosyalar otomatik olarak tarih ile isimlendirilir.
            </p>
          </div>

          <div>
            <h3 style={{ fontSize: '1rem', marginBottom: '8px', color: 'var(--primary)' }}>
              3. ML Modelinizde Kullanın
            </h3>
            <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', margin: 0 }}>
              Python, R veya diğer ML araçlarında bu CSV'leri kullanarak tahmin modelleri oluşturun.
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
