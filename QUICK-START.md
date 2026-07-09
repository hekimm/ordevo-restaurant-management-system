# Ordevo Hızlı Başlangıç

Bu hızlı başlangıç yeni ASP.NET Core backend ve Oracle veritabanı için geçerlidir. Eski Supabase kurulumu artık aktif geliştirme yolu değildir.

## 1. Altyapıyı Başlat

```bash
cd deploy
docker compose up -d
```

Oracle container ilk açılışta birkaç dakika sürebilir. Durumu görmek için:

```bash
docker compose ps
```

`oracle` servisi healthy olduktan sonra migration adımına geç.

## 2. Oracle Schema'yı Kur

```bash
./db-migrate.sh migrate
```

Migration durumu için:

```bash
./db-migrate.sh info
```

Bu komutlar Flyway container'ını kullanır ve `backend/db/migrations` altındaki Oracle SQL dosyalarını `ORDEVO` schema'sına uygular.

## 3. API'yi Çalıştır

```bash
cd ../backend
dotnet run --project src/Ordevo.Api
```

Kontrol:

```bash
curl http://localhost:5144/
curl http://localhost:5144/health/ready
```

İlk açılışta demo tenant ve owner kullanıcı seed edilir.

## 4. Web UI'yi Çalıştır

Ayrı bir terminalde:

```bash
cd backend
dotnet run --project src/Ordevo.Web
```

Web UI API'ye `OrdevoApi:BaseUrl` ayarı üzerinden bağlanır.

## 5. Mobile Uygulamayı Çalıştır

```bash
cd mobile
npm install
npm start
```

Mobil uygulama artık Supabase URL ve anon key kullanmaz. API adresi için `EXPO_PUBLIC_API_BASE_URL`, tenant için `EXPO_PUBLIC_TENANT_SLUG` kullanılır. Varsayılan geliştirme değerleri `http://localhost:5144` ve `demo` şeklindedir.

## 6. WPF Desktop

WPF uygulaması Windows üzerinde çalışır.

```powershell
cd backend\src\Ordevo.Desktop.Wpf
dotnet run
```

Tek dosya EXE almak için:

```powershell
dotnet publish .\Ordevo.Desktop.Wpf.csproj -p:PublishProfile=win-x64-singlefile
```

Publish çıktısı:

```text
backend\src\Ordevo.Desktop.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\Ordevo.Desktop.exe
```

## Giriş Bilgileri

| Alan | Değer |
| --- | --- |
| Tenant | `demo` |
| Email | `owner@ordevo.local` |
| Şifre | `Owner_Dev_2026!` |

## Eski Sürümden Farkı

Eski hızlı başlangıç Supabase SQL Editor, Electron desktop ve doğrudan Supabase client ayarlarıyla çalışıyordu. Yeni akışta önce Oracle container açılır, sonra Flyway migration'ları uygulanır, ardından ASP.NET Core API başlatılır. Web, WPF ve mobile istemciler veritabanına değil API'ye bağlanır.
