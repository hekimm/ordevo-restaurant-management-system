# 🚀 Ordevo - Database Setup

Projeyi sıfırdan kurmak için gerekli SQL scriptleri.

## 📋 Kurulum Sırası

### 1. Schema Oluştur
```sql
-- setup/01-schema.sql
```
Tüm tabloları, indexleri ve trigger'ları oluşturur.

### 2. RLS Politikalarını Aktifleştir
```sql
-- setup/02-rls-policies.sql
```
Row Level Security politikalarını ayarlar.

### 3. Realtime'ı Aktifleştir
```sql
-- setup/03-realtime.sql
```
Tüm tablolar için realtime subscription'ları aktif eder.

### 4. Fonksiyonları Oluştur
```sql
-- setup/04-functions.sql
```
Günlük arşivleme, istatistik ve raporlama fonksiyonlarını ekler.

### 5. Örnek Veri Ekle (Opsiyonel)
```sql
-- setup/05-sample-data.sql
```
Test için örnek menü, masa ve kategori verileri ekler.

## 🎯 Hızlı Kurulum

### Supabase Dashboard'dan:

1. **SQL Editor**'ü aç
2. Scriptleri sırayla çalıştır:
   - `01-schema.sql`
   - `02-rls-policies.sql`
   - `03-realtime.sql`
   - `04-functions.sql`
   - `05-sample-data.sql` (opsiyonel)

### Tek Komutla (Tüm scriptler):

```sql
-- 1. Schema
\i setup/01-schema.sql

-- 2. RLS
\i setup/02-rls-policies.sql

-- 3. Realtime
\i setup/03-realtime.sql

-- 4. Functions
\i setup/04-functions.sql

-- 5. Sample Data (opsiyonel)
\i setup/05-sample-data.sql
```

## ✅ Kurulum Sonrası Kontrol

### Tabloları Kontrol Et
```sql
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;
```

### RLS Durumunu Kontrol Et
```sql
SELECT tablename, rowsecurity 
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY tablename;
```

### Realtime Durumunu Kontrol Et
```sql
SELECT schemaname, tablename 
FROM pg_publication_tables 
WHERE pubname = 'supabase_realtime'
ORDER BY tablename;
```

### Fonksiyonları Kontrol Et
```sql
SELECT routine_name 
FROM information_schema.routines 
WHERE routine_schema = 'public'
ORDER BY routine_name;
```

## 📊 Database Schema

### Core Tables
- `organizations` - Restoranlar
- `profiles` - Kullanıcılar
- `restaurant_tables` - Masalar
- `menu_categories` - Menü kategorileri
- `menu_items` - Menü ürünleri
- `orders` - Siparişler
- `order_items` - Sipariş ürünleri

### Settings Tables
- `organization_settings` - Organizasyon ayarları
- `category_printer_mappings` - Kategori-yazıcı eşleştirmeleri

### Analytics Tables
- `weather_data` - Hava durumu verileri
- `daily_sales_archive` - Günlük satış arşivi

## 🔐 Güvenlik

- ✅ RLS tüm tablolarda aktif
- ✅ Kullanıcılar sadece kendi organizasyonlarının verilerini görebilir
- ✅ Owner ve Manager rolleri yönetim yetkilerine sahip
- ✅ Waiter ve Cashier rolleri sınırlı yetkiye sahip

## 🔄 Realtime

Tüm tablolar realtime subscription destekler:
- Sipariş güncellemeleri
- Masa durumu değişiklikleri
- Menü değişiklikleri
- Anlık bildirimler

## 📝 Notlar

1. **İlk Kullanıcı**: Uygulamadan kayıt olun (Register)
2. **Organization ID**: Kayıt sonrası otomatik oluşur
3. **Sample Data**: `05-sample-data.sql` içinde organization_id'yi güncelleyin
4. **Backup**: Düzenli olarak database backup alın

## 🆘 Sorun Giderme

### RLS Hataları
```sql
-- RLS'i geçici olarak devre dışı bırak (sadece debug için)
ALTER TABLE table_name DISABLE ROW LEVEL SECURITY;
```

### Realtime Çalışmıyor
```sql
-- Realtime'ı yeniden ekle
ALTER PUBLICATION supabase_realtime DROP TABLE table_name;
ALTER PUBLICATION supabase_realtime ADD TABLE table_name;
```

### Trigger Hataları
```sql
-- Trigger'ı yeniden oluştur
DROP TRIGGER IF EXISTS trigger_name ON table_name;
CREATE TRIGGER trigger_name BEFORE UPDATE ON table_name
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
```

## 📞 Destek

Sorun yaşarsanız:
1. Hata mesajını kontrol edin
2. RLS politikalarını kontrol edin
3. Kullanıcı rolünü kontrol edin
4. Organization ID'nin doğru olduğundan emin olun
