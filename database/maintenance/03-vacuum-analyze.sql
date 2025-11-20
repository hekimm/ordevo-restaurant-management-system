-- ============================================
-- Database Optimizasyonu
-- ============================================

-- VACUUM ve ANALYZE tüm tablolar için
VACUUM ANALYZE organizations;
VACUUM ANALYZE profiles;
VACUUM ANALYZE restaurant_tables;
VACUUM ANALYZE menu_categories;
VACUUM ANALYZE menu_items;
VACUUM ANALYZE orders;
VACUUM ANALYZE order_items;
VACUUM ANALYZE organization_settings;
VACUUM ANALYZE category_printer_mappings;
VACUUM ANALYZE weather_data;
VACUUM ANALYZE daily_sales_archive;

-- İstatistikleri güncelle
ANALYZE;

-- Sonuç
SELECT 'Database optimizasyonu tamamlandı!' as status;
