// <copyright file="Program.cs" company="McLaren Applied Ltd.">
// Copyright (c) McLaren Applied Ltd.</copyright>

using MAT.OCS.Core;
using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Collections;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Infrastructure.Enumerators;
using MESL.SqlRace.Domain.Query;
using MESL.SqlRace.Enumerators;
using MESL.SqlRace.UI;
using MESL.SqlRace.Domain.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Functions.Interfaces.Enums;

namespace MAT.SQLRace.HelloData
{
    internal class Program
    {
        public static string ConnectionString;
        //a337e5f0-72fe-4691-8627-e45530ff5012

        private static void Main(string[] args)
        {
            //SQLServer style connection string
            //ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";  

            //SQLite style connection string
            ConnectionString = $@"DbEngine=SQLite;Data Source={
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                }\McLaren Applied Technologies\ATLAS 10\SQL Race\LiveSessionCache.ssn2;";

            //SQLite ssn style connection string
            //ConnectionString = $@"DbEngine=SQLite;Data Source={
            //        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            //    }\McLaren Applied Technologies\ATLAS 10\SQL Race\IndexingSessionCache.ssn2;";

            Console.WriteLine("Initializing SQL Race....");

            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            //QueryByDataItem();
            //ReadParameterUnit();
            //ReadSamples();
            //ReadData();
            //ReadEvents();
            //LapStatistics();
            //ConnectionManagerTest();
            //CreateSessionAndAddData();
            //CompositeSessionTestWithAppendedSessions();

            //CompositeSessionWithvTagSessionsTest();
            //AddInMemoryParameterToSession();
            //AddParameterToExistingSession();
            //ChannelStats();
            //Benchmark();
            //LoadAssociated();

            //LoadSSNWithAssociatedMerge();
            //AddEvents();
            //LoadLiveSessionAndWaitForLapEvents();
            //LoadLiveSessionAndWaitForEvents();
            //LoadLiveSamples();
            LoadSSN();
            //GetSessionSummaryBySessionGUID();
            //LoadLiveFunction();
            //WholeSessionsCompareMode();

            Console.WriteLine("Press ENTER key to close.");
            Console.ReadLine();
        }

        private static void WholeSessionsCompareMode()
        {
            var session1Path = $@"C:\Users\{Environment.UserName}\Documents\McLaren Electronic Systems\ATLAS 9\Data\Server\190708111749.ssn";

            var session2Path = $@"C:\Users\{Environment.UserName}\Documents\McLaren Electronic Systems\ATLAS 9\Data\Server\190722121003.ssn";

            var compositeSessionPrimary = new CompositeSession(CompositeSessionKey.NewKey(), "PrimaryComposite");
            var compositeSessionSecondary = new CompositeSession(CompositeSessionKey.NewKey(), "SecondaryComposite");

            var compositeSessionContainer = new CompositeSessionContainer(CompositeSessionContainerKey.NewKey(), "CompareSampleContainer");
            compositeSessionContainer.SetSessionCompareMode(compositeSessionPrimary.Key, SessionCompareMode.WholeSession);
            compositeSessionContainer.Add(compositeSessionPrimary);
            compositeSessionContainer.Add(compositeSessionSecondary);

            var fileSessionManager = FileSessionManager.CreateFileSessionManager();

            Console.WriteLine("Loading sessions...");

            var sessionPrimary = fileSessionManager.Load(session1Path);
            var sessionSecondary = fileSessionManager.Load(session2Path);

            if (sessionPrimary == null || sessionSecondary == null)
            {
                Console.WriteLine("Unable to load session(s)!");
                return;
            }

            compositeSessionPrimary.Add(sessionPrimary);
            compositeSessionSecondary.Add(sessionSecondary);

            ICompositeContainerParameterDataAccess compositeContainerParameterDataAccess = compositeSessionContainer.CreateParameterDataAccess("vCar:Chassis");
            if (compositeContainerParameterDataAccess == null)
            {
                Console.WriteLine("CompositeContainerParameterDataAccess not found!");
                return;
            }

            const int NumberOfSamples = 10000;
            using (compositeContainerParameterDataAccess)
            {
                compositeContainerParameterDataAccess.GoTo(compositeSessionPrimary.StartTime);

                var sessionValuesKvp = compositeContainerParameterDataAccess.GetNextSamples(NumberOfSamples);
                
                if (sessionValuesKvp.Count == 0)
                {
                    Console.WriteLine("No parameter result found!");
                    return;
                }

                foreach (var kvp in sessionValuesKvp)
                {
                    var parameterValues = kvp.Value;
                    Console.WriteLine($"Samples Found for '{kvp.Key}' = {parameterValues.SampleCount}");
                }
            }
        }

        private static void LapStatistics()
        {
            var identifiers = new List<string>
            {
                "vCar:Chassis",
                //"MEngine:Controller",
                //"gLat:Chassis",
            };

            ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";

            var sessionKey = SessionKey.Parse("7C57A82B-96AE-43C6-9304-36719C3C9701");

            Console.WriteLine("Loading session....");

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                Console.WriteLine("Session loaded");
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                foreach (var identifier in identifiers)
                {
                    using (var pda = clientSession.Session.CreateParameterDataAccess(identifier))
                    {
                        foreach (var lap in session.LapCollection)
                        {
                            var statistics = pda.GetLapStatistics(lap, false, StatisticOption.Max);
                            Console.WriteLine($"{statistics.Lap.Number}: {statistics.MaximumTime}, {statistics.MaximumValue}");
                        }
                    }
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        ///     Creates a session and add data.
        /// </summary>
        private static void CreateSessionAndAddData()
        {
            ConnectionString = $@"DbEngine=SQLite;Data Source=c:\ssn2\test01.ssn2;";
            
            var sessionManager = SessionManager.CreateSessionManager();

            var clientSession = sessionManager.CreateSession(
                ConnectionString,
                SessionKey.NewKey(),
                "Add data session test",
                DateTime.Now,
                "Session");

            Console.WriteLine($"Creating session");
            var parameter = SessionHelper.CreateSessionConfigurationForOneParameter(clientSession);

            var session = clientSession.Session;

            Console.WriteLine($"Adding data");

            for (var i = 0; i < 10; i++)
            {
                var newTimestamp = 10.SecondsToNanoseconds() + i * parameter.Channels[0].Interval;
                var newValue = (short)(100 + i);

                session.AddChannelData(
                    parameter.ChannelIds.FirstOrDefault(),
                    newTimestamp,
                    1,
                    BitConverter.GetBytes(newValue));

                Console.WriteLine($"Written sample. Timestamp: {newTimestamp.ToTimeString()} Value:{newValue}");
            }

            Console.WriteLine($"Reading data");

            // read data back to check if values are correct
            using (var pda = clientSession.Session.CreateParameterDataAccess(parameter.Identifier))
            {
                var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

                for (var i = 0; i < samples.SampleCount; i++)
                {
                    Console.WriteLine(
                        $"Read Sample. Timestamp: {samples.Timestamp[i].ToTimeString()} Value:{samples.Data[i]}");
                }
            }

            // to associate this session to a parent session call this method
            //session.AssociateWithParent(/*Parent session key*/);

            clientSession.Close();
        }

        private static long[] CreateTimestamps(long startTime, int numberOfTimestamps, long interval)
        {
            var timestamps = new long[numberOfTimestamps];
            var timestamp = startTime;

            for (var i = 0; i < numberOfTimestamps; i++)
            {
                timestamps[i] = timestamp;
                timestamp += interval;
            }

            return timestamps;
        }

        private static long[] CreateTimestampsForTimerange(long startTime, long endTime, long interval)
        {
            var timestampsRequired = ((endTime - startTime) / interval) + 1;
            return CreateTimestamps(startTime, (int)timestampsRequired, interval);
        }

        /// <summary>
        ///     Loads a session with an associated session
        /// </summary>
        private static void LoadAssociated()
        {
            ConnectionString = @"Data Source=mesltgs1;Initial Catalog=SQLRACE143;Integrated Security=True";

            string parameterIdentifier = "BAeroAutoZero:MRLAero";
            SessionKey sessionKey = new SessionKey("3FA70D1D-F526-4A10-9396-589B3BE23625");

            var sessionManager = SessionManager.CreateSessionManager();
            var sessionSummaries = sessionManager.LoadAssociatesForSession(sessionKey, ConnectionString);
            var additionalSessionKey = sessionSummaries.OrderBy(ss => ss.TimeOfRecording).Last();
            IClientSession clientSession = sessionManager.Load(sessionKey, ConnectionString, new[] { additionalSessionKey.Key });

            var containsChannel = clientSession.Session.ContainsParameter(parameterIdentifier);
            Console.WriteLine(
                $"Parameter {parameterIdentifier} exists in session: {containsChannel}");

            using (var pda = clientSession.Session.CreateParameterDataAccess(parameterIdentifier))
            {
                Console.WriteLine(
                    $"PDA {parameterIdentifier} successfully created");
            }
        }

        /// <summary>
        ///     Loads a live session from the database and get last 2 samples every 5 seconds
        /// </summary>
        private static void LoadLiveSamples()
        {
            ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=SQLRACE01;Integrated Security=True";

            var qm = QueryManager.CreateQueryManager(ConnectionString);

            // Get the most recent live session from the database
            var ss = qm.ExecuteQuery()
                .OrderByDescending(x => x.TimeOfRecording)
                .FirstOrDefault(y => y.State == SessionState.Live && !y.Identifier.Contains("VTS"));
            if (ss == null)
            {
                Console.WriteLine("No live session found");
                return;
            }

            Console.WriteLine($"Loading session {ss.Identifier}");

            var clientSession = LoadSession(ss.Key, ss.GetConnectionString());
            clientSession.Session.LoadConfiguration();
            var session = clientSession.Session;

            for (var i = 0; i < 100; i++)
            {
                using (var pda = clientSession.Session.CreateParameterDataAccess("vCar:Chassis"))
                {
                    pda.GoTo(session.EndTime);
                    var samples = pda.GetNextSamples(10, StepDirection.Reverse);

                    Console.WriteLine("** Samples **");
                    for (var j = 0; j < samples.SampleCount; j++)
                    {
                        Console.WriteLine($"{samples.Timestamp[j].ToTimeString()} {samples.Data[j]}");
                    }
                }

                Thread.Sleep(5000);
            }

            Console.WriteLine();
        }

        /// <summary>
        ///     Loads a live session from the database and wait for lap events to come
        /// </summary>
        private static async void LoadLiveSessionAndWaitForLapEvents()
        {
            ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";

            var qm = QueryManager.CreateQueryManager(ConnectionString);

            // Get the most recent live session from the database
            var ss = qm.ExecuteQuery()
                .OrderByDescending(x => x.TimeOfRecording)
                .FirstOrDefault(y => y.State == SessionState.Live && !y.Identifier.Contains("VTS"));
            if (ss == null)
            {
                Console.WriteLine("No live session found");
                return;
            }

            Console.WriteLine("Loading session....");

            var clientSession = LoadSession(ss.Key, ss.GetConnectionString());
            clientSession.Session.LoadConfiguration();
            var session = clientSession.Session;

            // listen to laps added events
            session.LapStarted += Session_LapStarted;

            // wait events without blocking the main thread
            await Task.Delay(TimeSpan.FromHours(1));

            Console.WriteLine();
        }

        public static void QueryByDataItem()
        {
            using (var queryManager = QueryManager.CreateQueryManager(@"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True"))
            {
                var filter = new ScalarFilter("SessionStartDateTime", MatchingRule.GreaterThan, new DateTime(2017, 9, 01).ToShortDateString(), true);
                queryManager.Filter = filter;
                var sessions = queryManager.ExecuteQuery();

                foreach (var session in sessions)
                {
                    Console.WriteLine($"{session.Identifier} - {session.TimeOfRecording}");
                }
            }
        }

        /// <summary>
        ///     Loads a live session from the database and wait for events to come
        /// </summary>
        private static async void LoadLiveSessionAndWaitForEvents()
        {
            ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";

            var qm = QueryManager.CreateQueryManager(ConnectionString);

            // Get the most recent live session from the database
            var ss = qm.ExecuteQuery()
                .OrderByDescending(x => x.TimeOfRecording)
                .FirstOrDefault(y => y.State == SessionState.Live && !y.Identifier.Contains("VTS"));
            if (ss == null)
            {
                Console.WriteLine("No live session found");
                return;
            }

            Console.WriteLine("Loading session....");

            var sessionManager = SessionManager.CreateSessionManager();
            var clientSession = sessionManager.Load(ss.Key, ss.GetConnectionString());

            // listen to laps added events
            clientSession.Session.EventDataAdded += Session_EventDataAdded;

            Console.WriteLine("Waiting for events....");
            // wait events without blocking the main thread
            await Task.Delay(TimeSpan.FromHours(1));

            clientSession.Close();
            Console.WriteLine();
        }

        private static void Session_EventDataAdded(object sender, ItemEventArgs e)
        {
            Console.WriteLine($"Event occurred for session {e.SessionKey}: {e.Item}");
        }

        private static IClientSession LoadSession(SessionKey sessionKey, string connection)
        {
            var sessionManager = SessionManager.CreateSessionManager();
            return sessionManager.Load(sessionKey, connection);
        }

        private static void AddParameterToExistingSession()
        {
            ConnectionString = $@"DbEngine=SQLite;Data Source={
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                }\McLaren Applied Technologies\ATLAS 10\SQL Race\LiveSessionCache.ssn2;";

            Console.WriteLine("Loading session....");
            // var sessionKey = SessionKey.Parse("40a4f6cf-767c-4a81-b7db-da7dcbffb034");
            var sessionKey = SessionKey.Parse("3c368c0d-2456-4f15-b3b8-b76caf2be99b");

            string identifier;

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                var parameter = SessionHelper.CreateSessionConfigurationForOneParameter(clientSession);
                identifier = parameter.Identifier;

                for (var i = 0; i < 10; i++)
                {
                    var newTimestamp = session.StartTime + i * parameter.Channels[0].Interval;
                    var newValue = (short)(100 + i);

                    session.AddChannelData(
                        parameter.ChannelIds.FirstOrDefault(),
                        newTimestamp,
                        1, //value of 1 param
                        BitConverter.GetBytes(newValue));

                    Console.WriteLine($"Written sample. Timestamp: {newTimestamp.ToTimeString()} Value:{newValue}");
                }
            }

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                // read data back to check if values are correct
                using (var pda = clientSession.Session.CreateParameterDataAccess(identifier))
                {
                    var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

                    for (var i = 0; i < samples.SampleCount; i++)
                    {
                        Console.WriteLine(
                            $"Read Sample. Timestamp: {samples.Timestamp[i].ToTimeString()} Value:{samples.Data[i]}");
                    }
                }
            }

            Console.WriteLine();
        }

        private static void AddInMemoryParameterToSession()
        {
            Console.WriteLine("Loading session....");
            var sessionKey = SessionKey.Parse("3c4a8093-43fd-4639-9485-edc360093698");

            string identifier;
            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                var transientParameter = SessionHelper.CreateTransientConfigurationForOneParameter(clientSession);
                identifier = transientParameter.Identifier;

                for (var i = 0; i < 10; i++)
                {
                    var newTimestamp = session.StartTime + i * transientParameter.Channels[0].Interval;
                    var newValue = (double)(100 + i);

                    session.AddChannelData(
                        transientParameter.ChannelIds.FirstOrDefault(),
                        newTimestamp,
                        1,  //value of 1 param
                        BitConverter.GetBytes(newValue));

                    Console.WriteLine($"Written sample. Timestamp: {newTimestamp.ToTimeString()} Value:{newValue}");
                }

                // read data back to check if values are correct
                using (var pda = clientSession.Session.CreateParameterDataAccess(transientParameter.Identifier))
                {
                    var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

                    for (var i = 0; i < samples.SampleCount; i++)
                    {
                        Console.WriteLine(
                            $"Read Sample. Timestamp: {samples.Timestamp[i].ToTimeString()} Value:{samples.Data[i]}");
                    }
                }
            }

            Console.WriteLine("Reloading session");

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();

                if (clientSession.Session.ContainsParameter(identifier))
                {
                    throw new Exception("Transient parameter should not be persisted");
                }
            }

            Console.WriteLine("Transient parameter not found. Test succeeded");

            Console.WriteLine();
        }

        private static void ChannelStats()
        {
            Console.WriteLine("Loading session....");
            var sessionKey = SessionKey.Parse("4d8b762b-5e61-4538-87ac-1af9d2383279");

            var identifiers = new List<string> { "vCar:Chassis", "gLat:Chassis", "a2msMainshaft:Chassis" };

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                var channelBasedParameters = session.Parameters.Where(a => a is ChannelBasedParameter)
                    .Cast<ChannelBasedParameter>()
                    .ToList();

                var results = new Dictionary<string, Frequency>();

                foreach (var identifier in identifiers)
                {
                    var parameter = channelBasedParameters.FirstOrDefault(a => a.Identifier == identifier);
                    if (parameter == null)
                    {
                        continue;
                    }

                    var minChannelInterval = parameter.Channels.Select(a => a.Interval).Where(a => a > 0).ToList();
                    if (!minChannelInterval.Any())
                    {
                        results.Add(identifier, new Frequency(0, FrequencyUnit.Hz));//ROW
                        continue;
                    }

                    var maxFreq = minChannelInterval.Min().ToFrequency();

                    results.Add(identifier, maxFreq);
                }

                var csv = results.Select(a => new
                {
                    Print = a.Key + "," + a.Value.Value,
                    Freq = a.Value.Value
                })
                .OrderByDescending(b => b.Freq)
                .Select(c => c.Print);

                //Print to csv file
            }

            Console.WriteLine();
        }

        private static void LoadLiveFunction()
        {
            ConnectionString = $@"DbEngine=SQLite;Data Source={
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                }\McLaren Applied Technologies\ATLAS 10\SQL Race\LiveSessionCache.ssn2;";

            var qm = QueryManager.CreateQueryManager(ConnectionString);

            // Get the most recent live session from the database
            var ss = qm.ExecuteQuery()
                .OrderByDescending(x => x.TimeOfRecording)
                .FirstOrDefault(y => y.State == SessionState.Live && !y.Identifier.Contains("VTS"));
            if (ss == null)
            {
                Console.WriteLine("No live session found");
                return;
            }

            Console.WriteLine($"Loading session {ss.Identifier}");

            var clientSession = LoadSession(ss.Key, ss.GetConnectionString());
            clientSession.Session.LoadConfiguration();
            var session = clientSession.Session;


            ParameterValues samples;
            var startTime = session.StartTime;
            var endTime = session.EndTime;


            var interval = (endTime - startTime) / 2800;
            var timeStamps = CreateTimestampsForTimerange(startTime, endTime, interval);

            using (var pda = clientSession.Session.CreateParameterDataAccess("vCar:Chassis"))
            {
                pda.GoTo(endTime);
                samples = pda.GetNextSamples(5, StepDirection.Reverse);

                Console.WriteLine("** Samples **");
                for (var j = 0; j < samples.SampleCount; j++)
                {
                    Console.WriteLine($"{samples.Timestamp[j].ToTimeString()} {samples.Data[j]}");
                }
            }




            //Create & Build Function
            var functionManager = FunctionManagerFactory.Create();
            var functionDefinition = FunctionHelper.CreateFdlFunctionDefinition(functionManager, FunctionMode.LeadingEdge);

            var buildResults = functionManager.Build(functionDefinition);

            if (buildResults.Errors.Any())
            {
                Console.WriteLine("Function build failed.");
            }

            var outputParameter = functionDefinition.OutputParameterDefinitions.First();   //created only one output parameter
            var functionPda = session.CreateParameterDataAccess(outputParameter.Identifier);

            functionPda.GoTo(endTime);
            samples = functionPda.GetNextSamples(5, StepDirection.Reverse);
            if (samples == null)
            {
                Console.WriteLine("** Failed to get function samples **");
                return;
            }

            Console.WriteLine("** Function Results **");
            for (var j = 0; j < samples.SampleCount; j++)
            {
                Console.WriteLine($"{samples.Timestamp[j].ToTimeString()} {samples.Data[j]}");
            }
        }

        private static void Benchmark()
        {
            //string conn = @"Data Source=mesltgs1;Initial Catalog=SQLRACE143;Integrated Security=True";
            //var sessionKey = new SessionKey("B06E1E02-14A4-4368-885E-CCE6C016B88D");
            string conn = $@"DbEngine=SQLite;Data Source=C:\Users\{Environment.UserName}\AppData\Local\McLaren Applied Technologies\ATLAS 10\SQL Race\LiveSessionCache.ssn2";
            var sessionKey = new SessionKey("f791aa10-a636-4bc6-b401-0d84adf02b0e");
            var sessionManager = SessionManager.CreateSessionManager();
            var clientSession = sessionManager.Load(sessionKey, conn);
            const string parameter = "nEngine:FIA"; //1Khz
            //var parameter = "xIntakeL:Controller"; //200Hz
            //var parameter = "TTyreSurfaceFLMuxed:MathMRL"; //1Khz

            var totalMilliseconds = 0D;
            var max = 0D;
            var min = double.MaxValue;
            const int minutes = 30;
            const int iterations = 50;

            var startTime = clientSession.Session.LapCollection.First(x => x.Number == 20).StartTime;
            var endTimeTime = startTime + TimeSpan.FromMinutes(minutes).ToNanoseconds();

            for (var i = 0; i < iterations; i++)
            {
                using (var pda = clientSession.Session.CreateParameterDataAccess(parameter))
                {
                    var sw = Stopwatch.StartNew();
                    var samples = pda.GetSamplesBetween(
                        startTime,
                        endTimeTime,
                        0);
                    sw.Stop();

                    Console.WriteLine($"Elapsed {sw.Elapsed.TotalMilliseconds} Sample Freq: {samples.SampleCount / (minutes * 60)}Hz");
                    totalMilliseconds += sw.Elapsed.TotalMilliseconds;
                    if (sw.Elapsed.TotalMilliseconds > max)
                    {
                        max = sw.Elapsed.TotalMilliseconds;
                    }
                    if (sw.Elapsed.TotalMilliseconds < min)
                    {
                        min = sw.Elapsed.TotalMilliseconds;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Average: {totalMilliseconds / iterations} Min: {min} Max: {max}");
        }

        /// <summary>
        /// Use connection manager to get connections
        /// </summary>
        private static void ConnectionManagerTest()
        {
            ConnectionManager cm = new ConnectionManager();
            var dbConnections = cm.GetDataBaseConnections();
            foreach (var db in dbConnections)
            {
                Console.WriteLine($"Connection {db.Identifier} {db.DatabaseName}");
            }
        }

        /// <summary>
        ///     Get a session from DB by SessionGUID
        /// </summary>
        private static void GetSessionSummaryBySessionGUID()
        {
            using (var qm = QueryManager.CreateQueryManager(ConnectionString))
            {
                qm.Filter = new ScalarFilter("SessionGUID", MatchingRule.EqualTo, "9485a6cf-442f-497d-bb08-3946c2b0ab28", true);
                var sessionSummary01 = qm.ExecuteQuery().FirstOrDefault();
                if (sessionSummary01 == null)
                {
                    Console.WriteLine("Session not found");
                    return;
                }

                Console.WriteLine($"Session found {sessionSummary01.Identifier}");
            }
        }

        /// <summary>
        /// Load data from an SSN file
        /// </summary>
        private static void LoadSSN()
        {
            var fileSessionManager = FileSessionManager.CreateFileSessionManager();

            //var session01 = fileSessionManager.Load(@"C:\Session Location\Session To Load.ssn", false);
            var session01 = fileSessionManager.Load(@"C:\Session Location\Session To Load.ssn", new List<string>
            {
                @"C:\Session Location\Session To Load.VTS.001.ssv"
            }); // session with associates

            if (session01 == null)
            {
                Console.WriteLine("Session not found");
                return;
            }

            var vCarIdentifier = "vCar:Chassis";
            using (var pda = session01.Session.CreateParameterDataAccess(vCarIdentifier))
            {
                pda.GoTo(session01.Session.StartTime + (session01.Session.EndTime - session01.Session.StartTime) / 2);
                var samples = pda.GetNextSamples(10);

                Console.WriteLine($"** Data for {vCarIdentifier}");
                for (var i = 0; i < samples.SampleCount; i++)
                {
                    Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                }
            }
        }

        private static void LoadSSNWithAssociatedMerge()
        {
            //SQLite ssn style connection string
            ConnectionString = $@"DbEngine=SQLite;Data Source={
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                }\McLaren Applied Technologies\ATLAS 10\SQL Race\IndexingSessionCache.ssn2;";

            var qm = QueryManager.CreateQueryManager(ConnectionString);
            // Get the most recent live session from the database
            var sessionSummaries = qm.ExecuteQuery().ToList();

            var sessionSummary01 =
                sessionSummaries.FirstOrDefault(
                    x => x.Identifier == "<SESSION IDENTIFIER>");
            if (sessionSummary01 == null)
            {
                Console.WriteLine("Session not found");
                return;
            }

            Console.WriteLine("Loading session...");
            var sessionManager = SessionManager.CreateSessionManager();

            var session01 = sessionManager.Load(sessionSummary01.Key, ConnectionString, new[] { sessionSummary01.Associates.Last() });
            var session = session01.Session;
            Console.WriteLine("Session loaded");

            var muTyreRLIdentifier = "muTyreRL:MRLTyres";
            using (var pda = session01.Session.CreateParameterDataAccess(muTyreRLIdentifier))
            {
                pda.GoTo(session.StartTime + (session.EndTime - session.StartTime) / 2);
                var samples = pda.GetNextSamples(10);

                Console.WriteLine($"** Data for {muTyreRLIdentifier}");
                for (var i = 0; i < samples.SampleCount; i++)
                {
                    Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                }
            }

            var vCarIdentifier = "vCar:Chassis";
            using (var pda = session01.Session.CreateParameterDataAccess(vCarIdentifier))
            {
                pda.GoTo(session.StartTime + (session.EndTime - session.StartTime) / 2);
                var samples = pda.GetNextSamples(10);

                Console.WriteLine($"** Data for {vCarIdentifier}");
                for (var i = 0; i < samples.SampleCount; i++)
                {
                    Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                }
            }
        }

        private static void CompositeSessionTestWithAppendedSessions()
        {
            ConnectionString = @"Data Source=mesltgs1;Initial Catalog=SQLRACE143;Integrated Security=True";

            var compositeSession = new CompositeSession(CompositeSessionKey.NewKey(), "CompositeSessionWithvTagSessionsTest");

            var qm = QueryManager.CreateQueryManager(ConnectionString);
            // Get the most recent live session from the database
            var sessionSummaries = qm.ExecuteQuery().ToList();

            var sessionSummary01 =
                sessionSummaries.FirstOrDefault(
                    x => x.Identifier == "<SESSION 1 IDENTIFIER>");
            if (sessionSummary01 == null)
            {
                Console.WriteLine("Session not found");
                return;
            }

            var sessionSummary02 =
                sessionSummaries.FirstOrDefault(
                    x => x.Identifier == "<SESSION 2 IDENTIFIER>");
            if (sessionSummary02 == null)
            {
                Console.WriteLine("Session not found");
                return;
            }

            var sessionManager = SessionManager.CreateSessionManager();

            Console.WriteLine("Loading sessions...");

            var session01 = sessionManager.Load(sessionSummary01.Key, ConnectionString);
            Console.WriteLine($"Session 1 loaded (start: {session01.Session.StartTime.ToTimeString()} end: {session01.Session.EndTime.ToTimeString()}");

            var session02 = sessionManager.Load(sessionSummary02.Key, ConnectionString);
            Console.WriteLine($"Session 2 loaded (start: {session02.Session.StartTime.ToTimeString()} end: {session02.Session.EndTime.ToTimeString()}");

            compositeSession.Add(session01);
            compositeSession.Add(session02);

            var parametervCar = session01.Session.GetParameter("vCar:Chassis");
            if (parametervCar == null)
            {
                Console.WriteLine("Parameter not found");
                return;
            }

            using (var pda = compositeSession.CreateParameterDataAccess(parametervCar.Identifier))
            {
                pda.GoTo(compositeSession.StartTime);
                var samples = pda.GetNextSamples(compositeSession.EndTime);

                Console.WriteLine($"** Data for {parametervCar.Identifier}");
                for (var i = 0; i < samples.SampleCount; i++)
                {
                    Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                }
            }
        }

        private static void CompositeSessionWithvTagSessionsTest()
        {
            //SQLite ssn style connection string
            ConnectionString = @"Data Source=mesltgs1;Initial Catalog=SQLRACE143;Integrated Security=True";

            var compositeSession = new CompositeSession(CompositeSessionKey.NewKey(), "CompositeSessionWithvTagSessionsTest");


            var qm = QueryManager.CreateQueryManager(ConnectionString);
            // Get the most recent live session from the database
            var sessionSummaries = qm.ExecuteQuery().ToList();

            var sessionSummary01 =
                sessionSummaries.FirstOrDefault(
                    x => x.Identifier == "<SESSION IDENTIFIER>");
            if (sessionSummary01 == null)
            {
                Console.WriteLine("Session not found");
                return;
            }

            var sessionSummary02 =
                sessionSummaries.FirstOrDefault(
                    x => x.Identifier == "<ASSOCIATED SESSION IDENTIFIER>");
            if (sessionSummary02 == null)
            {
                Console.WriteLine("Session not found");
                return;
            }

            var sessionManager = SessionManager.CreateSessionManager();

            Console.WriteLine("Loading sessions...");

            var session01 = sessionManager.Load(sessionSummary01.Key, ConnectionString);
            Console.WriteLine("Session loaded");

            var session02 = sessionManager.Load(sessionSummary02.Key, ConnectionString);
            Console.WriteLine("Associated SSV loaded");

            compositeSession.Add(session01);
            compositeSession.Add(session02);

            var parameterAero = session02.Session.GetParameter("muTyreRL:MRLTyres");
            if (parameterAero == null)
            {
                Console.WriteLine("Parameter not found");
                return;
            }

            using (var pda = compositeSession.CreateParameterDataAccess(parameterAero.Identifier))
            {
                pda.GoTo(compositeSession.StartTime + (compositeSession.EndTime - compositeSession.StartTime) / 2);
                var samples = pda.GetNextSamples(10);

                Console.WriteLine($"** Data for {parameterAero.Identifier}");
                for (var i = 0; i < samples.SampleCount; i++)
                {
                    Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                }
            }

            var parametervCar = session01.Session.GetParameter("vCar:Chassis");
            if (parametervCar == null)
            {
                Console.WriteLine("Parameter not found");
                return;
            }

            using (var pda = compositeSession.CreateParameterDataAccess(parametervCar.Identifier))
            {
                pda.GoTo(compositeSession.StartTime + (compositeSession.EndTime - compositeSession.StartTime) / 2);
                var samples = pda.GetNextSamples(10);

                Console.WriteLine($"** Data for {parametervCar.Identifier}");
                for (var i = 0; i < samples.SampleCount; i++)
                {
                    Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                }
            }
        }

        /// <summary>
        ///     Reads subsampled data from an historic session.
        /// </summary>
        private static void ReadEvents()
        {
            ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";

            var sessionKey = SessionKey.Parse("7C57A82B-96AE-43C6-9304-36719C3C9701");

            Console.WriteLine("Loading session....");

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                var eventsData = session.Events.GetEventData(session.StartTime, session.EndTime);
                foreach (var eventData in eventsData)
                {
                    var eventDefinition = session.GetEventDefinition(eventData.Key);
                    Console.WriteLine($"{eventData.TimeStamp.ToTimeString()} {eventDefinition.Description} {eventDefinition.Priority}");
                }
            }

            Console.WriteLine();
        }

        private static void AddEvents()
        {
            var sessionKey = SessionKey.Parse("06b75c89-63f9-4266-9bc3-ea9a3666b5b3");

            Console.WriteLine("Loading session....");

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                var eventsData = session.Events.GetEventData(session.StartTime, session.EndTime);
                Console.WriteLine($"Number Of Events: {eventsData.Count}");

                var sessionEventService = EventsFactory.CreateSessionEventService();
                var eventDefinition = sessionEventService.AddEventDefinition(sessionKey, "boxbox", EventPriorityType.Low, "FunctionEvents");
                Console.WriteLine($"EventDefinition Added: {eventDefinition.EventDefinitionId}, {eventDefinition.Description}");

                sessionEventService.AddEvent(sessionKey, eventDefinition.EventDefinitionId, session.StartTime + 1000000000);//1sec

                eventsData = session.Events.GetEventData(session.StartTime, session.EndTime);
                Console.WriteLine($"Number Of Events: {eventsData.Count}");
            }

            Console.WriteLine();
        }

        /// <summary>
        ///     Reads subsampled data from an historic session.
        /// </summary>
        private static void ReadData()
        {
            var identifiers = new List<string>
            {
                //"nEngine:FIA",
                "gLat:Chassis",
            };

            ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";

            var sessionKey = SessionKey.Parse("7C57A82B-96AE-43C6-9304-36719C3C9701");

            Console.WriteLine("Loading session....");

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                var startTime = session.StartTime;
                var endTime = session.EndTime;

                var interval = (endTime - startTime) / 2800;

                var timeStamps = CreateTimestampsForTimerange(startTime, endTime, interval);

                foreach (var identifier in identifiers)
                {
                    using (var pda = clientSession.Session.CreateParameterDataAccess(identifier))
                    {
                        pda.GoTo(session.StartTime);
                        var samples = pda.GetData(timeStamps);

                        Console.WriteLine($"** Data for {identifier}");
                        for (var i = 0; i < samples.SampleCount; i++)
                        {
                            Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                        }
                    }
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        ///     Reads subsampled data from an historic session.
        /// </summary>
        private static void ReadAndRewriteData()
        {
            var pdaDict = new Dictionary<string, ParameterDataAccessBase>();

            var sessionKey = SessionKey.Parse("403f2ef1-fd67-b560-2410-07606a7c99e2");
            ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";

            Console.WriteLine("Loading session....");

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                var startTime = session.StartTime;
                var endTime = session.EndTime;

                var identifiers = session.Parameters.Select(x => x.Identifier).ToList();

                // create the writing session
                var sessionManager = SessionManager.CreateSessionManager();
                var writingClientSession = sessionManager.CreateSession(
                    ConnectionString,
                    SessionKey.NewKey(),
                    "Add data session test",
                    DateTime.Now,
                    "Session");

                var channels = SessionHelper.CreateSessionConfigurationForMultipleParameter(clientSession, identifiers);

                var writingSession = writingClientSession.Session;

                var currentTime = startTime;
                long nextTime = 0;

                Console.WriteLine("Start reading session");

                do
                {
                    nextTime = currentTime + TimeSpan.FromSeconds(1).ToNanoseconds();
                    nextTime = Math.Min(nextTime, session.EndTime);

                    var interval = (nextTime - currentTime) / 1000;

                    var timeStamps = CreateTimestampsForTimerange(currentTime, nextTime, interval);

                    foreach (var identifier in identifiers)
                    {
                        try
                        {
                            ParameterDataAccessBase pda;
                            if (pdaDict.ContainsKey(identifier))
                            {
                                pda = pdaDict[identifier];
                            }
                            else
                            {
                                pda = session.CreateParameterDataAccess(identifier);
                                pdaDict.Add(identifier, pda);
                            }

                            var samples = pda.GetData(timeStamps);

                            if (samples.SampleCount > 0)
                            {
                                var data = new byte[samples.SampleCount * sizeof(double)];

                                var index = 0;
                                for (var i = 0; i < samples.SampleCount; i++)
                                {
                                    Array.Copy(BitConverter.GetBytes(samples.Data[i]), 0, data, index, sizeof(double));
                                    index += sizeof(double);
                                }

                                writingSession.AddChannelData(
                                    channels[identifier],
                                    samples.Timestamp.First(),
                                    samples.SampleCount,
                                    data);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading parameter {identifier}", ex);
                        }
                    }

                    currentTime = nextTime;

                } while (nextTime >= session.EndTime);
            }

            Console.WriteLine("Finish reading session");
        }

        /// <summary>
        ///     Reads samples from an historic session.
        /// </summary>
        private static void ReadSamples()
        {
            var identifiers = new List<string>
            {
                //"vCar:Chassis",
                //"MEngine:Controller",
                "gLat:Chassis",
            };

            ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";

            var sessionKey = SessionKey.Parse("7C57A82B-96AE-43C6-9304-36719C3C9701");

            Console.WriteLine("Loading session....");

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                Console.WriteLine("Session loaded");
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                foreach (var identifier in identifiers)
                {
                    using (var pda = clientSession.Session.CreateParameterDataAccess(identifier))
                    {
                        var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

                        Console.WriteLine($"** Samples for {identifier}");
                        for (var i = 0; i < samples.SampleCount; i++)
                        {
                            Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                        }
                    }
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        ///     Reads samples from an historic session.
        /// </summary>
        private static void ReadParameterUnit()
        {
            ConnectionString = @"Data Source=MAT-TWFIASQL02\LOCALSERVER1;Initial Catalog=SQLRACE_DEV1;Integrated Security=True";

            var sessionKey = SessionKey.Parse("7C57A82B-96AE-43C6-9304-36719C3C9701");
            //var sessionKey = SessionKey.Parse("bd0f4cda-80ac-4c80-80f3-a8b59f97a4de");

            Console.WriteLine("Loading session....");

            using (var clientSession = LoadSession(sessionKey, ConnectionString))
            {
                Console.WriteLine("Session loaded");
                clientSession.Session.LoadConfiguration();
                var session = clientSession.Session;

                var unit = string.Empty;
                var parameter = session.GetParameter("vCar:Chassis");

                // the Unit set in the parameter, has the priority
                if (string.IsNullOrEmpty(parameter.Unit))
                {
                    // if the Unit in the parameter is not set, the one in the conversion is used
                    var conversion = session.GetConversion(parameter.ConversionFunctionName);
                    if (conversion != null)
                    {
                        unit = conversion.Units;
                    }
                }
                else
                {
                    unit = parameter.Unit;
                }

                Console.WriteLine($"The unit for vCar is {unit}");
            }

            Console.WriteLine();
        }

        private static void Session_LapStarted(object sender, LapEventArgs e)
        {
            Console.WriteLine($"Lap started {e.Lap.Name}");
        }
    }
}