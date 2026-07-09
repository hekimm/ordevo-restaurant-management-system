# Ordevo Backend

Bu klasör Ordevo'nun yeni backend tarafıdır. Eski sürümde client uygulamaları Supabase'e doğrudan bağlanıyordu. Yeni sürümde bütün client'lar ASP.NET Core API üzerinden konuşur; database erişimi, transaction sınırı, yetkilendirme, realtime yayın ve offline sync sözleşmeleri backend içinde toplanır.

Backend .NET 10 ile yazılmış modular monolith yapısındadır. Veritabanı Oracle'dır. Basit read/write işlemlerinde Dapper kullanılır, kritik iş kuralları PL/SQL package içinde çalışır.

## Yapı

```text
backend/
├── Ordevo.slnx
├── db/
│   ├── flyway.conf
│   └── migrations/
└── src/
    ├── Ordevo.Api/
    ├── Ordevo.Web/
    ├── Ordevo.Desktop.Wpf/
    ├── Ordevo.BuildingBlocks/
    └── Modules/
```

## Ana Projeler

| Proje | Rol |
| --- | --- |
| `Ordevo.Api` | REST endpoint'leri, SignalR hub'ları, health check, OpenAPI ve module bootstrap |
| `Ordevo.Web` | Razor Pages tabanlı web UI |
| `Ordevo.Desktop.Wpf` | Windows desktop executable istemcisi |
| `Ordevo.BuildingBlocks` | Oracle connection factory, Dapper ayarları, tenant context, JWT, validation ve Result modeli |
| `Modules/*` | Identity, Menu, Ordering, Payment, Kitchen, Inventory, Shift, Reporting, Finance, Print, CRM, Sync, Integration, EInvoice |

## Request Akışı

1. Client API'ye JWT access token ile gelir.
2. `TenantContext` tenant, branch ve user bilgisini token claim'lerinden okur.
3. Endpoint ilgili application service'e gider.
4. Service basit okuma/yazma için repository kullanır veya kritik işlem için PL/SQL package çağırır.
5. Sonuç `Result` modeliyle API boundary'ye döner.
6. Gerekirse SignalR üzerinden order, table veya KDS event'i yayınlanır.

Bu akış eski Supabase client modelinden farklıdır. Client artık tablo yapısını veya SQL fonksiyonlarını bilmez; HTTP contract'ını bilir.

## Modül Notları

- Identity, Supabase Auth yerine geçer. Login, refresh token rotation, logout, user management ve permission seeding burada.
- Ordering, adisyon lifecycle için `PKG_ORDERING` kullanır. Client fiyat hesaplamaz; fiyat backend ve database tarafından belirlenir.
- Payment, ödeme kaydı ve order close akışını `PKG_PAYMENT` ile transaction içinde yürütür.
- Inventory, stok hareketlerini ledger gibi tutar. Order kapanınca stok düşümü database trigger/package akışına bağlıdır.
- Shift, kasa oturumunu ve Z rapor hesabını database tarafında tutarlı hesaplar.
- Reporting, API için hızlı read modelleri ve Oracle materialized view kullanır.
- Sync, offline desktop/mobile için server outbox ve client mutation inbox sağlar.
- Integration ve EInvoice, dış sistemlerle ilgili kalıcı durumları backend arkasında tutar.

## Veritabanı

Aktif migration klasörü `backend/db/migrations` altındadır. Flyway sıralı `V*.sql` dosyalarını `ORDEVO` schema'sına uygular.

Local geliştirme:

```bash
cd ../deploy
docker compose up -d
./db-migrate.sh migrate
cd ../backend
```

Migration durumunu görmek için:

```bash
./db-migrate.sh info
```

Oracle bağlantısı development için `src/Ordevo.Api/appsettings.json` içinde tanımlıdır. Production değerleri buraya yazılmamalı; secret kaynağından gelmelidir.

## API Çalıştırma

```bash
dotnet run --project src/Ordevo.Api
```

Kontrol:

```bash
curl http://localhost:5144/
curl http://localhost:5144/health/ready
```

Development seed açıksa ilk açılışta `demo` tenant ve `owner@ordevo.local` kullanıcısı oluşturulur.

## Web UI

```bash
dotnet run --project src/Ordevo.Web
```

Web UI API'ye `OrdevoApi:BaseUrl` ile bağlanır. Web UI desktop EXE'nin yerine geçmez; ayrı bir browser client'tır.

## WPF Desktop

Windows üzerinde:

```powershell
dotnet run --project src\Ordevo.Desktop.Wpf
```

Publish:

```powershell
dotnet publish src\Ordevo.Desktop.Wpf\Ordevo.Desktop.Wpf.csproj -p:PublishProfile=win-x64-singlefile
```

## Endpoint Grupları

| Grup | Prefix |
| --- | --- |
| Identity auth | `/api/identity/auth` |
| Identity users | `/api/identity/users` |
| Settings | `/api/settings` |
| Menu | `/api/menu` |
| Ordering tables | `/api/ordering` |
| Ordering orders | `/api/ordering/orders` |
| Payment | `/api/payment` |
| Kitchen | `/api/kitchen` |
| Inventory | `/api/inventory` |
| Shift | `/api/shift` |
| Reporting | `/api/reporting` |
| Finance | `/api/finance` |
| Print | `/api/print` |
| CRM | `/api/m9-crm` |
| Sync | `/api/sync` |
| Integration | `/api/integrations` |
| EInvoice | `/api/einvoice` |

SignalR hub'ları:

- `/hubs/orders`
- `/hubs/tables`
- `/hubs/kds`

## Eski Sürümden Geçiş

Eski backend aslında Supabase idi. PostgreSQL tabloları, RLS policy'leri, Supabase Realtime ve birkaç PostgreSQL function proje davranışını taşıyordu. Yeni backend bu davranışları daha açık katmanlara böldü.

| Eski sorumluluk | Yeni yer |
| --- | --- |
| Supabase Auth | Identity modülü |
| RLS policy | JWT claim, authorization policy ve tenant-scoped query |
| Supabase Realtime | SignalR |
| PostgreSQL function | Oracle PL/SQL package |
| Client içi iş kuralı | Application service veya PL/SQL transaction |
| Electron desktop | WPF desktop |
| Supabase env değerleri | API base URL, JWT ve Oracle connection string |

## Geliştirme Notları

Yeni module eklerken module projesi `Modules` altında durmalı, `IModule` implement etmeli ve `Ordevo.Api/Modules/ModuleRegistry.cs` listesine eklenmelidir.

Tenant'a ait her query `TENANT_ID` filtresiyle yazılmalıdır. Client'tan gelen tenant değeri güvenilir kabul edilmez; tenant context token üzerinden alınır.

Para, stok, ödeme ve adisyon gibi çift yazma riski olan işler PL/SQL package içinde kalmalıdır. API bu işlemleri orchestration ve response mapping için kullanır.
