# Freshservice Ticket Archive — Search Portal

A read-only web portal for browsing and searching **archived Freshservice tickets**.
It runs on **IIS** with **Windows (AD) Authentication** and reads directly from the
existing SQL Server archive database populated by the Freshservice archiver console app.

> This app **only reads** the archive tables. It never calls the Freshservice API and
> never writes to the database.

---

## What it does

- **Search & filter** tickets by free text (ticket #, subject, description, requester),
  status, priority, group, and created-date range.
- **Sortable, paged** results grid.
- **Ticket detail** view with full description, the complete conversation thread
  (public replies and private notes), requester/agent/group info, and category.
- **Attachment download** — serves files that the archiver saved to local/UNC storage.
- **Windows Authentication** — no login screen; the signed-in AD user is shown in the header.

---

## Architecture

```
┌─────────────────────────────────────────────┐
│ IIS site (Windows Authentication)            │
│                                              │
│  ASP.NET Core 8 (in-process)                 │
│   ├── /            → React SPA (wwwroot)      │
│   └── /api/*       → JSON API (Dapper)        │
│                        │                      │
└────────────────────────┼──────────────────────┘
                         │  Trusted_Connection (App Pool identity)
                         ▼
             SQL Server — FS_*_Archive tables
```

- **Backend:** ASP.NET Core 8 Web API, [Dapper](https://github.com/DapperLib/Dapper) +
  `Microsoft.Data.SqlClient`. Read-only, parameterized queries.
- **Frontend:** React 19 + TypeScript + Vite. HTML email/ticket bodies are sanitized with
  DOMPurify before rendering.
- **Single deployable:** `npm run build` emits the SPA straight into the API's `wwwroot`,
  so IIS hosts one site / one app pool.

### Source layout

```
FreshServiceArchives/
├─ FreshServiceArchives.slnx
├─ build-and-publish.ps1          # one-shot build (SPA + API) → .\publish
├─ db/
│  └─ recommended_indexes.sql     # optional perf indexes for the search queries
└─ src/
   ├─ FreshServiceArchives.Api/   # ASP.NET Core API + serves the SPA
   │  ├─ Controllers/             # Tickets, Lookups, Me
   │  ├─ Data/                    # Dapper repo, connection factory, code lookups
   │  ├─ Models/                  # DTOs
   │  ├─ Program.cs               # auth + pipeline wiring
   │  └─ appsettings.json         # ⇦ set the connection string here
   └─ freshservice-archives-web/  # React + Vite SPA
```

---

## Data source

The archiver console app maintains these tables (the portal reads them as-is):

| Table | Used for |
| --- | --- |
| `FS_Tickets_Archive` | ticket list + detail |
| `FS_Ticket_Conversations_Archive` | conversation thread |
| `FS_Ticket_Attachments_Archive` | attachment list + download (`LocalFilePath`) |
| `FS_Requesters_Archive` | requester name/email |
| `FS_Responders_Archive` | agent name/email |
| `FS_Groups_Archive` | group name + filter dropdown |

Freshservice numeric codes (status/priority/source) are mapped to text in
`Data/FreshserviceLookups.cs`.

---

## Configure

Edit `src/FreshServiceArchives.Api/appsettings.json` and set the connection string:

```json
"ConnectionStrings": {
  "ArchiveDb": "Server=SQLHOST\\INSTANCE;Database=FreshserviceArchive;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;"
}
```

`Trusted_Connection=True` means SQL is accessed as the **IIS App Pool identity** — no
password is stored. Grant that identity `db_datareader` on the archive database (see below).

---

## Run locally (development)

Two terminals:

```bash
# 1) API (Kestrel + Negotiate) on http://localhost:5028
cd src/FreshServiceArchives.Api
dotnet run
```

```bash
# 2) SPA dev server on http://localhost:5173, proxies /api → :5028
cd src/freshservice-archives-web
npm install
npm run dev
```

Open http://localhost:5173. Your Windows credentials are sent automatically by the browser.

> Optional: browse the API directly at http://localhost:5028/swagger (Development only).

---

## Build for deployment

```powershell
pwsh ./build-and-publish.ps1 -Output D:\inetpub\FreshserviceArchive
```

This builds the SPA into `wwwroot`, then `dotnet publish` produces a deployable folder
(including the auto-generated `web.config` for the ASP.NET Core Module).

---

## Deploy to IIS

**Prerequisites on the server**
- [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) (ASP.NET Core Module v2).
- IIS role features: *Windows Authentication*.

**Steps**
1. Copy the published folder to the server (e.g. `D:\inetpub\FreshserviceArchive`).
2. In **IIS Manager**, create (or reuse) an **Application Pool**:
   - .NET CLR version: **No Managed Code**
   - Identity: a domain service account (recommended) *or* `ApplicationPoolIdentity`.
3. Create a **Website / Application** pointing at the published folder, using that pool.
4. Select the site → **Authentication**:
   - **Enable** *Windows Authentication*
   - **Disable** *Anonymous Authentication*
5. Grant the **App Pool identity** read access to SQL Server:
   ```sql
   -- Run against the archive database
   CREATE USER [DOMAIN\svc-freshservice-archive] FROM LOGIN [DOMAIN\svc-freshservice-archive];
   ALTER ROLE db_datareader ADD MEMBER [DOMAIN\svc-freshservice-archive];
   ```
   (For `ApplicationPoolIdentity`, the login is `DOMAIN\MACHINE$` — the computer account.)
6. Ensure the App Pool identity can **read the attachment storage path** referenced by
   `FS_Ticket_Attachments_Archive.LocalFilePath` (local folder or UNC share).
7. (Optional) Run `db/recommended_indexes.sql` for faster search on large archives.
8. Browse to the site — you should be signed in automatically and see the search grid.

**Kerberos note:** if the App Pool runs under a domain account and users hit the site by
host name, register an SPN so Windows Auth uses Kerberos:
```
setspn -S HTTP/archive.yourdomain.com DOMAIN\svc-freshservice-archive
```

---

## Security notes

- All `/api/*` endpoints require an authenticated Windows user (enforced by a global
  authorization fallback policy). Only the static SPA shell is anonymous.
- Attachment downloads are served **only** from the `LocalFilePath` recorded by the
  archiver, looked up by attachment id — user input never forms a file path.
- Archived HTML bodies are sanitized with DOMPurify (client) before rendering.
- The connection string uses Integrated Security, so no SQL password is stored in config.
