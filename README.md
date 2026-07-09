# Ordevo

Ordevo is a restaurant operations platform: tables and tabs (adisyon), a kitchen display, payments, inventory, shift and cash reconciliation, reporting, CRM, printing, and fiscal/e-invoice integration. This document explains how the project got to its current shape, and then goes deep on the piece I am shipping now: the web application.

If you just want to get it running, read [SETUP.md](SETUP.md).

## Where this came from

The first version of Ordevo was client-heavy. The desktop app was Electron plus React and Vite, the mobile app was React Native with Expo, and both of them talked to Supabase (PostgreSQL) directly. Auth went through Supabase Auth and a `profiles` table, access rules lived in Row Level Security, and a fair amount of business logic sat in Postgres functions and in the client code.

```
   Electron desktop        Mobile (Expo/RN)
          |                       |
          +-----------+-----------+
                      |
                      v
                  Supabase
        (PostgreSQL, Auth, RLS, Realtime,
         SQL functions; clients connect directly)
```

That setup was quick to start with, but the backend boundary was never clearly drawn. The rules for closing a tab, taking a payment, deducting stock, reconciling a shift, and syncing offline devices were spread across three places: the client, the SQL functions, and the app flow. For those operations you really want one transaction boundary and one place that owns the rules.

So the core was rebuilt. The new architecture is an ASP.NET Core (.NET 10) modular monolith backed by Oracle, with the critical rules moved into PL/SQL packages so money, stock, and tab state stay inside a single database transaction. The clients no longer touch the database. Web, desktop, and mobile all go through the same HTTP API, and that API owns tenancy, users, permissions, transactions, and realtime.

The old version is kept out of the way in a single `legacy/` folder. Everything at the repository root is the current system; `legacy/` holds the archived old version in full (the Supabase SQL, the Electron desktop app, and the old Supabase mobile app), kept only as a reference for when you need to see how something used to work or how an old table or function became a new one.

| Old piece | New home |
| --- | --- |
| Electron desktop (`legacy/desktop/`) | `backend/src/Ordevo.Desktop.Wpf` (WPF) |
| Supabase mobile app (`legacy/mobile/`) | `mobile/`, migrated onto the .NET API |
| Supabase Auth, `profiles` | Identity module: JWT access token + rotating refresh token, `USERS`/`USER_ROLES`/`USER_BRANCHES` |
| `organizations` | `TENANTS` |
| `restaurant_tables` | `DINING_TABLES` and `TABLE_SECTIONS` |
| Supabase RLS | API authorization policies, JWT claims, tenant-scoped queries |
| Supabase Realtime | SignalR hubs: `/hubs/orders`, `/hubs/tables`, `/hubs/kds` |
| PostgreSQL functions | Oracle PL/SQL packages (`PKG_ORDERING`, `PKG_PAYMENT`, ...) |
| `legacy/setup/*.sql` | `backend/db/migrations/V*.sql` (Flyway) |
| Supabase query from the client | API endpoint calls |

## The architecture today

There is one backend and, on top of it, a web app, a desktop app, and two mobile apps. Only the web app is finished; the others are in active development on the same API and the same architecture.

```
   Web (Razor Pages)   WPF desktop   Waiter mobile   Owner mobile
         |                  |              |               |
         +--------+---------+------+-------+------+--------+
                  |                        |
                  v                        v
         ASP.NET Core API  ---- SignalR ---->  clients (realtime)
         (.NET 10, modular monolith)
                  |
                  v
         Oracle + PL/SQL packages
         (transactions live here)
```

- Backend API: `backend/src/Ordevo.Api`, modules under `backend/src/Modules`, shared infrastructure in `Ordevo.BuildingBlocks` (Oracle connection factory, Dapper setup, tenant context, JWT auth, the `Result`/`Error` model, and the minimal-API validation filter).
- Database: Oracle, schema and packages created by Flyway migrations in `backend/db/migrations`.
- Web UI: `backend/src/Ordevo.Web`, the subject of the rest of this document.

### Backend modules

| Module | Responsibility |
| --- | --- |
| Identity | Tenant, branch, user, role, permission, JWT login, refresh rotation, bootstrap seed |
| Menu | Categories, items, modifiers, barcodes, and the combined menu tree the POS reads |
| Ordering | Tables, tabs, order-item lifecycle, transfer/merge/split, orders and tables hubs |
| Payment | Multi-tender payment, refund, invoice, and the order-close hand-off |
| Kitchen | KDS stations, the kitchen board, and item status flow |
| Inventory | Stock items, recipes, suppliers, purchases, wastage, and stock movements |
| Shift | Cash registers, shift sessions, cash movements, and the Z report |
| Reporting | Daily sales, top items, hourly and category/payment breakdowns, ML CSV export |
| Finance | Income and expense, accounts, counterparties, and cash flow |
| Print | Account and kitchen tickets, ESC/POS payloads, and the print job queue |
| M9Crm | Customers, addresses, loyalty, campaigns, reservations, couriers, delivery zones |
| Sync | Offline device registration, server outbox, client mutations, conflict records |
| Integration | Connectors, webhooks, terminals, and outbound commands to external systems |
| EInvoice | e-Fatura / e-Arsiv documents behind a provider abstraction |

## The web application

`Ordevo.Web` is a server-rendered ASP.NET Core Razor Pages app. It is the operator-facing surface: the screen a cashier, waiter, kitchen line, or owner actually uses on a terminal or a browser. It does not contain business logic of its own. Every screen reads and writes through the API, and the API talks to Oracle.

### Why Razor Pages

I wanted the web client to be simple to run and simple to reason about. Razor Pages gives me server-side rendering with real routing, model binding, and antiforgery out of the box, and it lets me keep the API token on the server. There is no SPA build step and no client-side bundler to babysit. The interactive screens (the POS and the kitchen board) are plain JavaScript that calls back into named page handlers, so the parts that need to feel instant are dynamic, and everything else is a normal server-rendered page.

### How a request flows

The important detail is that the browser never sees the API JWT.

```
browser  ->  Razor page / named handler  ->  OrdevoApiClient  ->  API  ->  Oracle
   (cookie: ordevo.web)          (holds the JWT server-side)
```

When you log in, the web app calls the API, receives the access and refresh tokens, and stores them inside the authentication cookie's token store on the server. The browser only ever holds an HttpOnly cookie. On each request, `Api/OrdevoApiClient.cs` attaches the access token to the outgoing API call, refreshes it transparently when it is close to expiring or gets a 401, and rotates the stored tokens. The refresh runs at most once per request because refresh tokens rotate with server-side reuse detection, and refreshing twice would invalidate the whole token family.

The dynamic screens (POS, kitchen) post to Razor named handlers such as `/tables?handler=Pay` rather than calling the API directly from JavaScript. That keeps the token on the server and lets antiforgery protect the writes. The JavaScript sends the antiforgery token in a `RequestVerificationToken` header.

### Screen map

Every page except the login and access-denied pages requires an authenticated cookie. Authorization is applied to the whole `/` folder in `Program.cs`.

| Route | Screen | What it does |
| --- | --- | --- |
| `/dashboard` | Dashboard | Operations overview: daily revenue, occupancy, a venue seat map by section, and the live kitchen queue |
| `/tables` | Masalar (POS) | The main POS. Table cards by section and takeaway packages, and the full tab (adisyon) view: menu, order lines, quantity and void, discount, comp, transfer/merge/split, multi-tender payment, receipt |
| `/tables/manage` | Table management | Section and table CRUD |
| `/kitchen` | Mutfak (KDS) | Kitchen display board: tickets grouped by order and stage, station filters, timers, and one-tap status advance, live over SignalR |
| `/menu` | Menu | Category, item, and modifier management |
| `/crm` | CRM | Customers and reservations |
| `/inventory` | Stok | Stock items, units, suppliers, purchases, and adjustments |
| `/personel-cihaz` | Personel / Cihaz | Waiter accounts and PINs plus the cash register and shift flow |
| `/finance` | Finans | Income and expense, accounts, and counterparties |
| `/printer` | Yazici | Printer status, setup, and print jobs |
| `/sales-analysis` | Satis Analizi | Sales KPIs and charts |
| `/ayarlar` | Ayarlar | A password-gated developer area: module and integration toggles, fiscal integration, and sync |
| `/login`, `/logout` | Auth | Cookie sign-in and sign-out |

### Front-end assets

There is no build pipeline for the front end. The assets are plain files under `wwwroot`, and Bootstrap 5.3, Bootstrap Icons, and the Nunito font come from a CDN.

JavaScript (`wwwroot/js`):

- `ordevo.js`: shared UI helpers used everywhere. Toasts, the confirm dialog, list filtering, and a friendly-error mapper that turns API error codes and raw status codes into human messages.
- `pos.js`: the tables hub and the tab view. Table and package cards, the menu, order editing, discounts, transfer/merge/split, and the payment modal.
- `kds.js`: the kitchen board. Ticket rendering, stage transitions, station filters, timers, an audio alert, and the SignalR connection with a polling fallback.

CSS (`wwwroot/css/ordevo`): the stylesheet used to be one large `ordevo.css`. It was trimmed of dead rules and then split into twelve ordered modules for readability, from `01-foundation.css` to `12-kds-polish.css`. The load order matters because it is the CSS cascade order, so the modules are listed, in order, in the shared partial `Pages/Shared/_OrdevoStyles.cshtml`, which both `_Layout.cshtml` and `Login.cshtml` pull in. If you add a module, drop it in at its numbered slot and add the matching `<link>` in that partial.

### Realtime

The POS and kitchen screens open SignalR connections to the API hubs (`/hubs/orders`, `/hubs/tables`, `/hubs/kds`) using the access token through `accessTokenFactory`. When an order changes, the kitchen board refreshes within a debounce window instead of waiting for the next poll. If SignalR cannot connect, both screens fall back to periodic polling, so nothing breaks on a flaky network.

## Repository layout

Everything at the root is the current version. The previous version is isolated in a single `legacy/` folder so it never gets in the way.

```
ordevo-restaurant-management-system/
  backend/
    Ordevo.slnx
    db/
      flyway.conf
      migrations/            Flyway V*.sql: schema and PL/SQL packages
    src/
      Ordevo.Api/            the HTTP API
      Ordevo.Web/            the web app (this document)
      Ordevo.Desktop.Wpf/    the Windows desktop client
      Ordevo.BuildingBlocks/ shared infrastructure
      Modules/               one folder per module
  deploy/
    docker-compose.yml       Oracle, Redis, RabbitMQ, MinIO, Seq
    db-migrate.sh            runs Flyway against the compose network
    oracle-init/
  mobile/                    Expo / React Native (waiter ordering app, in progress)
  legacy/                    the old version in full, archived (Supabase SQL + Electron desktop + old mobile) for reference only
```

## Running it

The full walkthrough is in [SETUP.md](SETUP.md). The short version:

```bash
cd deploy && docker compose up -d        # start Oracle (first boot is slow)
./db-migrate.sh migrate                  # apply Flyway migrations
cd ../backend
dotnet run --project src/Ordevo.Api      # API on http://localhost:5144
dotnet run --project src/Ordevo.Web --launch-profile http   # web on http://localhost:5100
```

Then open `http://localhost:5100`, and sign in with the development seed account: tenant `demo`, email `owner@ordevo.local`, password `Owner_Dev_2026!`.

## Status

The web client is complete and is what I am shipping in this pass. It exercises the whole stack end to end: log in, open a tab, send items to the kitchen, watch them move across the KDS in realtime, take a multi-tender payment, close the tab with an invoice, and see stock deducted and the shift reconciled.

The other clients are under active development, all on the same .NET API and the same architecture as the web app:

- Desktop app: the WPF client (`backend/src/Ordevo.Desktop.Wpf`) is being continued on the same tech stack.
- Waiter ordering app: a mobile app for waiters to take orders at the table.
- Owner management app: a separate mobile app for the restaurant owner, focused on management and oversight (reporting, monitoring, and control).

These are still in progress and are not part of this release; the web app is.

## Related documentation

- [SETUP.md](SETUP.md): step-by-step local setup for the web app
- `QUICK-START.md`: a shorter bring-up checklist
- `DATABASE-SETUP.md`: Oracle, Flyway, and where the old Supabase scripts landed
- `backend/README.md`: the API, its modules, and the migration flow

## Contributing

Contributions are genuinely welcome. If you find a bug, have an idea, or want to help build out one of the in-progress clients, please open an issue or a pull request. It helps to start with a short issue describing the problem or the change so I can give feedback on the direction before you spend real time on it. Keep pull requests focused, follow the style of the code around what you are touching, and explain the reasoning behind non-obvious decisions.

If the project is useful to you, or you like where it is going, a star is appreciated. It is a small thing, but it is a real signal that the work is worth continuing, and it helps other people find it.

## Usage and licensing

This is source-available so you can read it, run it locally, learn from it, and build on it for personal, educational, and non-commercial work. Commercial use is a separate matter and requires prior written permission from the maintainer, so if you are planning to use Ordevo, or any meaningful part of it, in a product or a paid setting, please get in touch first and I will sort out the terms with you. I would rather have that conversation early than have anyone build on an assumption that turns out to be wrong.
