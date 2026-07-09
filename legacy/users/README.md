# 👥 Kullanıcı Yönetimi

Kullanıcı ekleme, silme ve yönetme scriptleri.

## 📋 Scriptler

### 1. Garson Ekle
```sql
-- users/01-add-waiter.sql
```
Yeni garson kullanıcısı ekler.

**Değiştirilmesi Gerekenler:**
- `v_email` - Garson email adresi
- `v_password` - Garson şifresi
- `v_full_name` - Garson tam adı
- `v_org_id` - Organization ID (73cdab97-03c7-466e-91c9-f7c8c18c1f2f)

### 2. Kullanıcıları Listele
```sql
-- users/02-list-users.sql
```
Tüm kullanıcıları listeler.

### 3. Kullanıcı Sil
```sql
-- users/03-delete-user.sql
```
Kullanıcıyı tamamen siler.

**Değiştirilmesi Gerekenler:**
- `v_email` - Silinecek kullanıcı email

### 4. Şifre Değiştir
```sql
-- users/04-change-password.sql
```
Kullanıcı şifresini değiştirir.

**Değiştirilmesi Gerekenler:**
- `v_email` - Kullanıcı email
- `v_new_password` - Yeni şifre

## 🎯 Kullanım Örnekleri

### Garson Ekle
```sql
-- Script'i aç: users/01-add-waiter.sql
-- Değişkenleri düzenle:
v_email := 'ahmet@ordevo.com';
v_password := 'ahmet123';
v_full_name := 'Ahmet Yılmaz';
v_org_id := '73cdab97-03c7-466e-91c9-f7c8c18c1f2f';
-- Çalıştır
```

### Kullanıcıları Görüntüle
```sql
-- users/02-list-users.sql çalıştır
-- Tüm kullanıcılar listelenecek
```

### Kullanıcı Sil
```sql
-- Script'i aç: users/03-delete-user.sql
-- Email'i düzenle:
v_email := 'silinecek@ordevo.com';
-- Çalıştır
```

### Şifre Değiştir
```sql
-- Script'i aç: users/04-change-password.sql
-- Değişkenleri düzenle:
v_email := 'ahmet@ordevo.com';
v_new_password := 'yenisifre123';
-- Çalıştır
```

## 👤 Kullanıcı Rolleri

### Owner (Sahip)
- Tüm yetkilere sahip
- Organizasyon ayarlarını değiştirebilir
- Kullanıcı ekleyebilir/silebilir
- Menü ve masa yönetimi yapabilir

### Manager (Yönetici)
- Menü ve masa yönetimi yapabilir
- Raporları görüntüleyebilir
- Ayarları değiştirebilir
- Kullanıcı ekleyemez

### Cashier (Kasiyer)
- Siparişleri kapatabilir
- Raporları görüntüleyebilir
- Menü ve masa ekleyemez

### Waiter (Garson)
- Sipariş alabilir
- Sipariş güncelleyebilir
- Sadece kendi siparişlerini görebilir
- Yönetim paneline erişemez

## 🔐 Güvenlik

- Şifreler bcrypt ile hashlenir
- Minimum şifre uzunluğu: 6 karakter
- Email adresleri unique olmalı
- RLS politikaları otomatik uygulanır

## ⚠️ Önemli Notlar

1. **Organization ID**: Tüm kullanıcılar aynı organization'a ait olmalı
2. **Email Doğrulama**: Production'da email doğrulama aktif olmalı
3. **Şifre Güvenliği**: Güçlü şifreler kullanın
4. **Yedekleme**: Kullanıcı silmeden önce yedek alın

## 🆘 Sorun Giderme

### "Kullanıcı bulunamadı" Hatası
```sql
-- Kullanıcıyı kontrol et
SELECT * FROM profiles WHERE email = 'email@example.com';
```

### "Organization bulunamadı" Hatası
```sql
-- Organization'ı kontrol et
SELECT * FROM organizations WHERE id = 'org-id';
```

### Şifre Çalışmıyor
```sql
-- Şifreyi sıfırla
-- users/04-change-password.sql kullan
```

### Kullanıcı Giriş Yapamıyor
```sql
-- Auth durumunu kontrol et
SELECT 
  u.id,
  u.email,
  u.email_confirmed_at,
  u.banned_until
FROM auth.users u
WHERE u.email = 'email@example.com';
```
