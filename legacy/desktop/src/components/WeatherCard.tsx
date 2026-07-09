import { useEffect, useState } from 'react'
import { supabase } from '@/lib/supabase'
import { WeatherObservation } from '@/types/weather'
import { 
  MdWbSunny, 
  MdCloud, 
  MdThunderstorm, 
  MdAcUnit, 
  MdGrain,
  MdRefresh,
  MdThermostat,
  MdWater,
  MdAir
} from 'react-icons/md'
import './WeatherCard.css'

export default function WeatherCard() {
  const [weather, setWeather] = useState<WeatherObservation | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadWeather()

    // Realtime subscription - yeni hava durumu verisi geldiğinde güncelle
    const channel = supabase
      .channel('weather-updates')
      .on(
        'postgres_changes',
        {
          event: 'INSERT',
          schema: 'public',
          table: 'weather_observations',
        },
        (payload) => {
          console.log('New weather data:', payload)
          setWeather(payload.new as WeatherObservation)
        }
      )
      .subscribe((status) => {
        console.log('Weather realtime subscription status:', status)
      })

    // Her 5 dakikada bir otomatik yenile (yedek mekanizma)
    const interval = setInterval(() => {
      console.log('Auto-refreshing weather data...')
      loadWeather()
    }, 5 * 60 * 1000) // 5 dakika

    return () => {
      channel.unsubscribe()
      clearInterval(interval)
    }
  }, [])

  const loadWeather = async () => {
    try {
      setLoading(true)
      setError(null)

      // Supabase'den son hava durumu verisini çek
      const { data, error: fetchError } = await supabase
        .from('weather_observations')
        .select('*')
        .order('observed_at', { ascending: false })
        .limit(1)
        .single()

      if (fetchError) {
        if (fetchError.code === 'PGRST116') {
          setError('Henüz hava durumu verisi yok')
        } else {
          throw fetchError
        }
      } else {
        setWeather(data)
      }
    } catch (err: any) {
      console.error('Weather load error:', err)
      const errorMessage = err?.message || err?.error?.message || 'Hava durumu yüklenemedi'
      setError(errorMessage)
    } finally {
      setLoading(false)
    }
  }

  const handleRefresh = async () => {
    setRefreshing(true)
    setError(null)

    try {
      console.log('[WeatherCard] Manual refresh triggered')

      // Supabase'den direkt son veriyi çek
      const { data, error: fetchError } = await supabase
        .from('weather_observations')
        .select('*')
        .order('observed_at', { ascending: false })
        .limit(1)
        .single()

      if (fetchError) {
        if (fetchError.code === 'PGRST116') {
          setError('Henüz hava durumu verisi yok')
        } else {
          throw fetchError
        }
      } else {
        console.log('[WeatherCard] Weather data refreshed:', data)
        setWeather(data)
      }

      // Eğer Electron'daysa, yeni veri çekmesini tetikle (opsiyonel)
      if (window.electron) {
        console.log('[WeatherCard] Triggering Electron weather fetch')
        window.electron.ipcRenderer.invoke('fetch-weather').catch(err => {
          console.log('[WeatherCard] Electron weather fetch error (ignored):', err)
        })
      }
    } catch (err: any) {
      console.error('[WeatherCard] Weather refresh error:', err)
      setError(err.message || 'Hava durumu güncellenemedi')
    } finally {
      setRefreshing(false)
    }
  }

  const getWeatherIcon = (weatherMain: string) => {
    const main = weatherMain.toUpperCase()
    switch (main) {
      case 'CLEAR':
        return <MdWbSunny className="weather-icon sunny" />
      case 'CLOUDS':
        return <MdCloud className="weather-icon cloudy" />
      case 'RAIN':
      case 'DRIZZLE':
        return <MdGrain className="weather-icon rainy" />
      case 'THUNDERSTORM':
        return <MdThunderstorm className="weather-icon stormy" />
      case 'SNOW':
        return <MdAcUnit className="weather-icon snowy" />
      default:
        return <MdCloud className="weather-icon" />
    }
  }

  const getWeatherColor = (weatherMain: string) => {
    const main = weatherMain.toUpperCase()
    switch (main) {
      case 'CLEAR':
        return '#FDB813'
      case 'CLOUDS':
        return '#95A5A6'
      case 'RAIN':
      case 'DRIZZLE':
        return '#3498DB'
      case 'THUNDERSTORM':
        return '#9B59B6'
      case 'SNOW':
        return '#ECF0F1'
      default:
        return '#95A5A6'
    }
  }

  const formatTime = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleTimeString('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  if (loading) {
    return (
      <div className="weather-card loading">
        <div className="spinner"></div>
        <p>Hava durumu yükleniyor...</p>
      </div>
    )
  }

  if (error && !weather) {
    // Tablo yoksa daha açıklayıcı mesaj göster
    const isTableMissing = error.includes('relation') || error.includes('does not exist') || error.includes('PGRST')
    
    return (
      <div className="weather-card error">
        {isTableMissing ? (
          <>
            <p className="error-text">⚠️ Hava durumu tablosu henüz oluşturulmamış</p>
            <p style={{ fontSize: '0.85rem', opacity: 0.8, marginTop: '8px' }}>
              Supabase'de <code>supabase-create-weather-table.sql</code> dosyasını çalıştırın
            </p>
          </>
        ) : (
          <>
            <p className="error-text">{error}</p>
            <button onClick={loadWeather} className="btn btn-sm btn-secondary">
              Tekrar Dene
            </button>
          </>
        )}
      </div>
    )
  }

  if (!weather) {
    return (
      <div className="weather-card empty">
        <p>Hava durumu verisi bulunamadı</p>
        <p style={{ fontSize: '0.85rem', opacity: 0.8, marginTop: '8px' }}>
          Electron uygulaması otomatik olarak veri çekecek
        </p>
      </div>
    )
  }

  const weatherColor = getWeatherColor(weather.weather_main)

  return (
    <div className="weather-card" style={{ borderColor: weatherColor }}>
      <div className="weather-header">
        <div className="weather-title">
          <h3>{weather.city_name}</h3>
          <p className="weather-time">
            Son güncelleme: {formatTime(weather.observed_at)}
          </p>
        </div>
        <button
          onClick={handleRefresh}
          disabled={refreshing}
          className="weather-refresh-btn"
          title="Hava durumunu güncelle"
        >
          <MdRefresh className={refreshing ? 'spinning' : ''} />
        </button>
      </div>

      <div className="weather-main">
        <div className="weather-icon-container" style={{ color: weatherColor }}>
          {getWeatherIcon(weather.weather_main)}
        </div>
        <div className="weather-temp">
          <div className="temp-value">{Math.round(weather.temp_c)}°</div>
          <div className="weather-desc">{weather.weather_desc}</div>
        </div>
      </div>

      <div className="weather-details">
        <div className="weather-detail-item">
          <MdThermostat className="detail-icon" />
          <div className="detail-content">
            <span className="detail-label">Hissedilen</span>
            <span className="detail-value">{Math.round(weather.feels_like_c)}°C</span>
          </div>
        </div>

        <div className="weather-detail-item">
          <MdWater className="detail-icon" />
          <div className="detail-content">
            <span className="detail-label">Nem</span>
            <span className="detail-value">{weather.humidity}%</span>
          </div>
        </div>

        <div className="weather-detail-item">
          <MdAir className="detail-icon" />
          <div className="detail-content">
            <span className="detail-label">Rüzgar</span>
            <span className="detail-value">{weather.wind_speed} m/s</span>
          </div>
        </div>
      </div>

      {(weather.is_rain || weather.is_snow) && (
        <div className="weather-alert">
          {weather.is_rain && '🌧️ Yağmur yağıyor'}
          {weather.is_snow && '❄️ Kar yağıyor'}
        </div>
      )}

      {error && (
        <div className="weather-error-banner">
          <small>{error}</small>
        </div>
      )}
    </div>
  )
}
