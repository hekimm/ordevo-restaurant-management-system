// CSV Utility Functions

/**
 * Convert array of objects to CSV string
 */
export function objectsToCSV<T extends Record<string, any>>(
  data: T[],
  headers?: string[]
): string {
  if (data.length === 0) {
    return headers ? headers.join(',') + '\n' : ''
  }

  // Get headers from first object if not provided
  const csvHeaders = headers || Object.keys(data[0])

  // Build CSV
  const headerRow = csvHeaders.join(',')
  const dataRows = data.map((row) => {
    return csvHeaders
      .map((header) => {
        const value = row[header]
        return formatCSVValue(value)
      })
      .join(',')
  })

  return [headerRow, ...dataRows].join('\n')
}

/**
 * Format a single value for CSV
 */
function formatCSVValue(value: any): string {
  if (value === null || value === undefined) {
    return ''
  }

  // Convert to string
  let strValue = String(value)

  // Escape quotes and wrap in quotes if contains comma, quote, or newline
  if (
    strValue.includes(',') ||
    strValue.includes('"') ||
    strValue.includes('\n')
  ) {
    strValue = '"' + strValue.replace(/"/g, '""') + '"'
  }

  return strValue
}

/**
 * Generate filename with date
 */
export function generateFilename(
  prefix: string,
  date: string,
  extension: string = 'csv'
): string {
  return `${prefix}_${date}.${extension}`
}

/**
 * Validate date format (YYYY-MM-DD)
 */
export function isValidDate(dateString: string): boolean {
  const regex = /^\d{4}-\d{2}-\d{2}$/
  if (!regex.test(dateString)) {
    return false
  }

  const date = new Date(dateString)
  return date instanceof Date && !isNaN(date.getTime())
}

/**
 * Format date for SQL queries
 */
export function formatDateForSQL(date: string): {
  startOfDay: string
  endOfDay: string
} {
  return {
    startOfDay: `${date} 00:00:00`,
    endOfDay: `${date} 23:59:59`,
  }
}

/**
 * Get weekday number (1-7, 1=Monday)
 */
export function getWeekday(dateString: string): number {
  const date = new Date(dateString)
  const day = date.getDay()
  // Convert Sunday (0) to 7, keep others as is
  return day === 0 ? 7 : day
}

/**
 * Check if date is weekend
 */
export function isWeekend(dateString: string): boolean {
  const weekday = getWeekday(dateString)
  return weekday === 6 || weekday === 7 // Saturday or Sunday
}

/**
 * Generate time buckets for a day
 */
export function generateTimeBuckets(
  date: string,
  bucketMinutes: number
): Array<{ start: string; end: string }> {
  const buckets: Array<{ start: string; end: string }> = []
  const totalMinutes = 24 * 60
  const numBuckets = Math.ceil(totalMinutes / bucketMinutes)

  for (let i = 0; i < numBuckets; i++) {
    const startMinutes = i * bucketMinutes
    const endMinutes = Math.min((i + 1) * bucketMinutes, totalMinutes)

    const startHour = Math.floor(startMinutes / 60)
    const startMin = startMinutes % 60
    const endHour = Math.floor(endMinutes / 60)
    const endMin = endMinutes % 60

    buckets.push({
      start: `${String(startHour).padStart(2, '0')}:${String(startMin).padStart(2, '0')}:00`,
      end: `${String(endHour).padStart(2, '0')}:${String(endMin).padStart(2, '0')}:00`,
    })
  }

  return buckets
}
