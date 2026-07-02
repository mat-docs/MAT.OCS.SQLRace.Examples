# Connection Strings

SQL Race supports two database backends: **SQLite** (local files) and **SQL Server** (network databases). This guide covers the connection string format, configuration options, and best practices.

## SQLite (Local File)

SQLite connections use a local `.ssn2` file. No server required.

```
DbEngine=SQLite;Data Source=C:\data\my-session.ssn2;
```

### Default for examples

All snippets in this repository default to a temporary file that works without any configuration:

```csharp
var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
```

This creates `sqlrace-examples.ssn2` in your system's temp directory (e.g., `%TEMP%` on Windows, `/tmp` on Linux/macOS).

### Cross-platform paths

```csharp
// Always use Path.Combine for cross-platform compatibility
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyApp", "data.ssn2");
var connectionString = $"DbEngine=SQLite;Data Source={dbPath};";
```

## SQL Server

SQL Server connections require a running SQL Server instance with the SQL Race schema installed.

```
Server=myserver\SQLEXPRESS;Database=SQLRACE01;Trusted_Security=True;
```

### Common variations

```
# Windows authentication
Server=myserver;Database=SQLRACE01;Trusted_Security=True;

# SQL authentication
Server=myserver;Database=SQLRACE01;User Id=myuser;Password=mypassword;

# Named instance
Server=myserver\INSTANCENAME;Database=SQLRACE01;Trusted_Security=True;

# Custom port
Server=myserver,1434;Database=SQLRACE01;Trusted_Security=True;
```

## Configuration Hierarchy

The Core library's `ConnectionStringProvider` resolves connection strings in this order:

1. **Explicit parameter** — passed directly to the method
2. **Environment variable** — `SQLRACE_CONNECTION_STRING`
3. **appsettings.json** — under the key `SqlRace:ConnectionString`
4. **Default** — SQLite file in the temp directory

### Environment variable

```bash
# Set once for your shell session
export SQLRACE_CONNECTION_STRING="Server=myserver;Database=SQLRACE01;Trusted_Security=True;"
```

```powershell
# PowerShell
$env:SQLRACE_CONNECTION_STRING = "Server=myserver;Database=SQLRACE01;Trusted_Security=True;"
```

### appsettings.json

Create an `appsettings.json` in your project root:

```json
{
  "SqlRace": {
    "ConnectionString": "DbEngine=SQLite;Data Source=C:\\data\\my-session.ssn2;"
  }
}
```

Make sure it's copied to the output directory:

```xml
<ItemGroup>
  <None Include="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## Tips

- **Start with SQLite.** It requires no installation, no configuration, and works everywhere. Move to SQL Server when you need multi-user access or live data.
- **Never hard-code server names.** Use environment variables or `appsettings.json` so your code works across environments.
- **Use the Core library's `ConnectionStringProvider`** in projects to get the configuration hierarchy for free.
- **File sessions (`.ssn2`) are single-writer.** Only one process can write to a file session at a time. Use SQL Server for concurrent access.
