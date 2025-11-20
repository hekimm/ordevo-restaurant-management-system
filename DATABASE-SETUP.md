# 🗄️ Ordevo - Database Kurulum Rehberi

Projeyi sıfırdan kurmak için eksiksiz SQL scriptleri.

## 📁 Klasör Yapısı

```
setup/          # İlk kurulum scriptleri
├── 01-schema.sql           # Tablo ve index'ler
├── 02-rls-policies.sql     # Güvenlik politikaları
├── 03-realtime.sql         # Realtime subscriptions
├── 04-functions.sql        # Database fonksiyonları
├── 05-sample-data.sql      # Örnek veriler (opsiyonel)
└── README.md

users/          # Kullanıcı yönetimi
├── 01-add-waiter.sql       # Garson ekle
├── 02-list-users.sql       # Kullanıcıları listele
├── 03-delete-user.sql      # Kullanıcı sil
├── 04-change-password.sql  # Şifre değiştir
└── README.md

maintenance/    # Bakım ve optimizasyon
├── 01-check-health.sql     # Sağlık kontrolü
├── 02-cleanup-old-data.sql # Eski verileri temizle
├── 03-vacuum-analyze.sql   # Optimizasyon
└── README.md
```

## 🚀 Hızlı Başlangıç

### 1. Supabase Projesi Oluştur
1. https://supabase.com adresine git
2. "New Project" oluştur
3. Project URL ve Anon Key'i kopyala

### 2. Database Kurulumu
Supabase SQL Editor'de sırayla çalıştır:

```sql
-- 1. Schema (Tablolar)
\i setup/01-schema.sql

-- 2. RLS Politikaları
\i setup/02-rls-policies.sql

-- 3. Realtime
\i setup/03-realtime.sql

-- 4. Fonksiyonlar
\i setup/04-functions.sql

-- 5. Örnek Veriler (opsiyonel)
\i setup/05-sample-data.sql
```

### 3. İlk Kullanıcı Oluştur
Uygulamadan kayıt ol (Register sayfası):
- Email: m.sirinyilmaz6@gmail.com
- Şifre: (güçlü şifre)
- Restoran Adı: (restoran adınız)

### 4. Organization ID'yi Al
```sql
SELECT organization_id 
FROM profiles 
WHERE email = 'm.sirinyilmaz6@gmail.com';
```

### 5. Config Dosyalarını Güncelle
```typescript
// src/config/organization.ts
// mobile-new/config/organization.ts
ORGANIZATION_ID: 'buraya-organization-id-yapistir'
```

### 6. Garson Kullanıcısı Ekle
```sql
-- users/01-add-waiter.sql
-- Email, şifre ve organization_id'yi düzenle
-- Çalıştır
```

## ✅ Kurulum Kontrolü

### Tabloları Kontrol Et
```sql
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;
```

Beklenen tablolar:
- ✅ organizations
- ✅ profiles
- ✅ restaurant_tables
- ✅ menu_categories
- ✅ menu_items
- ✅ orders
- ✅ order_items
- ✅ organization_settings
- ✅ category_printer_mappings
- ✅ weather_data
- ✅ daily_sales_archive

### RLS Kontrolü
```sql
SELECT tablename, rowsecurity 
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY tablename;
```

Tüm tablolarda `rowsecurity = true` olmalı.

### Realtime Kontrolü
```sql
SELECT tablename 
FROM pg_publication_tables 
WHERE pubname = 'supabase_realtime'
ORDER BY tablename;
```

11 tablo listelenmelidir.

## 📊 Database Schema

### Core Tables

**organizations** - Restoranlar
- id, name, slug
- created_at, updated_at

**profiles** - Kullanıcılar
- id, organization_id, email, full_name, role
- Roller: owner, manager, cashier, waiter

**restaurant_tables** - Masalar
- id, organization_id, name, capacity
- is_active, sort_order

**menu_categories** - Menü Kategorileri
- id, organization_id, name, sort_order

**menu_items** - Menü Ürünleri
- id, organization_id, category_id
- name, description, price, vat_rate

**orders** - Siparişler
- id, organization_id, table_id
- status (open/closed/cancelled)
- opened_by_user_id, closed_by_user_id
- opened_at, closed_at

**order_items** - Sipariş Ürünleri
- id, organization_id, order_id, menu_item_id
- quantity, unit_price, total_price
- status (pending/in_kitchen/served/cancelled)

### Settings Tables

**organization_settings** - Organizasyon Ayarları
- auto_print_enabled, default_printer

**category_printer_mappings** - Kategori-Yazıcı Eşleştirmeleri
- category_id, printer_name

### Analytics Tables

**weather_data** - Hava Durumu
- location, temperature, humidity, wind_speed

**daily_sales_archive** - Günlük Satış Arşivi
- business_date, total_orders, total_revenue

## 🔐 Güvenlik

### RLS (Row Level Security)
- ✅ Tüm tablolarda aktif
- ✅ Kullanıcılar sadece kendi organizasyonlarını görebilir
- ✅ Rol bazlı yetkilendirme

### Roller ve Yetkiler

| Rol | Görüntüleme | Sipariş | Menü Yönetimi | Ayarlar |
|-----|-------------|---------|---------------|---------|
| Owner | ✅ | ✅ | ✅ | ✅ |
| Manager | ✅ | ✅ | ✅ | ✅ |
| Cashier | ✅ | ✅ | ❌ | ❌ |
| Waiter | ✅ | ✅ | ❌ | ❌ |

## 🔄 Realtime

Tüm tablolar realtime subscription destekler:
- Sipariş güncellemeleri
- Masa durumu değişiklikleri
- Menü değişiklikleri
- Anlık bildirimler

## 📝 Kullanım Senaryoları

### Yeni Restoran Ekle
1. Register sayfasından kayıt ol
2. Organization otomatik oluşur
3. Örnek veriler eklenir (opsiyonel)

### Garson Ekle
```sql
-- users/01-add-waiter.sql kullan
```

### Menü Güncelle
Uygulama üzerinden:
- Menu sayfası > Kategori/Ürün ekle

### Sipariş Al
Mobile app üzerinden:
- Masa seç > Ürün ekle > Sipariş oluştur

### Rapor Al
Dashboard üzerinden:
- Reports sayfası > Tarih seç > Export

## 🔧 Bakım

### Günlük
```sql
-- maintenance/01-check-health.sql
```

### Haftalık
```sql
-- maintenance/03-vacuum-analyze.sql
```

### Aylık
```sql
-- maintenance/02-cleanup-old-data.sql
```

## 🆘 Sorun Giderme

### "Permission Denied" Hatası
```sql
-- RLS politikalarını kontrol et
SELECT * FROM pg_policies WHERE schemaname = 'public';
```

### "Organization Not Found" Hatası
```sql
-- Organization ID'yi kontrol et
SELECT * FROM profiles WHERE email = 'your@email.com';
```

### Realtime Çalışmıyor
```sql
-- Realtime'ı yeniden ekle
ALTER PUBLICATION supabase_realtime DROP TABLE table_name;
ALTER PUBLICATION supabase_realtime ADD TABLE table_name;
```

### Yavaş Sorgular
```sql
-- Index'leri kontrol et
SELECT * FROM pg_indexes WHERE schemaname = 'public';
```

## 📞 Destek

Sorun yaşarsanız:
1. Hata mesajını kontrol edin
2. RLS politikalarını kontrol edin
3. Kullanıcı rolünü kontrol edin
4. Organization ID'nin doğru olduğundan emin olun

## 🎯 Sonraki Adımlar

1. ✅ Database kurulumu tamamlandı
2. ✅ İlk kullanıcı oluşturuldu
3. ✅ Organization ID alındı
4. ✅ Config dosyaları güncellendi
5. ✅ Garson kullanıcısı eklendi
6. 🚀 Uygulamayı başlat!

```bash
# Electron App
npm run dev

# Mobile App
cd mobile-new
npm start
```

## 📚 Daha Fazla Bilgi

- `setup/README.md` - Kurulum detayları
- `users/README.md` - Kullanıcı yönetimi
- `maintenance/README.md` - Bakım ve optimizasyon
