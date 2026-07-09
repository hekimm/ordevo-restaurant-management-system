// Export Master CSV for ML

import { SupabaseClient } from '@supabase/supabase-js'
import { writeFile } from 'fs/promises'
import {
  objectsToCSV,
  generateTimeBuckets,
  getWeekday,
  isWeekend,
} from './csvUtils'
import { getMasterDataForBucket, getWeatherForBucket } from './queries'
import type { MasterCSVRow, ExportOptions } from '../../src/types/ml-export'

/**
 * Export master CSV for ML analysis
 */
export async function exportMasterCSV(
  supabase: SupabaseClient,
  options: ExportOptions,
  filePath: string
): Promise<{ success: boolean; rowCount: number; error?: string }> {
  try {
    console.log('[ExportMasterCSV] Starting export:', options)

    const { date, bucketMinutes, organizationId } = options

    // Generate time buckets
    const buckets = generateTimeBuckets(date, bucketMinutes)
    console.log(`[ExportMasterCSV] Generated ${buckets.length} time buckets`)

    // Get weekday info
    const weekday = getWeekday(date)
    const isWeekendDay = isWeekend(date) ? 1 : 0

    // Collect data for each bucket
    const rows: MasterCSVRow[] = []

    for (const bucket of buckets) {
      console.log(`[ExportMasterCSV] Processing bucket: ${bucket.start} - ${bucket.end}`)

      // Get sales data
      const salesData = await getMasterDataForBucket(
        supabase,
        organizationId,
        date,
        bucket.start,
        bucket.end
      )

      // Get weather data
      const weatherData = await getWeatherForBucket(supabase, date, bucket.start)

      // Combine into row
      const row: MasterCSVRow = {
        // Time/Date
        date: date,
        time_bucket_start: bucket.start,
        time_bucket_end: bucket.end,
        weekday: weekday,
        is_weekend: isWeekendDay,

        // Sales metrics
        total_orders: salesData.total_orders,
        total_revenue: Math.round(salesData.total_revenue * 100) / 100,
        avg_order_value: Math.round(salesData.avg_order_value * 100) / 100,
        total_items: salesData.total_items,
        unique_tables: salesData.unique_tables,
        avg_table_occupancy_min: salesData.avg_table_occupancy_min
          ? Math.round(salesData.avg_table_occupancy_min * 10) / 10
          : null,
        num_cash_payments: salesData.num_cash_payments,
        num_card_payments: salesData.num_card_payments,
        num_cancelled_orders: salesData.num_cancelled_orders,
        discount_total: salesData.discount_total,
        avg_people_per_table: null, // TODO: Add people count to tables

        // Weather
        temp_c: weatherData.temp_c,
        feels_like_c: weatherData.feels_like_c,
        humidity: weatherData.humidity,
        wind_speed: weatherData.wind_speed,
        weather_main: weatherData.weather_main,
        weather_desc: weatherData.weather_desc,
        is_rain: weatherData.is_rain,
        is_snow: weatherData.is_snow,
        is_storm: weatherData.is_storm,

        // Special days
        is_holiday: 0, // TODO: Add holiday calendar
        is_special_day: 0, // TODO: Add special days
      }

      rows.push(row)
    }

    console.log(`[ExportMasterCSV] Collected ${rows.length} rows`)

    // Convert to CSV
    const csvContent = objectsToCSV(rows)

    // Write to file
    await writeFile(filePath, csvContent, 'utf-8')

    console.log(`[ExportMasterCSV] Successfully exported to: ${filePath}`)

    return {
      success: true,
      rowCount: rows.length,
    }
  } catch (error: any) {
    console.error('[ExportMasterCSV] Error:', error)
    return {
      success: false,
      rowCount: 0,
      error: error.message || 'Unknown error',
    }
  }
}
