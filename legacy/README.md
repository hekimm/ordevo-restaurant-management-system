# Legacy (old version)

This folder holds the previous version of Ordevo. It is here for reference only, and the current system does not use any of it.

Back then the app was built on Supabase (PostgreSQL) with two clients that talked to Supabase directly: an Electron desktop app and a React Native mobile app. All of it is kept here:

- `desktop/`: the old Electron desktop app (React, TypeScript, Vite). Its replacement is the WPF client at `backend/src/Ordevo.Desktop.Wpf`.
- `mobile/`: the old React Native (Expo) app. It has since been migrated onto the .NET API, and the current version lives at the repository root in `mobile/`. This copy is the earlier Supabase version, kept for reference.
- `setup/`: the old Supabase schema, RLS policies, realtime configuration, functions, and sample data.
- `users/`: the old user management scripts (add a waiter, list users, delete a user, change a password).
- `maintenance/`: the old PostgreSQL and Supabase maintenance scripts.

Do not run any of this against the current system. The SQL is PostgreSQL and Supabase specific and will not work on Oracle, and both old clients target the Supabase backend that no longer exists. The active system lives at the repository root: the API and database are under `backend/`, and the real migrations are in `backend/db/migrations`.

If you want to see how a table, function, or flow moved from the old version to the new one, the mapping tables in the root `README.md` and in `DATABASE-SETUP.md` are the place to look.
