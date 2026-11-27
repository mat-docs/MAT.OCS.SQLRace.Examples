// ─────────────────────────────────────────────────────────────
// SQL Race Example: Transient Events
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Adding and reading an in-memory Transient Event
// Prerequisites: SQL Race API with Transient Events support, .NET 8, an .ssn2 file
// Input: Path to an .ssn2 file (or edit DefaultSessionPath below)
// Output: Event visible in the active session, then absent after reload
// Note: A new event definition is added to the session before the transient event.
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Events;
using MESL.SqlRace.Domain.Query;
using MESL.SqlRace.Enumerators;
using System;

const string DefaultSessionPath = @"C:\path\to\session.ssn2";

Console.WriteLine($"Initialising...");
Core.LicenceProgramName = "SQLRace";
Core.Initialize();

// --- 1. Select the .ssn2 file (pass a path or edit DefaultSessionPath) ---
var filePath = args.Length > 0 ? args[0] : DefaultSessionPath;
if (!File.Exists(filePath))
{
    Console.WriteLine($"Session file not found: {filePath}");
    Console.WriteLine("Pass the path to an .ssn2 file: dotnet run -- <path-to-ssn2-file>");
    return;
}

var connectionString = $"DbEngine=SQLite;Data Source={filePath};";
var sessionManager = SessionManager.CreateSessionManager();
using var queryManager = QueryManager.CreateQueryManager(connectionString);

// --- 2. Find the most recent session in the file ---
var sessionSummary = queryManager.ExecuteQuery()
    .OrderByDescending(summary => summary.TimeOfRecording)
    .FirstOrDefault();

if (sessionSummary is null)
{
    Console.WriteLine($"No sessions found in {filePath}");
    return;
}

int eventDefinitionId;
long timestamp;
double[] rawData;

// --- 3. Load the session, add a transient event, and read it back ---
Console.WriteLine($"Loading...");
using (var clientSession = sessionManager.Load(sessionSummary.Key, connectionString))
{
    var session = clientSession.Session;
    var eventService = EventsFactory.CreateSessionEventService();
    var definition = eventService.AddEventDefinition(
        session.Key,
        "Transient Event Example",
        EventPriorityType.High,
        "Transient Events",
        shouldPersist: true);
    eventDefinitionId = definition.EventDefinitionId;
    timestamp = session.EndTime;
    rawData = [12345.6789];

    var transientEventService = SessionTransientEventServiceProvider.SessionTransientEventService();
    transientEventService.AddTransientEvent(session.Key, eventDefinitionId, timestamp, rawData);

    var addedEvent = session.Events.GetEventData(timestamp, timestamp)
        .Single(item =>
            item.IsTransient &&
            item.EventDefinitionKey == eventDefinitionId &&
            item.RawData.SequenceEqual(rawData));
    Console.WriteLine(
        $"Before reload: transient = {addedEvent.IsTransient}, timestamp = {addedEvent.TimeStamp}, " +
        $"raw data = [{string.Join(", ", addedEvent.RawData)}]");

    // Flush explicitly: transient events should still not be persisted.
    Console.WriteLine($"Closing...");
    session.Flush();
}

// --- 4. Reload and verify the transient event was not persisted ---
Console.WriteLine($"Loading...");
using (var reloaded = sessionManager.Load(sessionSummary.Key, connectionString))
{
    var remainingEvents = reloaded.Session.Events.GetEventData(timestamp, timestamp)
        .Where(item =>
            item.EventDefinitionKey == eventDefinitionId &&
            item.RawData.SequenceEqual(rawData))
        .ToList();

    Console.WriteLine(remainingEvents.Count == 0
        ? "After reload: the transient event was not found (it was not persisted)."
        : $"After reload: found {remainingEvents.Count} matching event(s).");
}
