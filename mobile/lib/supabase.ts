import 'react-native-url-polyfill/auto';
import { createClient } from '@supabase/supabase-js';
import Constants from 'expo-constants';

const supabaseUrl = Constants.expoConfig?.extra?.supabaseUrl || process.env.EXPO_PUBLIC_SUPABASE_URL || '';
const supabaseAnonKey = Constants.expoConfig?.extra?.supabaseAnonKey || process.env.EXPO_PUBLIC_SUPABASE_ANON_KEY || '';

if (!supabaseUrl || !supabaseAnonKey) {
  console.warn('Supabase URL veya Anon Key tanımlı değil!');
}

export const supabase = createClient(supabaseUrl, supabaseAnonKey, {
  auth: {
    autoRefreshToken: true,
    persistSession: true,
    detectSessionInUrl: false,
  },
});

// Tip tanımlamaları
export interface Organization {
  id: string;
  name: string;
  slug: string;
  created_at: string;
}

export interface Profile {
  id: string;
  organization_id: string;
  full_name: string;
  role: 'owner' | 'manager' | 'cashier' | 'waiter';
  created_at: string;
}

export interface RestaurantTable {
  id: string;
  organization_id: string;
  name: string;
  capacity?: number;
  is_active: boolean;
  sort_order: number;
  created_at: string;
}

export interface MenuCategory {
  id: string;
  organization_id: string;
  name: string;
  sort_order: number;
  created_at: string;
}

export interface MenuItem {
  id: string;
  organization_id: string;
  category_id: string;
  name: string;
  description?: string;
  price: number;
  is_active: boolean;
  vat_rate: number;
  created_at: string;
  category?: MenuCategory;
}

export interface Order {
  id: string;
  organization_id: string;
  table_id: string;
  status: 'open' | 'closed' | 'cancelled';
  opened_by_user_id: string;
  closed_by_user_id?: string;
  opened_at: string;
  closed_at?: string;
  note?: string;
  table?: RestaurantTable;
}

export interface OrderItem {
  id: string;
  organization_id: string;
  order_id: string;
  menu_item_id: string;
  quantity: number;
  unit_price: number;
  total_price: number;
  status: 'pending' | 'in_kitchen' | 'served' | 'cancelled';
  created_by_user_id: string;
  created_at: string;
  menu_item?: MenuItem;
}
