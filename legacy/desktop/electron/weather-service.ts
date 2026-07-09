// Weather Service - Electron Main Process
// OpenWeatherMap API'den hava durumu verisi çeker ve Supabase'e kaydeder

import { createClient } from '@supabase/supabase-js'

const OPENWEATHER_API_KEY = '11426b89ab587e713885d7d6e003cf35'
const OPENWEATHER_BASE_URL = 'https://api.openweathermap.org/data/2.5/weather'
// Seferihisar, İzmir koordinatları (Mersin Alanı)
const DEFAULT_LAT = 38.1967
const DEFAULT_LON = 26.8383
const DEFAULT_CITY = 'Seferihisar'
const UPDATE_INTERVAL = 30 * 60 * 1000 // 30 dakika

// Supabase client
// Electron main process'te VITE_ prefix'li değişkenler otomatik yüklenmez
// Bu yüzden hardcode ediyoruz (güvenli çünkü anon key zaten public)
const supabaseUrl = 'https://kiucehfasorbtftnferi.supabase.co'
const supabaseKey = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImtpdWNlaGZhc29yYnRmdG5mZXJpIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjI4NzQ3MzgsImV4cCI6MjA3ODQ1MDczOH0.xetRavdybCP-gNwPNQJLeGOt5rVcHsM3Y2VKVTcPKSM'

const supabase = createClient(supabaseUrl, supabaseKey)

interface OpenWeatherResponse {
  coord: { lon: number; lat: number }
  weather: Array<{
    id: number
    main: string
    description: string
    icon: string
  }>
  main: {
    temp: number
    feels_like: number
    temp_min: number
    temp_max: number
    pressure: number
    humidity: number
  }
  wind: {
    speed: number
    deg: number
  }
  clouds: { all: number }
  dt: number
  sys: {
    country: string
    sunrise: number
    sunset: number
  }
  name: string
  cod: number
}

interface WeatherData {
  observed_at: string
  city_name: string
  temp_c: number
  feels_like_c: number
  humidity: number
  wind_speed: number
  weather_main: string
  weather_desc: string
  weather_icon: string
  is_rain: boolean
  is_snow: boolean
}

class WeatherService {
  private intervalId: NodeJS.Timeout | null = null
  private latitude: number = DEFAULT_LAT
  private longitude: number = DEFAULT_LON
  private cityName: string = DEFAULT_CITY

  constructor() {
    console.log('[WeatherService] Initialized')
  }

  /**
   * Hava durumu servisini başlat
   */
  start(lat: number = DEFAULT_LAT, lon: number = DEFAULT_LON, city: string = DEFAULT_CITY): void {
    this.latitude = lat
    this.longitude = lon
    this.cityName = city
    console.log(`[WeatherService] Starting for: ${this.cityName} (${this.latitude}, ${this.longitude})`)

    // İlk veriyi hemen çek
    this.fetchAndSaveWeather()

    // 30 dakikada bir güncelle
    this.intervalId = setInterval(() => {
      this.fetchAndSaveWeather()
    }, UPDATE_INTERVAL)

    console.log(`[WeatherService] Scheduled updates every ${UPDATE_INTERVAL / 60000} minutes`)
  }

  /**
   * Hava durumu servisini durdur
   */
  stop(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId)
      this.intervalId = null
      console.log('[WeatherService] Stopped')
    }
  }

  /**
   * Konum değiştir
   */
  setLocation(lat: number, lon: number, city: string): void {
    this.latitude = lat
    this.longitude = lon
    this.cityName = city
    console.log(`[WeatherService] Location changed to: ${this.cityName} (${this.latitude}, ${this.longitude})`)
    // Hemen yeni konum için veri çek
    this.fetchAndSaveWeather()
  }

  /**
   * Şehir adını değiştir (geriye uyumluluk için)
   */
  setCity(city: string): void {
    // Bazı bilinen şehirlerin koordinatları
    const cities: Record<string, { lat: number; lon: number }> = {
      'Seferihisar': { lat: 38.1967, lon: 26.8383 },
      'Istanbul': { lat: 41.0082, lon: 28.9784 },
      'Ankara': { lat: 39.9334, lon: 32.8597 },
      'Izmir': { lat: 38.4237, lon: 27.1428 },
    }

    const coords = cities[city]
    if (coords) {
      this.setLocation(coords.lat, coords.lon, city)
    } else {
      console.warn(`[WeatherService] Unknown city: ${city}, keeping current location`)
    }
  }

  /**
   * OpenWeatherMap API'den hava durumu verisi çek (koordinat bazlı)
   */
  private async fetchWeatherData(): Promise<OpenWeatherResponse> {
    const url = `${OPENWEATHER_BASE_URL}?lat=${this.latitude}&lon=${this.longitude}&appid=${OPENWEATHER_API_KEY}&units=metric&lang=tr`

    console.log(`[WeatherService] Fetching weather for: ${this.cityName} (${this.latitude}, ${this.longitude})`)

    const response = await fetch(url)

    if (!response.ok) {
      const errorText = await response.text()
      throw new Error(`OpenWeather API error: ${response.status} - ${errorText}`)
    }

    const data: OpenWeatherResponse = await response.json()
    console.log(`[WeatherService] Received data for: ${data.name}`)

    return data
  }

  /**
   * OpenWeather response'unu database formatına dönüştür
   */
  private mapWeatherData(response: OpenWeatherResponse): WeatherData {
    const weather = response.weather[0]
    const weatherMain = weather.main.toUpperCase()

    // API'den gelen şehir adı yerine bizim belirlediğimiz adı kullan
    const displayName = this.cityName || response.name

    return {
      observed_at: new Date().toISOString(),
      city_name: displayName,
      temp_c: Math.round(response.main.temp * 10) / 10,
      feels_like_c: Math.round(response.main.feels_like * 10) / 10,
      humidity: response.main.humidity,
      wind_speed: Math.round(response.wind.speed * 10) / 10,
      weather_main: weather.main,
      weather_desc: weather.description,
      weather_icon: weather.icon,
      is_rain: weatherMain === 'RAIN' || weatherMain === 'DRIZZLE' || weatherMain === 'THUNDERSTORM',
      is_snow: weatherMain === 'SNOW',
    }
  }

  /**
   * Hava durumu verisini Supabase'e kaydet
   */
  private async saveToDatabase(weatherData: WeatherData): Promise<void> {
    console.log('[WeatherService] Saving to database:', weatherData)

    const { error } = await supabase
      .from('weather_observations')
      .insert(weatherData)

    if (error) {
      throw new Error(`Supabase insert error: ${error.message}`)
    }

    console.log('[WeatherService] Successfully saved to database')
  }

  /**
   * Hava durumu verisini çek ve kaydet (ana fonksiyon)
   */
  async fetchAndSaveWeather(): Promise<WeatherData | null> {
    try {
      // API'den veri çek
      const apiResponse = await this.fetchWeatherData()

      // Veriyi dönüştür
      const weatherData = this.mapWeatherData(apiResponse)

      // Veritabanına kaydet
      await this.saveToDatabase(weatherData)

      console.log('[WeatherService] Weather update completed successfully')
      return weatherData
    } catch (error) {
      console.error('[WeatherService] Error:', error)
      return null
    }
  }

  /**
   * Son hava durumu verisini Supabase'den çek
   */
  async getLatestWeather(): Promise<WeatherData | null> {
    try {
      const { data, error } = await supabase
        .from('weather_observations')
        .select('*')
        .order('observed_at', { ascending: false })
        .limit(1)
        .single()

      if (error) {
        console.error('[WeatherService] Error fetching latest weather:', error)
        return null
      }

      return data
    } catch (error) {
      console.error('[WeatherService] Error:', error)
      return null
    }
  }
}

// Singleton instance
export const weatherService = new WeatherService()
