// ML Export Types

export interface MasterCSVRow {
  // Zaman / Tarih
  date: string // YYYY-MM-DD
  time_bucket_start: string // HH:MM:SS
  time_bucket_end: string // HH:MM:SS
  weekday: number // 1-7 (1=Monday)
  is_weekend: number // 0 or 1

  // Satış Metrikleri
  total_orders: number
  total_revenue: number
  avg_order_value: number
  total_items: number
  unique_tables: number
  avg_table_occupancy_min: number | null
  num_cash_payments: number
  num_card_payments: number
  num_cancelled_orders: number
  discount_total: number
  avg_people_per_table: number | null

  // Hava Durumu
  temp_c: number | null
  feels_like_c: number | null
  humidity: number | null
  wind_speed: number | null
  weather_main: string | null
  weather_desc: string | null
  is_rain: number // 0 or 1
  is_snow: number // 0 or 1
  is_storm: number // 0 or 1

  // Özel Günler
  is_holiday: number // 0 or 1
  is_special_day: number // 0 or 1
}

export interface ProductCSVRow {
  date: string
  time_bucket_start: string
  time_bucket_end: string
  product_id: string
  product_name: string
  category: string
  qty_sold: number
  revenue: number
}

export interface ExportOptions {
  date: string // YYYY-MM-DD
  bucketMinutes: number // 30, 60, etc.
  organizationId: string
}

export interface ExportResult {
  success: boolean
  filePath?: string
  error?: string
  rowCount?: number
}
