# 🖥️ Ordevo Desktop

Electron tabanlı restoran yönetim sistemi.

## 🚀 Başlangıç

### Kurulum
```bash
npm install
```

### Development
```bash
npm run dev
```

### Build
```bash
npm run build
npm run electron:build
```

## 📦 Build Çıktıları

Build sonrası `release/` klasöründe:
- Windows: `.exe` installer
- macOS: `.dmg` installer
- Linux: `.AppImage`

## 🔧 Teknolojiler

- Electron
- React + TypeScript
- Vite
- Zustand (State Management)
- Supabase (Backend)

## 📝 Environment Variables

`.env` dosyası oluşturun:
```env
VITE_SUPABASE_URL=your_supabase_url
VITE_SUPABASE_ANON_KEY=your_supabase_anon_key
```

## 🎯 Özellikler

- Masa yönetimi
- Menü yönetimi
- Sipariş takibi
- Raporlama
- Yazıcı entegrasyonu
- Hava durumu
- ML Export
