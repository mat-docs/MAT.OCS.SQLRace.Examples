# Server Listener Setup (Live Data)

The Server Listener is the mechanism that enables **live data streaming** from a recording session to remote SQL Race clients. Without it, sessions appear as static snapshots — you can only see data after the recording is complete.

This guide explains how to configure the Server Listener so that the live-data snippets and the LiveDashboard project work against actively recording sessions.

## How It Works

When a recording starts on the ADS (ATLAS Data Server), the Server Listener broadcasts session updates over the network. Remote clients connect to the ADS and receive data as it is recorded.

```
┌─────────────────┐         TCP/UDP          ┌──────────────────┐
│   ADS Machine    │ ─────────────────────►  │  ATLAS Client /   │
│  (recording to   │   Server Listener port   │  SQL Race API app │
│   SQL Server DB) │                          │  (reads live data)│
└────────┬─────────┘                          └──────────────────┘
         │
         │  writes to
         ▼
┌─────────────────┐
│  SQL Server DB   │
│  (SQLRACE01)     │
└─────────────────┘
```

Key points:

- The ADS stores session metadata (including the Server Listener IP/port) in the database alongside the session data.
- The **client's** Server Listener settings do not matter — only the ADS settings are used since the client is not creating a session.
- The client reads from the **same SQL Server database** that the ADS is recording to.

> **Note:** For local recording with a DST recorder, the recorder takes data from Multicast and creates a session locally — the local ATLAS 10 acts as both the Server Listener "server" and "client". The Server Listener setup described here is only needed for **remote** live data access.

## What You Need

You need **three components** (they can be on separate machines or on the same machine):

| Component | Role |
|-----------|------|
| **ADS machine** | Runs the recorder, writes data to SQL Server, hosts the Server Listener |
| **SQL Server database** | Stores session data (must be accessible from both ADS and client) |
| **Client machine** | Runs your SQL Race API application (LiveDashboard, snippets, ATLAS, etc.) |

## Step-by-Step Setup

### 1. Configure the ADS Server Listener

On the ADS machine:

1. Open ATLAS and go to **Tools → SQL Race → Settings**
2. Navigate to the **Server Listener** section
3. Set the **Database connection** to the SQL Server database you will record to (e.g., `SQLRACE01`)
4. Tick **Enable server listener**
5. Set the **IP Address** to a network address that client machines can reach (this should match the Wide Band Address)
6. Set the **Port** — ATLAS uses `7831` by default
7. Click **OK**

### 2. Configure the Recorder

Still on the ADS machine:

1. Go to **Setup** and open the recorder configuration
2. Set **Session Destination** to **SQL Race**
3. Set the **SQL Race Database** to the **same database** selected in the Server Listener settings

> **Important:** The Server Listener database and the recorder database **must be the same**. If they differ, the client will not receive live data.

### 3. Open Firewall Ports

On the ADS machine, ensure the Server Listener port (e.g., `7831`) is open for **both TCP and UDP** inbound connections.

On the client machine, ensure outbound connections to that port are allowed.

```powershell
# Example: open port 7831 on the ADS machine (run as Administrator)
New-NetFirewallRule -DisplayName "ATLAS Server Listener" -Direction Inbound -Protocol TCP -LocalPort 7831 -Action Allow
New-NetFirewallRule -DisplayName "ATLAS Server Listener UDP" -Direction Inbound -Protocol UDP -LocalPort 7831 -Action Allow
```

### 4. Configure the Client

On the client machine, add the SQL Server database (the one the ADS records to) to your indexed sources. In ATLAS, this is under **Tools → SQL Race → Settings → Connections**.

For SQL Race API applications, you just need the connection string to the same database:

```csharp
var connectionString = "Server=your-sql-server;Database=SQLRACE01;Trusted_Connection=True;";
```

Or set it via environment variable:

```powershell
$env:SQLRACE_CONNECTION_STRING = "Server=your-sql-server;Database=SQLRACE01;Trusted_Connection=True;"
```

## Starting a Live Recording

1. Start the recorder on the ADS machine
2. On the client, check the session state — it should show as **Live**, not **LiveNotInServer**
   - If it shows **LiveNotInServer**, check your Server Listener configuration (IP, port, database match, firewall)
3. Wait until the session reports at least **1 lap** before loading it in ATLAS
4. The session should behave like a live session — data updates as the ADS records

## Verifying Live Data with the Tools

Use the `FindLiveSession` tool to automatically detect active sessions:

```powershell
dotnet run --project tools/FindLiveSession
```

This queries the database, loads recent sessions, and monitors for EndTime growth (which indicates active recording). If a live session is found, it prints the session key and ready-to-use snippet commands.

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| Session shows **LiveNotInServer** | Server Listener not enabled, or IP/port mismatch | Check ADS Server Listener settings — IP must be reachable from client |
| Session loads but data doesn't update | Server Listener database ≠ recorder database | Ensure both point to the same SQL Server database |
| Client can't connect | Firewall blocking the port | Open the Server Listener port (TCP+UDP) on both machines |
| `FindLiveSession` says "no live sessions" | No active recording, or data hasn't flushed yet | Start a recorder on the ADS; use `--wait 90` for slow flush intervals |
| `CreateParameterDataAccess` throws | Session loaded before parameters were flushed | Wait for the first data flush (~60–70s), then reload the session |
| Data values don't change between polls | Cached session data from initial load | Reload the session (`SessionManager.Load()`) each poll cycle — see `poll-live-samples.cs` |

## Related

- [snippets/csharp/live-data/](../snippets/csharp/live-data/) — Live data snippet examples
- [projects/csharp/LiveDashboard/](../projects/csharp/LiveDashboard/) — Real-time dashboard project
- [projects/csharp/ServerListenerRecorder/](../projects/csharp/ServerListenerRecorder/) — Example recorder using Server Listener
- [docs/connection-strings.md](connection-strings.md) — Database connection string reference
