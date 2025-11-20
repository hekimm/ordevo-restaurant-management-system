// Weather Types

export interface WeatherObservation {
  id: string
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
  created_at: string
}

export interface OpenWeatherResponse {
  coord: {
    lon: number
    lat: number
  }
  weather: Array<{
    id: number
    main: string
    description: string
    icon: string
  }>
  base: string
  main: {
    temp: number
    feels_like: number
    temp_min: number
    temp_max: number
    pressure: number
    humidity: number
  }
  visibility: number
  wind: {
    speed: number
    deg: number
  }
  clouds: {
    all: number
  }
  dt: number
  sys: {
    type: number
    id: number
    country: string
    sunrise: number
    sunset: number
  }
  timezone: number
  id: number
  name: string
  cod: number
}

export interface WeatherData {
  city: string
  temp: number
  feelsLike: number
  humidity: number
  windSpeed: number
  weatherMain: string
  weatherDesc: string
  weatherIcon: string
  isRain: boolean
  isSnow: boolean
  observedAt: string
}
