-- ============================================
-- ORDEVO - Realtime Subscriptions
-- ============================================

-- Enable Realtime for all tables
ALTER PUBLICATION supabase_realtime ADD TABLE organizations;
ALTER PUBLICATION supabase_realtime ADD TABLE profiles;
ALTER PUBLICATION supabase_realtime ADD TABLE restaurant_tables;
ALTER PUBLICATION supabase_realtime ADD TABLE menu_categories;
ALTER PUBLICATION supabase_realtime ADD TABLE menu_items;
ALTER PUBLICATION supabase_realtime ADD TABLE orders;
ALTER PUBLICATION supabase_realtime ADD TABLE order_items;
ALTER PUBLICATION supabase_realtime ADD TABLE organization_settings;
ALTER PUBLICATION supabase_realtime ADD TABLE category_printer_mappings;
ALTER PUBLICATION supabase_realtime ADD TABLE weather_data;
ALTER PUBLICATION supabase_realtime ADD TABLE daily_sales_archive;

-- Verify Realtime is enabled
SELECT schemaname, tablename 
FROM pg_publication_tables 
WHERE pubname = 'supabase_realtime'
ORDER BY tablename;
