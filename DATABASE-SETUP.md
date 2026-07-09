# Ordevo Database Kurulum Rehberi

Aktif veritabanı kurulumu Oracle ve Flyway üstünden yapılır. Eski Supabase/PostgreSQL dosyaları repo içinde duruyor, fakat yeni backend onları çalıştırmaz. Bu ayrım önemli: `legacy/` klasörü eski sürümün dokümante edilmiş hali, `backend/db/migrations` ise yeni sürümün gerçek migration kaynağıdır.

## Önceki Database Yapısı

Eski sürüm Supabase PostgreSQL kullanıyordu. Kurulum Supabase SQL Editor'de şu dosyaları sırayla çalıştırarak yapılıyordu:

- `legacy/setup/01-schema.sql`
- `legacy/setup/02-rls-policies.sql`
- `legacy/setup/03-realtime.sql`
- `legacy/setup/04-functions.sql`
- `legacy/setup/05-sample-data.sql`

O modelde ana tablolar `organizations`, `profiles`, `restaurant_tables`, `menu_categories`, `menu_items`, `orders`, `order_items`, `organization_settings`, `category_printer_mappings`, `weather_data` ve `daily_sales_archive` idi. Tenant izolasyonu Supabase RLS ile sağlanıyordu. Realtime davranışı Supabase publication üzerinden veriliyordu. Kullanıcı açma ve şifre değiştirme gibi işler `legacy/users` altındaki PostgreSQL scriptleriyle destekleniyordu.

Yeni mimaride bu model taşındı ama bire bir kopyalanmadı. Tenant ve branch ayrımı netleştirildi, kullanıcı ve izin modeli genişletildi, adisyon ve ödeme gibi kritik iş kuralları PL/SQL paketlerine alındı.

## Yeni Database Yapısı

Yeni aktif database Oracle'dır. Local geliştirme için `deploy/docker-compose.yml` Oracle 23ai Free container'ı açar. Schema Oracle 19c/23ai portable kalacak şekilde yazılmıştır.

Temel kurallar:

- Primary key değerleri uygulama tarafından üretilen `VARCHAR2(36)` GUID değerleridir.
- Boolean alanlar `NUMBER(1)` ve `CHECK` constraint ile tutulur.
- JSON alanları `CLOB` ve `IS JSON` constraint ile tutulur.
- Para alanları `NUMBER(18,4)` kullanır.
- Tenant'a ait tablolarda `TENANT_ID`, audit kolonları ve `ROW_VERSION` bulunur.
- Kritik mutation'lar PL/SQL package içinde transaction olarak çalışır.

## Kurulum

Altyapıyı başlat:

```bash
cd deploy
docker compose up -d
```

Oracle ilk açılışta `deploy/oracle-init/01-create-ordevo-user.sql` dosyasını çalıştırır. Bu dosya `FREEPDB1` içinde `ORDEVO` kullanıcısını oluşturur ve migration için gereken yetkileri verir.

Migration'ları uygula:

```bash
./db-migrate.sh migrate
```

Durumu kontrol et:

```bash
./db-migrate.sh info
./db-migrate.sh validate
```

Flyway ayarları `backend/db/flyway.conf` içindedir. Script, migration klasörünü Flyway container'ına read-only mount eder ve compose network içindeki `oracle:1521/FREEPDB1` servisine bağlanır.

## Migration Dosyaları

| Migration | İçerik |
| --- | --- |
| `V1__baseline_platform.sql` | Platform ayarları ve ortak schema konvansiyonları |
| `V2__identity.sql` | Tenant, branch, role, permission, user, device, refresh token ve audit log |
| `V3__menu.sql` | Menü kategorileri, ürünler, modifier, price list, combo ve barkod |
| `V4__ordering.sql` | Masa, adisyon, order item, modifier, indirim ve transfer tabloları |
| `V5__pkg_ordering.sql` | Adisyon lifecycle PL/SQL paketi |
| `V6__payment.sql` | Ödeme, refund, invoice ve cash movement tabloları |
| `V7__pkg_payment.sql` | Ödeme ve order close PL/SQL paketi |
| `V8__kitchen.sql` | KDS istasyonları |
| `V9__inventory.sql` | Stok, reçete, tedarikçi, satın alma, fire ve sayım tabloları |
| `V10__pkg_inventory.sql` | Stok hareketleri ve otomatik stok düşümü |
| `V11__shift.sql` | Kasa ve vardiya oturumu tabloları |
| `V12__pkg_shift.sql` | Vardiya lifecycle ve Z rapor hesapları |
| `V13__reporting.sql` | Günlük satış arşivi, materialized view ve reporting paketi |
| `V14__m9_crm.sql` | CRM, sadakat, kampanya, rezervasyon ve teslimat tabloları |
| `V15__pkg_m9_crm.sql` | CRM ve kampanya iş kuralları |
| `V16__sync.sql` | Offline sync outbox, checkpoint, client mutation ve conflict tabloları |
| `V17__pkg_sync.sql` | Offline sync PL/SQL paketi |
| `V18__integration.sql` | Connector, webhook, terminal ve command tabloları |
| `V19__pkg_integration.sql` | Entegrasyon lifecycle paketi |
| `V20__finance_print.sql` | Finans kayıtları, print template ve print job tabloları |
| `V21__einvoice.sql` | e-Fatura/e-Arşiv doküman tablosu |

## Eski Tablo Karşılıkları

| Eski PostgreSQL | Yeni Oracle |
| --- | --- |
| `organizations` | `TENANTS` |
| `profiles` | `USERS`, `USER_ROLES`, `USER_BRANCHES` |
| `restaurant_tables` | `DINING_TABLES`, `TABLE_SECTIONS` |
| `menu_categories` | `MENU_CATEGORIES` |
| `menu_items` | `MENU_ITEMS`, `MODIFIER_GROUPS`, `MODIFIERS` |
| `orders` | `ORDERS` |
| `order_items` | `ORDER_ITEMS`, `ORDER_ITEM_MODIFIERS` |
| `daily_sales_archive` | `DAILY_SALES_ARCHIVE`, `MV_DAILY_SALES` |
| Supabase RLS | API authorization, JWT claims ve tenant-scoped queries |
| Supabase Realtime | SignalR hub'ları |
| PostgreSQL functions | Oracle PL/SQL packages |

## Kontrol Komutları

API hazır mı:

```bash
curl http://localhost:5144/health/ready
```

Migration durumu:

```bash
cd deploy
./db-migrate.sh info
```

Oracle container logları:

```bash
cd deploy
docker compose logs oracle
```

## Development Bağlantı Bilgileri

| Alan | Değer |
| --- | --- |
| JDBC URL | `jdbc:oracle:thin:@//oracle:1521/FREEPDB1` |
| Local API connection string | `User Id=ORDEVO;Password=Ordevo_Dev_2026;Data Source=localhost:1521/FREEPDB1` |
| Schema | `ORDEVO` |
| SYS password | `Oracle_Dev_2026` |

Bu değerler sadece local development içindir. Production'da secret yönetimi, ayrı schema parolası, backup policy ve migration onayı gerekir.

## Sorun Giderme

Oracle healthy olmadan migration çalıştırılırsa bağlantı hatası alınır. `docker compose ps` çıktısında Oracle healthy görünene kadar beklemek gerekir.

`ORDEVO` kullanıcısı yoksa container ilk kurulum scriptini çalıştırmamış olabilir. Volume daha önce hatalı bir durumda oluştuysa local development için volume silinip tekrar oluşturulabilir.

Flyway checksum hatası alınırsa uygulanmış migration dosyası sonradan değişmiş demektir. Development ortamında sebep bilinmeden repair çalıştırmak yerine önce hangi dosyanın değiştiğini kontrol etmek daha sağlıklıdır.
