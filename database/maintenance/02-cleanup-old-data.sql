-- ============================================
-- Eski Verileri Temizle
-- ============================================

-- UYARI: Bu script eski verileri siler!
-- Çalıştırmadan önce yedek alın!

DO $$
DECLARE
  v_days_to_keep INTEGER := 90; -- Kaç günlük veri tutulacak
  v_cutoff_date DATE;
  v_deleted_weather INTEGER;
BEGIN
  v_cutoff_date := CURRENT_DATE - v_days_to_keep;

  -- Eski hava durumu verilerini sil
  DELETE FROM weather_data
  WHERE recorded_at < v_cutoff_date;
  
  GET DIAGNOSTICS v_deleted_weather = ROW_COUNT;

  RAISE NOTICE '% eski hava durumu kaydı silindi', v_deleted_weather;
  RAISE NOTICE 'Cutoff date: %', v_cutoff_date;
END $$;
