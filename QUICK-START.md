# ⚡ Ordevo - Hızlı Başlangıç

5 dakikada projeyi ayağa kaldırın!

## 1️⃣ Supabase Kurulumu (2 dakika)

### SQL Editor'de Çalıştır:
```sql
-- Sırayla çalıştır:
setup/01-schema.sql
setup/02-rls-policies.sql
setup/03-realtime.sql
setup/04-functions.sql
```

## 2️⃣ İlk Kullanıcı (1 dakika)

### Uygulamadan Kayıt Ol:
- Email: m.sirinyilmaz6@gmail.com
- Şifre: (güçlü şifre)
- Restoran: (restoran adı)

## 3️⃣ Organization ID Al (30 saniye)

```sql
SELECT organization_id 
FROM profiles 
WHERE email = 'm.sirinyilmaz6@gmail.com';
```

Sonuç: `73cdab97-03c7-466e-91c9-f7c8c18c1f2f` ✅ (Zaten ayarlandı!)

## 4️⃣ Garson Ekle (1 dakika)

```sql
-- users/01-add-waiter.sql aç
-- Değişkenleri düzenle:
v_email := 'garson@ordevo.com';
v_password := 'garson123';
v_full_name := 'Garson Adı';
-- Çalıştır
```

## 5️⃣ Uygulamayı Başlat (30 saniye)

```bash
# Electron App
npm run dev

# Mobile App
cd mobile-new
npm start
```

## ✅ Tamamlandı!

Artık kullanmaya hazırsınız:
- 🖥️ Electron App: http://localhost:5173
- 📱 Mobile App: Expo Go ile QR kodu tarat

## 📚 Detaylı Dokümantasyon

- `DATABASE-SETUP.md` - Tam kurulum rehberi
- `setup/README.md` - Database detayları
- `users/README.md` - Kullanıcı yönetimi
- `maintenance/README.md` - Bakım scriptleri

## 🆘 Sorun mu var?

1. RLS kontrolü: `setup/02-rls-policies.sql`
2. Realtime kontrolü: `setup/03-realtime.sql`
3. Kullanıcı kontrolü: `users/02-list-users.sql`
