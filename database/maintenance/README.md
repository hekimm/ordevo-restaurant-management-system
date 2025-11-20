# 🔧 Bakım ve Optimizasyon

Database bakım ve optimizasyon scriptleri.

## 📋 Scriptler

### 1. Sağlık Kontrolü
```sql
-- maintenance/01-check-health.sql
```
Database durumunu kontrol eder:
- Tablo kayıt sayıları
- RLS durumu
- Realtime durumu
- Index durumu
- Aktif siparişler

**Ne Zaman Çalıştırılır:**
- Günlük (otomatik)
- Sorun şüphesi olduğunda
- Performans düşüşünde

### 2. Eski Verileri Temizle
```sql
-- maintenance/02-cleanup-old-data.sql
```
90 günden eski verileri siler:
- Hava durumu kayıtları

**Ne Zaman Çalıştırılır:**
- Aylık (otomatik önerilir)
- Database boyutu büyüdüğünde

**⚠️ UYARI:** Yedek almadan çalıştırmayın!

### 3. Database Optimizasyonu
```sql
-- maintenance/03-vacuum-analyze.sql
```
VACUUM ve ANALYZE işlemleri:
- Ölü satırları temizler
- İstatistikleri günceller
- Performansı artırır

**Ne Zaman Çalıştırılır:**
- Haftalık (otomatik önerilir)
- Büyük veri silme sonrası
- Performans düşüşünde

## 🎯 Kullanım

### Günlük Kontrol
```bash
# Her gün çalıştır
maintenance/01-check-health.sql
```

### Haftalık Bakım
```bash
# Her hafta çalıştır
maintenance/03-vacuum-analyze.sql
```

### Aylık Temizlik
```bash
# Her ay çalıştır (yedek al!)
maintenance/02-cleanup-old-data.sql
```

## 📊 Performans İzleme

### Tablo Boyutları
```sql
SELECT 
  schemaname,
  tablename,
  pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Yavaş Sorgular
```sql
SELECT 
  query,
  calls,
  total_time,
  mean_time,
  max_time
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;
```

### Index Kullanımı
```sql
SELECT 
  schemaname,
  tablename,
  indexname,
  idx_scan as index_scans,
  idx_tup_read as tuples_read,
  idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
ORDER BY idx_scan DESC;
```

## 🔄 Otomatik Bakım

### Supabase Cron Jobs (Önerilir)

```sql
-- Günlük sağlık kontrolü (her gün 02:00)
SELECT cron.schedule(
  'daily-health-check',
  '0 2 * * *',
  $$
  -- Sağlık kontrolü kodu buraya
  $$
);

-- Haftalık optimizasyon (her Pazar 03:00)
SELECT cron.schedule(
  'weekly-vacuum',
  '0 3 * * 0',
  $$
  VACUUM ANALYZE;
  $$
);

-- Aylık temizlik (her ayın 1'i 04:00)
SELECT cron.schedule(
  'monthly-cleanup',
  '0 4 1 * *',
  $$
  DELETE FROM weather_data WHERE recorded_at < CURRENT_DATE - 90;
  $$
);
```

## 💾 Yedekleme

### Manuel Yedek
```bash
# Supabase Dashboard > Database > Backups
# "Create Backup" butonuna tıkla
```

### Otomatik Yedek
Supabase Pro plan ile otomatik günlük yedekleme aktif.

## ⚠️ Önemli Notlar

1. **Yedek Alın**: Temizlik scriptlerini çalıştırmadan önce mutlaka yedek alın
2. **Test Edin**: Production'da çalıştırmadan önce test ortamında deneyin
3. **İzleyin**: Bakım sonrası performansı izleyin
4. **Zamanlayın**: Yoğun olmayan saatlerde çalıştırın (gece 02:00-04:00)

## 🆘 Sorun Giderme

### "Out of Memory" Hatası
```sql
-- Küçük parçalar halinde temizle
DELETE FROM weather_data 
WHERE recorded_at < CURRENT_DATE - 90
LIMIT 1000;
```

### "Lock Timeout" Hatası
```sql
-- Aktif bağlantıları kontrol et
SELECT * FROM pg_stat_activity 
WHERE state = 'active';
```

### Yavaş VACUUM
```sql
-- VACUUM FULL yerine normal VACUUM kullan
VACUUM (VERBOSE, ANALYZE) table_name;
```

## 📈 Monitoring

### Disk Kullanımı
```sql
SELECT 
  pg_size_pretty(pg_database_size(current_database())) as database_size;
```

### Bağlantı Sayısı
```sql
SELECT 
  COUNT(*) as active_connections
FROM pg_stat_activity
WHERE state = 'active';
```

### Cache Hit Ratio
```sql
SELECT 
  sum(heap_blks_hit) / (sum(heap_blks_hit) + sum(heap_blks_read)) as cache_hit_ratio
FROM pg_statio_user_tables;
```
