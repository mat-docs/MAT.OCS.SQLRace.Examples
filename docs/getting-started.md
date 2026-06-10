# Getting Started

This guide walks you through setting up your development environment to run the SQL Race API examples.

## Prerequisites

### .NET 8 SDK

Download and install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

Verify your installation:

```bash
dotnet --version
# Should output 8.0.x
```

### NuGet Package Source

The SQL Race API is distributed via GitHub Packages. Add the feed to your NuGet configuration:

```bash
dotnet nuget add source https://nuget.pkg.github.com/mat-docs/index.json \
  --name mat-docs \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT
```

You need a GitHub Personal Access Token (PAT) with `read:packages` scope. [Create one here](https://github.com/settings/tokens/new?scopes=read:packages).

Alternatively, add the source in your `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="mat-docs" value="https://nuget.pkg.github.com/mat-docs/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <mat-docs>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </mat-docs>
  </packageSourceCredentials>
</configuration>
```

### ATLAS (Optional)

ATLAS is the desktop application for viewing and analysing SQL Race sessions. It is required only for:

- Live data scenarios (server listener, live dashboard)
- Viewing `.ssn2` files with full visualisation
- Server-connected database sessions

For getting-started and offline examples, ATLAS is **not required**.

## First Run

### Option 1: Run a snippet directly

```bash
# Create a new console project
dotnet new console -n QuickStart
cd QuickStart

# Add the SQL Race package
dotnet add package MESL.SQLRace.API

# Copy a snippet into Program.cs
cp ../snippets/csharp/getting-started/04-create-session-write-data.cs Program.cs

# Run it
dotnet run
```

This will create a local SQLite session file, write sample data, and read it back — no server or configuration needed.

### Option 2: Run the GettingStarted project

```bash
cd projects/csharp
dotnet run --project GettingStarted
```

This uses the shared Core library for a cleaner API experience.

## Project Structure for Your Own Code

When building your own application with SQL Race, a typical project structure looks like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MESL.SQLRace.API" Version="*" />
  </ItemGroup>
</Project>
```

## Initialisation Pattern

Every application using SQL Race must initialise the runtime before any other API calls:

```csharp
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();
```

This is a one-time call. The [Core library](../projects/csharp/SqlRace.Examples.Core/) provides `SqlRaceBootstrapper` to handle this safely with idempotency.

## Next Steps

- [Connection Strings](connection-strings.md) — learn about SQLite, SQL Server, and configuration options
- [Snippets](../snippets/) — browse single-file examples by topic
- [COOKBOOK.md](../COOKBOOK.md) — find examples by task
- [CONCEPTS.md](../CONCEPTS.md) — map SQL Race concepts to your industry's terminology
