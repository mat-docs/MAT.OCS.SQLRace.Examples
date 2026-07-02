using MAT.OCS.Core;
using MESL.SqlRace.Domain;

namespace SqlRace.Examples.Core;

/// <summary>
/// Factory methods for creating and loading SQL Race sessions with
/// proper error handling and connection string resolution.
/// </summary>
public static class SessionFactory
{
    /// <summary>
    /// Creates a new session in the database specified by the connection string.
    /// </summary>
    /// <param name="name">A descriptive name for the session.</param>
    /// <param name="connectionString">
    /// Connection string. If null, resolved via <see cref="ConnectionStringProvider"/>.
    /// </param>
    /// <param name="dateTime">Session date/time. Defaults to <see cref="DateTime.UtcNow"/>.</param>
    /// <returns>An <see cref="IClientSession"/> that must be disposed when finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown if session creation fails.</exception>
    public static IClientSession CreateSession(
        string name,
        string? connectionString = null,
        DateTime? dateTime = null)
    {
        SqlRaceBootstrapper.Initialize();

        var connStr = ConnectionStringProvider.Resolve(connectionString);
        var sessionKey = SessionKey.NewKey();
        var sessionDate = dateTime ?? DateTime.UtcNow;

        try
        {
            var sessionManager = SessionManager.CreateSessionManager();
            var clientSession = sessionManager.CreateSession(
                connStr,
                sessionKey,
                name,
                sessionDate,
                "Example");

            return clientSession ?? throw new InvalidOperationException(
                $"SessionManager.CreateSession returned null. " +
                $"Connection string: {connStr}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to create session '{name}'. " +
                $"Connection string: {connStr}. " +
                $"Error: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Loads an existing session from a database by session key.
    /// </summary>
    /// <param name="sessionKey">The GUID key of the session to load.</param>
    /// <param name="connectionString">
    /// Connection string. If null, resolved via <see cref="ConnectionStringProvider"/>.
    /// </param>
    /// <returns>An <see cref="IClientSession"/> that must be disposed when finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the session cannot be found or loaded.</exception>
    public static IClientSession LoadSession(
        SessionKey sessionKey,
        string? connectionString = null)
    {
        SqlRaceBootstrapper.Initialize();

        var connStr = ConnectionStringProvider.Resolve(connectionString);

        try
        {
            var sessionManager = SessionManager.CreateSessionManager();
            var clientSession = sessionManager.Load(sessionKey, connStr);

            return clientSession ?? throw new InvalidOperationException(
                $"Session not found: {sessionKey}. " +
                $"Check that the session exists in the database at: {connStr}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to load session {sessionKey}. " +
                $"Connection string: {connStr}. " +
                $"Error: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Loads a session from a local .ssn or .ssn2 file.
    /// </summary>
    /// <param name="filePath">Path to the session file.</param>
    /// <returns>An <see cref="IClientSession"/> that must be disposed when finished.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the file cannot be loaded.</exception>
    public static IClientSession LoadFromFile(string filePath)
    {
        SqlRaceBootstrapper.Initialize();

        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                $"Session file not found: {filePath}. " +
                $"Check the path and ensure the file exists.",
                filePath);

        try
        {
            var fileSessionManager = FileSessionManager.CreateFileSessionManager();
            var clientSession = fileSessionManager.Load(filePath);

            return clientSession ?? throw new InvalidOperationException(
                $"FileSessionManager.Load returned null for: {filePath}. " +
                $"The file may be corrupt or in an unsupported format.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not FileNotFoundException)
        {
            throw new InvalidOperationException(
                $"Failed to load session file: {filePath}. " +
                $"Error: {ex.Message}",
                ex);
        }
    }
}
