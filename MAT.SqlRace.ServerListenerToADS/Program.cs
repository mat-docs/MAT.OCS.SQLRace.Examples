// <copyright file="Program.cs" company="McLaren Applied Ltd.">
// Copyright (c) McLaren Applied Ltd.</copyright>

using System.Net;

using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;
using System.Net.Sockets;
using MESL.SqlRace.Domain.Query;


namespace MAT.SqlRace.ServerListenerLive
{
    /// <summary>
    /// This class defines a console application that interacts with the SQLRace system to retrieve live telemetry data. 
    /// The program establishes a connection to a SQL Server database, configures a server listener, and identifies live sessions. 
    /// It then retrieves data for each session, specifically extracting and printing information related to laps. 
    /// An example of a use case for this scenarion can be found in #85881 - Missing Out Lap at start of Lap 1 using SQLRace.
    /// </summary>
    internal class Program
    {
        // The server listener configuration
        #region constants
        public static int ServerListenerPortNumber = 6565;
        public const string ServerListenerIpAddress = "127.0.0.1";
        public static string DataSource = "MCLA-2DSBLR3";
        public const string DbName = "MK_SQLRACE02_on_C";           // MK_SQLRACE02_on_C        MK_SQLRACE01_Local
        public const int TimerInterval = 20000;                     // 20 sec.
        public static string recorderDbEngine = "SQLServer";
        public static string ConnectionString;
        #endregion constants

        public static void Main(string[] args)
        {
            if (!IsPortInUse(ServerListenerPortNumber))
            {
                Console.WriteLine($"Could not establish tcp connection on Port {ServerListenerPortNumber}.");
                return;
            }

            //SQLServer style connection string
            ConnectionString = @"DbEngine=SQLServer;Data Source={DataSource};Initial Catalog={DbName};Integrated Security=True";

#pragma warning disable CA1416 // Validate platform compatibility

            Console.WriteLine("Initialising SQL Race...");
            Core.Initialize();

            Console.WriteLine("Setting up Server Listener Instance\r\n");

            Core.ConfigureServer(true, new IPEndPoint(IPAddress.Parse(ServerListenerIpAddress), ServerListenerPortNumber));

            var connectionString = GetSqlRaceConnectionString(DbName);

            Console.WriteLine($"connectionString: {connectionString}");
            var ss = GetMostRecentLiveSession(connectionString);

            if (ss == null)
            {
                Console.WriteLine("No live session found");
                return;
            }

            var sessionManager = SessionManager.CreateSessionManager();

            var recordersConfiguration = RecordersConfiguration.GetRecordersConfiguration();

            recordersConfiguration.AddConfiguration(
                Guid.NewGuid(),
                recorderDbEngine,
                DataSource,
                DataSource,
                connectionString,
                false);

            while (ss.State == SessionState.Live)
            {
                connectionString = GetSqlRaceConnectionString(DbName);

                ss = GetMostRecentLiveSession(connectionString);

                if (ss == null)
                {
                    Console.WriteLine("No live session found");
                    return;
                }

                Console.WriteLine($"Session Identifier: {ss.Identifier}");
                Console.WriteLine($"Time: {DateTime.Now.ToLocalTime()}, Laps count: {ss.Laps.Count}");

                foreach (var lap in ss.Laps.OrderBy(x => x.LapId))
                {
                    Console.WriteLine($"LapId: {lap.LapId}, Number: {lap.Number}, lap Name: {lap.Name}, StartTime: {lap.StartTime}, EndTime: {lap.EndTime}, LapTime: {lap.LapTime}");

                    if (lap.TimeRange.HasValue)
                    {
                        Console.WriteLine($"\t\t\tTimeRange/TimeSpan: {lap.TimeRange.Value.Span.ToTimeSpan()}, TriggerSource: {lap.TriggerSource}, CountForFastestLap: {lap.CountForFastestLap}");
                    }
                    Console.WriteLine("");
                }

                Thread.Sleep(TimerInterval);
            }
#pragma warning restore CA1416 // Validate platform compatibility

            Console.WriteLine("Press ENTER key to close.");
            Console.ReadLine();
        }

        static bool IsPortInUse(int port)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect("localhost", port);
                    return true;
                }
                catch (SocketException ex)
                {
                    return false;
                }
            }
        }

#pragma warning disable CA1416 // Validate platform compatibility
        private static string GetSqlRaceConnectionString(string connectionFriendlyName)
        {
            var connectionManager = new DatabaseConnectionManager();
            var databaseConnection = connectionManager.GetDatabaseConnections()
                .FirstOrDefault(c => c.FriendlyName == connectionFriendlyName);
            var connectionString = databaseConnection?.GetConnectionString();

            return connectionString;
        }

        private static SessionSummary GetMostRecentLiveSession(string connectionString)
        {
            using (var qm = QueryManager.CreateQueryManager(connectionString))
            {
                var sessionSummaries = qm.ExecuteQuery()
                                            .Where(x => x.State == SessionState.Live) 
                                            .OrderByDescending(x => x.TimeOfRecording)
                                            .ToList();

                var summary = sessionSummaries.FirstOrDefault();
                if (summary != null)
                {
                    return summary;
                }
            }

            return null;
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }
}
