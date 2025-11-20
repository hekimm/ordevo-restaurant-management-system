// Export Products CSV for ML

import { SupabaseClient } from '@supabase/supabase-js'
import { writeFile } from 'fs/promises'
import { objectsToCSV, generateTimeBuckets } from './csvUtils'
import { getProductDataForBucket } from './queries'
import type { ProductCSVRow, ExportOptions } from '../../src/types/ml-export'

/**
 * Export products CSV for ML analysis
 */
export async function exportProductsCSV(
  supabase: SupabaseClient,
  options: ExportOptions,
  filePath: string
): Promise<{ success: boolean; rowCount: number; error?: string }> {
  try {
    console.log('[ExportProductsCSV] Starting export:', options)

    const { date, bucketMinutes, organizationId } = options

    // Generate time buckets
    const buckets = generateTimeBuckets(date, bucketMinutes)
    console.log(`[ExportProductsCSV] Generated ${buckets.length} time buckets`)

    // Collect data for each bucket
    const rows: ProductCSVRow[] = []

    for (const bucket of buckets) {
      console.log(`[ExportProductsCSV] Processing bucket: ${bucket.start} - ${bucket.end}`)

      // Get product data for this bucket
      const products = await getProductDataForBucket(
        supabase,
        organizationId,
        date,
        bucket.start,
        bucket.end
      )

      // Add each product as a row
      products.forEach((product) => {
        const row: ProductCSVRow = {
          date: date,
          time_bucket_start: bucket.start,
          time_bucket_end: bucket.end,
          product_id: product.product_id,
          product_name: product.product_name,
          category: product.category,
          qty_sold: product.qty_sold,
          revenue: Math.round(product.revenue * 100) / 100,
        }

        rows.push(row)
      })
    }

    console.log(`[ExportProductsCSV] Collected ${rows.length} rows`)

    // Convert to CSV
    const csvContent = objectsToCSV(rows)

    // Write to file
    await writeFile(filePath, csvContent, 'utf-8')

    console.log(`[ExportProductsCSV] Successfully exported to: ${filePath}`)

    return {
      success: true,
      rowCount: rows.length,
    }
  } catch (error: any) {
    console.error('[ExportProductsCSV] Error:', error)
    return {
      success: false,
      rowCount: 0,
      error: error.message || 'Unknown error',
    }
  }
}
