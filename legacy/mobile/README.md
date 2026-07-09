# 📱 Ordevo Mobile

React Native + Expo tabanlı garson uygulaması.

## 🚀 Başlangıç

### Kurulum
```bash
npm install
```

### Development
```bash
npm start
```

Sonra:
- `i` - iOS simulator
- `a` - Android emulator
- QR kod ile fiziksel cihazda test

### Build

#### Android
```bash
eas build --platform android --profile preview
```

#### iOS (Apple Developer hesabı gerekli)
```bash
eas build --platform ios --profile preview
```

## 🔧 Teknolojiler

- React Native
- Expo
- TypeScript
- React Navigation
- Supabase (Backend)

## 📝 Environment Variables

`.env` dosyası oluşturun:
```env
EXPO_PUBLIC_SUPABASE_URL=your_supabase_url
EXPO_PUBLIC_SUPABASE_ANON_KEY=your_supabase_anon_key
```

## 🎯 Özellikler

- Sipariş alma
- Masa seçimi
- Menü görüntüleme
- Sipariş detayları
- Realtime güncellemeler
- Modern UI/UX
