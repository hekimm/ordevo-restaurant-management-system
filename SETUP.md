# Setting up the Ordevo web app locally

This guide walks through getting the web app running on your own machine, from an empty checkout to a logged-in browser. It focuses on the web client, but the web app is useless on its own, so this guide also covers the two things it depends on: the API and the Oracle database.

By the end you will have three things running:

- Oracle in a container, holding the schema and data
- the API on `http://localhost:5144`, talking to Oracle
- the web app on `http://localhost:5100`, talking to the API

The whole chain is: your browser talks to the web app, the web app talks to the API, and the API talks to Oracle. If any link is down the one above it will look broken, so it helps to bring them up in that order and check each one before moving on.

## Prerequisites

You need these installed first:

- .NET 10 SDK. Check it with `dotnet --version`; you want a `10.x` version.
- Docker and Docker Compose. Check with `docker --version` and `docker compose version`. Oracle runs in a container, so Docker has to be working before anything else.
- Git, to get the code.

That is all you need for the web path. The compose file also defines Redis, RabbitMQ, MinIO, and Seq, but the API only needs Oracle to boot, so you can leave the rest stopped for now.

## Step 1: Get the code

```bash
git clone <your-repo-url> ordevo
cd ordevo/ordevo-restaurant-management-system
```

Everything below is run from that `ordevo-restaurant-management-system` folder unless a step says otherwise.

## Step 2: Start Oracle

```bash
cd deploy
docker compose up -d oracle
```

This pulls the Oracle Database Free image and starts a container named `ordevo-oracle` with the port `1521` published. The first pull is a few gigabytes, so give it time.

The first boot is slow. Oracle initializes its data files the first time it comes up, and that usually takes two to three minutes. Do not run migrations until it reports healthy. You can watch it:

```bash
docker compose ps
docker logs -f ordevo-oracle
```

Wait until `docker compose ps` shows the oracle service as `healthy`. On later restarts it comes up much faster because the data files already exist (they live in a named volume, so they survive `docker compose down`).

## Step 3: Apply the database migrations

Once Oracle is healthy, create the schema and the PL/SQL packages with Flyway:

```bash
./db-migrate.sh migrate
```

This runs the Flyway container against the compose network and applies every `V*.sql` file in `backend/db/migrations` in order, connecting as the `ORDEVO` schema. When it finishes it prints the versions it applied. If you run it again later it only applies new files, so it is safe to re-run.

If you want to see the current state without changing anything, use `./db-migrate.sh info`.

## Step 4: Run the API

The web app cannot do anything until the API is up, so start it next. From the `backend` folder:

```bash
cd ../backend
dotnet run --project src/Ordevo.Api
```

The API listens on `http://localhost:5144`. On its first startup it seeds a demo tenant: branches, roles, permissions, and an owner account. That seeding is why the first run takes a little longer than later ones.

Check it is actually ready before moving on:

```bash
curl http://localhost:5144/health/ready
```

You want a healthy response. `health/ready` includes the Oracle check, so if this fails the usual cause is that Oracle is not up yet or the migrations have not run. There is also `health/live` for a lighter check that does not touch the database.

Leave this running in its own terminal.

## Step 5: Run the web app

In a new terminal, from the `backend` folder:

```bash
dotnet run --project src/Ordevo.Web --launch-profile http
```

The web app listens on `http://localhost:5100`. The `http` launch profile sets `ASPNETCORE_ENVIRONMENT=Development`, and in Development the app reads `OrdevoApi:BaseUrl` from `appsettings.Development.json`, which points at `http://localhost:5144`. That is how the web app finds your local API. In other environments the base URL comes from `appsettings.json` instead, so the setting is the single knob that decides which API the web app talks to.

Quick check that the web process itself is up:

```bash
curl http://localhost:5100/ui-health
```

## Step 6: Log in

Open `http://localhost:5100` in a browser. You will land on the login page. Use the development seed account:

- Tenant: `demo`
- Email: `owner@ordevo.local`
- Password: `Owner_Dev_2026!`

After you sign in you are on the dashboard. From there you can open the tables screen, start a tab, send items to the kitchen, watch them on the kitchen board, and take a payment. If the login form just reloads instead of taking you in, the web app almost certainly could not reach the API; jump to Troubleshooting.

These credentials are for development only. A real deployment needs its own secrets, its own signing key, and a different database password.

## Working on the front end

There is no build step for the front end, which makes the loop short. The CSS lives in `Ordevo.Web/wwwroot/css/ordevo` as twelve numbered modules, and the JavaScript is in `Ordevo.Web/wwwroot/js`. Edit a `.css` or `.js` file, save, and reload the page. The files are served straight off disk and cache-busted per request, so you see the change immediately without restarting anything.

Two things to keep in mind:

- The CSS modules load in order, from `01` to `12`, and that order is the cascade order. The list of `<link>` tags lives in `Pages/Shared/_OrdevoStyles.cshtml`. If you add a module, put it at the right numbered slot and add its link there too.
- A change to a `.cshtml` file is different. Razor pages are compiled, so editing a page or the layout means you have to stop the web process and start it again to see it.

## Running it a different way

`dotnet run` is the simplest way while you are developing, but two variations come up often.

If you prefer HTTPS locally, use `--launch-profile https`, which serves `https://localhost:7100` alongside the http port. You may need to trust the dev certificate once with `dotnet dev-certs https --trust`.

If you run the compiled DLL directly instead of `dotnet run`, the launch profile does not apply, so you have to set the environment yourself:

```bash
dotnet build -c Release
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5100 \
  dotnet src/Ordevo.Web/bin/Release/net10.0/Ordevo.Web.dll
```

Without `ASPNETCORE_URLS` it falls back to a default port, and without `ASPNETCORE_ENVIRONMENT=Development` it will try the production API base URL. Both catch people out.

## Troubleshooting

The login page reloads and never signs you in. This is almost always the web app failing to reach the API. Confirm the API answers on `curl http://localhost:5144/health/ready`, and that the web app is in the Development environment so it is pointed at `http://localhost:5144`. If the API went down after you had been using the app, you will also see repeated "session expired" style messages, for the same reason.

`health/ready` fails on the API. Oracle is not ready or the migrations did not run. Check `docker compose ps` shows oracle as healthy, then re-run `./db-migrate.sh info` to confirm the schema is there.

A port is already in use. Something is still bound to `5144` or `5100`, usually an old run you did not stop. Find it with `ss -ltnp | grep 5100` (or `5144`) and stop that process, then start again. When you stop and immediately restart, give the port a second or two to free up.

Oracle keeps restarting or never turns healthy. Give the first boot the full two to three minutes. If it still will not settle, look at `docker logs ordevo-oracle`. A common cause is not enough memory or disk for the container. If the data volume got into a bad state during a very first boot, `docker compose down -v` clears it so you can start clean, but note that `-v` deletes the database, so only do that before you have data you care about.

Migrations cannot connect. The migrate script talks to Oracle over the compose network. Make sure you started Oracle with `docker compose up` from the `deploy` folder so the network exists, and that Oracle is healthy first.

## How the pieces talk, one more time

- Browser to web app: a cookie session (`ordevo.web`). The browser never holds the API token.
- Web app to API: `OrdevoApiClient` attaches the API JWT on the server side and refreshes it quietly when needed.
- API to Oracle: Dapper and PL/SQL packages, with the money, stock, and tab rules inside a single transaction.
- Realtime: the POS and kitchen screens also open SignalR connections to the API, and fall back to polling if that connection cannot be made.

If you keep that chain in mind, most problems point at whichever link is down, and you can check them from the bottom up: Oracle, then the API, then the web app.
