# Ordevo Desktop WPF

Bu proje Ordevo'nun yeni Windows desktop hedefidir. Eski desktop uygulaması Electron + React + Vite ile geliştiriliyordu. Bu sürümde desktop hedefi native WPF uygulamasıdır ve Windows için `.exe` olarak yayınlanır.

## Rolü

WPF istemcisi doğrudan Oracle'a bağlanmaz. ASP.NET Core API'ye login olur, JWT access token kullanır ve gerekli yerlerde refresh token ile oturumu yeniler.

Uygulamanın kapsadığı ana alanlar:

- Dashboard
- Masa ve adisyon yönetimi
- KDS ve mutfak akışı
- Menü yönetimi
- CRM
- Stok
- Vardiya
- Finans
- Yazdırma
- Entegrasyon
- Sync
- Raporlar ve ML CSV export

## Konfigürasyon

`appsettings.json`:

```json
{
  "OrdevoApi": {
    "BaseUrl": "http://localhost:5144",
    "TimeoutSeconds": 20
  },
  "Offline": {
    "DatabasePath": "ordevo-desktop-cache.db"
  }
}
```

Relative SQLite cache path değerleri `%LOCALAPPDATA%\Ordevo` altında çözülür.

## Geliştirme

Önce Oracle, migration ve API hazır olmalı.

```powershell
dotnet run --project ..\Ordevo.Api\Ordevo.Api.csproj --launch-profile http
```

Sonra WPF istemcisi:

```powershell
dotnet run --project .\Ordevo.Desktop.Wpf.csproj
```

## Publish

```powershell
dotnet publish .\Ordevo.Desktop.Wpf.csproj -p:PublishProfile=win-x64-singlefile
```

Çıktı:

```text
bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\Ordevo.Desktop.exe
```

## Eski Electron Sürümünden Farkı

Electron sürümünde UI React içinde çalışıyor, Supabase client ile backend ihtiyacının büyük kısmını doğrudan karşılıyordu. WPF sürümünde UI daha ince bir istemci olarak konumlandı. İş kuralları ASP.NET Core API ve Oracle PL/SQL katmanına taşındı. Bu yüzden desktop publish daha net, API contract'ı daha kontrollü ve Windows POS senaryosu için runtime daha sade hale geldi.

Linux ve macOS üzerinde WPF uygulaması çalıştırılmaz. Bu platformlarda web UI kullanılabilir.
