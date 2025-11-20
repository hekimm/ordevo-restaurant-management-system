-- ============================================
-- Database Sağlık Kontrolü
-- ============================================

-- Tablo sayıları
SELECT 
  'organizations' as table_name,
  COUNT(*) as record_count
FROM organizations
UNION ALL
SELECT 'profiles', COUNT(*) FROM profiles
UNION ALL
SELECT 'restaurant_tables', COUNT(*) FROM restaurant_tables
UNION ALL
SELECT 'menu_categories', COUNT(*) FROM menu_categories
UNION ALL
SELECT 'menu_items', COUNT(*) FROM menu_items
UNION ALL
SELECT 'orders', COUNT(*) FROM orders
UNION ALL
SELECT 'order_items', COUNT(*) FROM order_items
UNION ALL
SELECT 'organization_settings', COUNT(*) FROM organization_settings
UNION ALL
SELECT 'category_printer_mappings', COUNT(*) FROM category_printer_mappings
UNION ALL
SELECT 'weather_data', COUNT(*) FROM weather_data
UNION ALL
SELECT 'daily_sales_archive', COUNT(*) FROM daily_sales_archive;

-- RLS Durumu
SELECT 
  schemaname,
  tablename,
  rowsecurity as rls_enabled
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY tablename;

-- Realtime Durumu
SELECT 
  schemaname,
  tablename
FROM pg_publication_tables 
WHERE pubname = 'supabase_realtime'
ORDER BY tablename;

-- Index Durumu
SELECT 
  schemaname,
  tablename,
  indexname,
  indexdef
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename, indexname;

-- Aktif Siparişler
SELECT 
  o.organization_id,
  COUNT(*) as open_orders,
  SUM(oi.total_price) as total_value
FROM orders o
LEFT JOIN order_items oi ON oi.order_id = o.id
WHERE o.status = 'open'
GROUP BY o.organization_id;
