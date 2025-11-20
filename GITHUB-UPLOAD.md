# 🚀 GitHub'a Yükleme Rehberi

## 1️⃣ Git Repository Oluştur

### GitHub'da:
1. https://github.com/new adresine git
2. Repository adı: `ordevo`
3. Description: `Modern Restoran Yönetim Sistemi`
4. Private/Public seç
5. **Initialize this repository with a README** seçme (zaten var)
6. Create repository

## 2️⃣ Local Git Başlat

```bash
# Git başlat
git init

# Dosyaları ekle
git add .

# İlk commit
git commit -m "Initial commit: Ordevo Restaurant Management System"

# Remote ekle (GitHub'dan aldığın URL)
git remote add origin https://github.com/KULLANICI_ADIN/ordevo.git

# Main branch'e push
git branch -M main
git push -u origin main
```

## 3️⃣ Hassas Bilgileri Kaldır

### ⚠️ Önemli: Push yapmadan önce kontrol et!

```bash
# .env dosyalarını kontrol et
cat desktop/.env
cat mobile/.env

# Eğer gerçek API key'ler varsa, .env.example oluştur:
```

**desktop/.env.example:**
```env
VITE_SUPABASE_URL=your_supabase_url_here
VITE_SUPABASE_ANON_KEY=your_supabase_anon_key_here
```

**mobile/.env.example:**
```env
EXPO_PUBLIC_SUPABASE_URL=your_supabase_url_here
EXPO_PUBLIC_SUPABASE_ANON_KEY=your_supabase_anon_key_here
```

## 4️⃣ .gitignore Kontrolü

```bash
# .gitignore'un çalıştığını kontrol et
git status

# Şunlar görünmemeli:
# - node_modules/
# - .env
# - dist/
# - build/
```

## 5️⃣ GitHub Repository Ayarları

### Branches:
- `main` - Production
- `develop` - Development
- `feature/*` - Yeni özellikler

### Branch Protection (Önerilir):
1. Settings > Branches
2. Add rule for `main`
3. ✅ Require pull request reviews
4. ✅ Require status checks to pass

## 6️⃣ GitHub Actions (Opsiyonel)

CI/CD için `.github/workflows/` klasörü oluşturabilirsiniz.

## ✅ Tamamlandı!

Repository linki: `https://github.com/KULLANICI_ADIN/ordevo`

## 📝 Sonraki Adımlar

1. README.md'de GitHub username'i güncelle
2. LICENSE dosyası ekle
3. CONTRIBUTING.md oluştur
4. GitHub Issues ve Projects kullan
5. Release tag'leri oluştur

## 🔄 Güncellemeler İçin

```bash
# Değişiklikleri ekle
git add .

# Commit
git commit -m "Açıklama"

# Push
git push origin main
```
