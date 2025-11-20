# 🍽️ Ordevo - Restoran Yönetim Sistemi

Modern, hızlı ve kullanıcı dostu restoran yönetim sistemi. Electron (Desktop) ve React Native (Mobile) ile geliştirilmiştir.

## 📱 Platformlar

- **Desktop**: Windows, macOS, Linux (Electron)
- **Mobile**: iOS, Android (React Native + Expo)

## ✨ Özellikler

### 🖥️ Desktop (Electron)
- ✅ Masa yönetimi
- ✅ Menü yönetimi (kategoriler ve ürünler)
- ✅ Sipariş takibi
- ✅ Raporlama ve istatistikler
- ✅ Otomatik yazıcı entegrasyonu
- ✅ Hava durumu entegrasyonu
- ✅ ML Export (CSV)
- ✅ Günlük satış arşivleme
- ✅ Çoklu kullanıcı desteği (Owner, Manager, Cashier, Waiter)

### 📱 Mobile (React Native)
- ✅ Garson uygulaması
- ✅ Sipariş alma
- ✅ Masa seçimi
- ✅ Menü görüntüleme
- ✅ Sipariş detayları
- ✅ Realtime güncellemeler
- ✅ Modern UI/UX

## 🚀 Hızlı Başlangıç

### Gereksinimler
- Node.js 18+
- npm veya yarn
- Supabase hesabı

### 1. Projeyi Klonla
```bash
git clone https://github.com/yourusername/ordevo.git
cd ordevo
```

### 2. Database Kurulumu
```bash
# Supabase SQL Editor'de sırayla çalıştır:
database/setup/01-schema.sql
database/setup/02-rls-policies.sql
database/setup/03-realtime.sql
database/setup/04-functions.sql
```

Detaylı kurulum için: [DATABASE-SETUP.md](DATABASE-SETUP.md)

### 3. Desktop Uygulaması

```bash
cd desktop
npm install
npm run dev
```

**Build:**
```bash
npm run build
npm run electron:build
```

### 4. Mobile Uygulaması

```bash
cd mobile
npm install
npm start
```

**Build:**
```bash
# Android
eas build --platform android --profile preview

# iOS (Apple Developer hesabı gerekli)
eas build --platform ios --profile preview
```

## 📁 Proje Yapısı

```
ordevo/
├── desktop/              # Electron Desktop App
│   ├── electron/         # Electron main process
│   ├── src/              # React frontend
│   │   ├── components/   # UI components
│   │   ├── pages/        # Pages
│   │   ├── store/        # Zustand stores
│   │   └── lib/          # Utilities
│   └── package.json
│
├── mobile/               # React Native Mobile App
│   ├── screens/          # App screens
│   ├── lib/              # Utilities
│   ├── config/           # Configuration
│   └── package.json
│
├── database/             # Database Scripts
│   ├── setup/            # Initial setup
│   ├── users/            # User management
│   └── maintenance/      # Maintenance scripts
│
└── README.md
```

## 🔧 Teknolojiler

### Desktop
- **Framework**: Electron + React + TypeScript
- **UI**: React Router, Zustand
- **Build**: Vite, electron-builder
- **Database**: Supabase (PostgreSQL)

### Mobile
- **Framework**: React Native + Expo
- **Navigation**: React Navigation
- **UI**: React Native Paper, Expo Blur
- **Database**: Supabase (PostgreSQL)

### Database
- **Database**: PostgreSQL (Supabase)
- **ORM**: Supabase Client
- **Realtime**: Supabase Realtime
- **Auth**: Supabase Auth

## 🔐 Güvenlik

- Row Level Security (RLS) aktif
- Rol bazlı yetkilendirme
- Şifreler bcrypt ile hashlenir
- Environment variables ile hassas bilgiler korunur

## 👥 Kullanıcı Rolleri

| Rol | Yetki |
|-----|-------|
| **Owner** | Tüm yetkiler |
| **Manager** | Menü, masa, rapor yönetimi |
| **Cashier** | Sipariş kapatma, raporlar |
| **Waiter** | Sipariş alma, güncelleme |

## 📊 Database Schema

- `organizations` - Restoranlar
- `profiles` - Kullanıcılar
- `restaurant_tables` - Masalar
- `menu_categories` - Menü kategorileri
- `menu_items` - Menü ürünleri
- `orders` - Siparişler
- `order_items` - Sipariş ürünleri
- `organization_settings` - Ayarlar
- `category_printer_mappings` - Yazıcı eşleştirmeleri
- `weather_data` - Hava durumu
- `daily_sales_archive` - Günlük satış arşivi

## 🔄 Realtime

Tüm tablolar realtime subscription destekler:
- Sipariş güncellemeleri
- Masa durumu değişiklikleri
- Menü değişiklikleri

## 📝 Environment Variables

### Desktop (.env)
```env
VITE_SUPABASE_URL=your_supabase_url
VITE_SUPABASE_ANON_KEY=your_supabase_anon_key
```

### Mobile (.env)
```env
EXPO_PUBLIC_SUPABASE_URL=your_supabase_url
EXPO_PUBLIC_SUPABASE_ANON_KEY=your_supabase_anon_key
```

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add amazing feature'`)
4. Push yapın (`git push origin feature/amazing-feature`)
5. Pull Request açın

## 📄 Lisans

Bu proje özel bir projedir. Ticari kullanım için izin gereklidir.

## 📞 İletişim

- Email: m.sirinyilmaz6@gmail.com
- GitHub: [@yourusername](https://github.com/yourusername)

## 🙏 Teşekkürler

- [Supabase](https://supabase.com) - Backend ve Database
- [Electron](https://electronjs.org) - Desktop framework
- [Expo](https://expo.dev) - Mobile development
- [React](https://react.dev) - UI framework

---

Made with ❤️ for restaurants
