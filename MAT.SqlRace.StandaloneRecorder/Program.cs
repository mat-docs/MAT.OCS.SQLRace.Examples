using MAT.OCS.ATLAS.Recording;
using MAT.OCS.Core;
using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.Enumerators;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MAT.SqlRace.StandaloneRecorder
{
    /// <summary>
    /// Demo features:
    ///     1- Record live data to a local SqlRace (Sqlite)
    ///     2- Read live vCar samples
    ///     3- Create custom parameter and write and read data to live session
    /// 
    /// NOTE: Make sure Server Listener in ADS is enabled under Tools -> SqlRace -> Settings...
    /// </summary>
    internal class Program
    {
        // The server listener configuration (leave those defaults in case not needed)
        private const int ServerListenerPortNumber = 6565;
        private const string ServerListenerIpAddress = "127.0.0.1";

        // The ADS Host:Name
        private const string RecorderDataServer = "M801338:Default";

        private static string sqlConnectionString;

        private static DataServerTelemetryRecorder recorder;
        private static RecorderState recordingState;

        public static void Main(string[] args)
        {
            try
            {
                var dbEngine = "SQLite";
                var dataSource = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                                 @"\McLaren Applied Technologies\ATLAS 10\SQL Race\LiveSessionCache.ssn2";
                sqlConnectionString = $@"DbEngine={dbEngine};Data Source={dataSource};";

                Core.Initialize();
                var sessionManager = SessionManager.CreateSessionManager();
                sessionManager.SessionEventOccurred += SessionManager_SessionEventOccurred;

                Console.WriteLine("Initialising");

                recordingState = RecorderState.Idle;

                Recorders.Initialise(ServerListenerIpAddress, ServerListenerPortNumber);

                Console.WriteLine("Setting up Recorder Instance");

                recorder = Recorders.CreateDataServerTelemetryRecorder();
                recorder.SetSessionIdentifier("%y%m%d%H%M%S");
                recorder.OnStatusChanged += recorder_OnStatusChanged;
                recorder.SetSQLRaceConnection(Guid.NewGuid(), dbEngine, dataSource, sqlConnectionString, sqlConnectionString, false);

                Console.WriteLine("Getting Available Server List");
                recorder.RefreshServerList();
                var availableServers = recorder.GetServerList();

                foreach (var svr in availableServers)
                {
                    Console.WriteLine("ADS Found : " + svr);
                }

                if (availableServers.Count > 0)
                {
                    Console.WriteLine($"Setting ADS Connection to {RecorderDataServer}");
                    recorder.SetDataServer(RecorderDataServer);

                    MonitorRecorder();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("");
                Console.WriteLine("ERROR:");
                Console.WriteLine(e);
                Console.WriteLine("ERROR");
                Console.WriteLine("");
            }
            Console.WriteLine("Test Complete");
            Console.ReadLine();
        }

        /// <summary>
        /// Pulls data out from the live session.
        /// </summary>
        /// <param name="sessionKey">The session key.</param>
        private static void StartGetData(SessionKey sessionKey)
        {
            Task.Run(
                () =>
                {
                    var sessionManager = SessionManager.CreateSessionManager();

                    var sessionSummary = sessionManager.FindSummaryBy(sessionKey, sqlConnectionString);
                    using (var clientSession = sessionManager.Load(sessionSummary.Key, sessionSummary.GetConnectionString()))
                    {
                        var session = clientSession.Session;

                        var mrs = new ManualResetEvent(false);
                        session.RdaParametersChanged += (sender, args) =>
                        {
                            Console.WriteLine($"Parameter number is {session.Parameters.Count}");
                            if (session.ContainsParameter("vCar:Chassis"))
                            {
                                mrs.Set();
                            }
                        };

                        // wait for the configuration to be processed
                        mrs.WaitOne();

                        var parameter = SessionHelper.CreateSessionConfigurationForOneParameter(session);

                        // pulls vCar data out every 2 seconds
                        try
                        {
                            using (var pda = session.CreateParameterDataAccess("vCar:Chassis"))
                            {
                                using (var pdaNewParameter = session.CreateParameterDataAccess(parameter.Identifier))
                                {
                                    while (true)
                                    {
                                        var samples = ReadData(session, pda);
                                        WriteData(session, parameter, pdaNewParameter, samples);

                                        Thread.Sleep(2000);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error occurred" + ex.Message);
                        }
                    }
                });
        }

        private static void WriteData(Session session, Parameter parameter, ParameterDataAccessBase pdaNewParameter, ParameterValues samples)
        {
            // add data to new created parameter
            for (var j = samples.SampleCount - 1; j >= 0; j--)
            {
                var newTimestamp = samples.Timestamp[j];
                var newValue = samples.Data[j] + 10;

                session.AddChannelData(
                    parameter.ChannelIds.FirstOrDefault(),
                    newTimestamp,
                    1, //value of 1 param
                    BitConverter.GetBytes(newValue));

                Console.WriteLine($"Written sample. Timestamp: {newTimestamp.ToTimeString()} Value:{newValue}");
            }

            // read data back to verify the data added
            pdaNewParameter.GoTo(session.EndTime);
            var samplesNewParameter = pdaNewParameter.GetNextSamples(10, StepDirection.Reverse);
            for (var j = 0; j < samplesNewParameter.SampleCount; j++)
            {
                Console.WriteLine($"{parameter.Identifier} {samplesNewParameter.Timestamp[j].ToTimeString()} {samplesNewParameter.Data[j]}");
            }
        }

        private static ParameterValues ReadData(Session session, ParameterDataAccessBase pda)
        {
            // read vCar data
            pda.GoTo(session.EndTime);
            var samples = pda.GetNextSamples(10, StepDirection.Reverse);

            Console.WriteLine("** Samples **");
            for (var j = 0; j < samples.SampleCount; j++)
            {
                Console.WriteLine($"vCar:Chassis {samples.Timestamp[j].ToTimeString()} {samples.Data[j]}");
            }

            return samples;
        }

        private static void recorder_OnStatusChanged(RecorderState newState)
        {
            Console.WriteLine("   => New Recorder Status : " + newState.ToString());
            recordingState = newState;
        }

        private static void SessionManager_SessionEventOccurred(object sender, SessionEventArgs e)
        {
            if (e.EventType == SessionEventType.SessionCreated)
            {
                StartGetData(e.SessionKey);
            }
        }

        private static void MonitorRecorder()
        {
            Console.WriteLine("");
            Console.WriteLine("Starting Recorder Test");

            Console.WriteLine("Auto Record Enabled");
            recorder.SetAutoRecordEnabled(true);
            recordingState = RecorderState.AutoRecordIdle;

            while (recordingState != RecorderState.Idle)
            {
                Thread.Sleep(30000);
                Console.WriteLine("      Recorder Tick...");
            }

            Console.WriteLine("Recording Complete");

            Console.WriteLine("Auto Record Disabled");
            recorder.SetAutoRecordEnabled(false);
            Thread.Sleep(5000);

            recorder.StopRecording();
            Thread.Sleep(25000);
        }
    }
}
