// SQL Queries for ML Export

import { SupabaseClient } from '@supabase/supabase-js'

/**
 * Get master data for a specific time bucket
 */
export async function getMasterDataForBucket(
  supabase: SupabaseClient,
  organizationId: string,
  date: string,
  bucketStart: string,
  bucketEnd: string
): Promise<any> {
  const startTimestamp = `${date} ${bucketStart}`
  const endTimestamp = `${date} ${bucketEnd}`

  // Get orders in this time bucket
  const { data: orders, error: ordersError } = await supabase
    .from('orders')
    .select(`
      id,
      table_id,
      opened_at,
      closed_at,
      status
    `)
    .eq('organization_id', organizationId)
    .gte('opened_at', startTimestamp)
    .lt('opened_at', endTimestamp)

  if (ordersError) throw ordersError

  if (!orders || orders.length === 0) {
    return {
      total_orders: 0,
      total_revenue: 0,
      avg_order_value: 0,
      total_items: 0,
      unique_tables: 0,
      avg_table_occupancy_min: null,
      num_cash_payments: 0,
      num_card_payments: 0,
      num_cancelled_orders: 0,
      discount_total: 0,
    }
  }

  const orderIds = orders.map((o) => o.id)

  // Get payments for these orders
  const { data: payments, error: paymentsError } = await supabase
    .from('payments')
    .select('amount, payment_method')
    .in('order_id', orderIds)

  if (paymentsError) throw paymentsError

  // Get order items for these orders
  const { data: orderItems, error: itemsError } = await supabase
    .from('order_items')
    .select('quantity, total_price, status')
    .in('order_id', orderIds)

  if (itemsError) throw itemsError

  // Calculate metrics
  const totalRevenue = payments?.reduce((sum, p) => sum + Number(p.amount), 0) || 0
  const totalOrders = orders.length
  const avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0

  const totalItems = orderItems?.reduce((sum, item) => {
    if (item.status !== 'cancelled') {
      return sum + item.quantity
    }
    return sum
  }, 0) || 0

  const uniqueTables = new Set(orders.map((o) => o.table_id)).size

  // Calculate average table occupancy (in minutes)
  const occupancyTimes = orders
    .filter((o) => o.closed_at)
    .map((o) => {
      const opened = new Date(o.opened_at).getTime()
      const closed = new Date(o.closed_at!).getTime()
      return (closed - opened) / (1000 * 60) // minutes
    })

  const avgTableOccupancyMin =
    occupancyTimes.length > 0
      ? occupancyTimes.reduce((sum, t) => sum + t, 0) / occupancyTimes.length
      : null

  const numCashPayments = payments?.filter((p) => p.payment_method === 'cash').length || 0
  const numCardPayments = payments?.filter((p) => p.payment_method === 'credit_card').length || 0

  const numCancelledOrders = orders.filter((o) => o.status === 'cancelled').length

  return {
    total_orders: totalOrders,
    total_revenue: totalRevenue,
    avg_order_value: avgOrderValue,
    total_items: totalItems,
    unique_tables: uniqueTables,
    avg_table_occupancy_min: avgTableOccupancyMin,
    num_cash_payments: numCashPayments,
    num_card_payments: numCardPayments,
    num_cancelled_orders: numCancelledOrders,
    discount_total: 0, // TODO: Add discount field to orders table
  }
}

/**
 * Get weather data for a specific time bucket
 */
export async function getWeatherForBucket(
  supabase: SupabaseClient,
  date: string,
  bucketStart: string
): Promise<any> {
  const targetTimestamp = `${date} ${bucketStart}`

  // Get closest weather observation
  const { data: weather, error } = await supabase
    .from('weather_observations')
    .select('*')
    .gte('observed_at', `${date} 00:00:00`)
    .lte('observed_at', `${date} 23:59:59`)
    .order('observed_at', { ascending: true })
    .limit(1)

  if (error) {
    console.error('Weather query error:', error)
    return {
      temp_c: null,
      feels_like_c: null,
      humidity: null,
      wind_speed: null,
      weather_main: null,
      weather_desc: null,
      is_rain: 0,
      is_snow: 0,
      is_storm: 0,
    }
  }

  if (!weather || weather.length === 0) {
    return {
      temp_c: null,
      feels_like_c: null,
      humidity: null,
      wind_speed: null,
      weather_main: null,
      weather_desc: null,
      is_rain: 0,
      is_snow: 0,
      is_storm: 0,
    }
  }

  const w = weather[0]
  return {
    temp_c: w.temp_c,
    feels_like_c: w.feels_like_c,
    humidity: w.humidity,
    wind_speed: w.wind_speed,
    weather_main: w.weather_main,
    weather_desc: w.weather_desc,
    is_rain: w.is_rain ? 1 : 0,
    is_snow: w.is_snow ? 1 : 0,
    is_storm: w.weather_main?.toUpperCase() === 'THUNDERSTORM' ? 1 : 0,
  }
}

/**
 * Get product data for a specific time bucket
 */
export async function getProductDataForBucket(
  supabase: SupabaseClient,
  organizationId: string,
  date: string,
  bucketStart: string,
  bucketEnd: string
): Promise<any[]> {
  const startTimestamp = `${date} ${bucketStart}`
  const endTimestamp = `${date} ${bucketEnd}`

  // Get orders in this time bucket
  const { data: orders, error: ordersError } = await supabase
    .from('orders')
    .select('id')
    .eq('organization_id', organizationId)
    .gte('opened_at', startTimestamp)
    .lt('opened_at', endTimestamp)

  if (ordersError) throw ordersError

  if (!orders || orders.length === 0) {
    return []
  }

  const orderIds = orders.map((o) => o.id)

  // Get order items with menu item details
  const { data: items, error: itemsError } = await supabase
    .from('order_items')
    .select(`
      menu_item_id,
      quantity,
      total_price,
      status,
      menu_items (
        id,
        name,
        category_id,
        menu_categories (
          name
        )
      )
    `)
    .in('order_id', orderIds)
    .neq('status', 'cancelled')

  if (itemsError) throw itemsError

  if (!items || items.length === 0) {
    return []
  }

  // Aggregate by product
  const productMap = new Map<string, any>()

  items.forEach((item: any) => {
    const menuItem = item.menu_items
    if (!menuItem) return

    const productId = menuItem.id
    const productName = menuItem.name
    const category = menuItem.menu_categories?.name || 'Unknown'

    if (productMap.has(productId)) {
      const existing = productMap.get(productId)
      existing.qty_sold += item.quantity
      existing.revenue += Number(item.total_price)
    } else {
      productMap.set(productId, {
        product_id: productId,
        product_name: productName,
        category: category,
        qty_sold: item.quantity,
        revenue: Number(item.total_price),
      })
    }
  })

  return Array.from(productMap.values())
}
