import { createClient } from '@supabase/supabase-js'

const supabaseUrl = import.meta.env.VITE_SUPABASE_URL
const supabaseAnonKey = import.meta.env.VITE_SUPABASE_ANON_KEY

if (!supabaseUrl || !supabaseAnonKey) {
  throw new Error('Supabase URL ve Anon Key tanımlanmalı!')
}

export const supabase = createClient(supabaseUrl, supabaseAnonKey)

// Tip tanımlamaları
export interface Organization {
  id: string
  name: string
  slug: string
  created_at: string
}

export interface Profile {
  id: string
  organization_id: string
  full_name: string
  role: 'owner' | 'manager' | 'cashier' | 'waiter'
  created_at: string
}

export interface RestaurantTable {
  id: string
  organization_id: string
  name: string
  capacity?: number
  is_active: boolean
  sort_order: number
  created_at: string
}

export interface MenuCategory {
  id: string
  organization_id: string
  name: string
  sort_order: number
  created_at: string
}

export interface MenuItem {
  id: string
  organization_id: string
  category_id: string
  name: string
  description?: string
  price: number
  is_active: boolean
  created_at: string
}

export interface Order {
  id: string
  organization_id: string
  table_id: string
  status: 'open' | 'closed' | 'cancelled'
  opened_by_user_id: string
  closed_by_user_id?: string
  opened_at: string
  closed_at?: string
  note?: string
}

export interface OrderItem {
  id: string
  organization_id: string
  order_id: string
  menu_item_id: string
  quantity: number
  unit_price: number
  total_price: number
  status: 'pending' | 'in_kitchen' | 'served' | 'cancelled'
  created_by_user_id: string
  created_at: string
}

export interface Payment {
  id: string
  organization_id: string
  order_id: string
  amount: number
  payment_method: 'cash' | 'credit_card' | 'online' | 'other'
  created_at: string
  created_by_user_id: string
}
