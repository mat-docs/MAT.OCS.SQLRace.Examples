using MESL.SqlRace.Domain;

namespace SqlRace.Examples.Core;

/// <summary>
/// Provides idempotent initialisation of the SQL Race runtime.
/// Call <see cref="Initialize"/> once at application startup before any other SQL Race API calls.
/// </summary>
public static class SqlRaceBootstrapper
{
    private static readonly object Lock = new();
    private static bool _initialized;

    /// <summary>
    /// Initialises the SQL Race runtime. Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    /// <param name="licenceProgramName">
    /// The licence program name. Defaults to <c>"SQLRace"</c>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if SQL Race initialisation fails.
    /// </exception>
    public static void Initialize(string licenceProgramName = "SQLRace")
    {
        if (_initialized)
            return;

        lock (Lock)
        {
            if (_initialized)
                return;

            try
            {
                MESL.SqlRace.Domain.Core.LicenceProgramName = licenceProgramName;
                MESL.SqlRace.Domain.Core.Initialize();
                _initialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to initialise SQL Race runtime. " +
                    $"Ensure the MESL.SQLRace.API NuGet package is installed and its native dependencies are present. " +
                    $"Inner error: {ex.Message}",
                    ex);
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> if <see cref="Initialize"/> has completed successfully.
    /// </summary>
    public static bool IsInitialized => _initialized;
}
