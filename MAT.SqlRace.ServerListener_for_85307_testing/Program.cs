// <copyright file="Program.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using System.Net;

using MAT.OCS.Core;
using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;

namespace MAT.SqlRace.ServerListenerLive
{
    /// <summary>
    /// Record live data to a local SqlRace (Sqlite)
    ///
    /// NOTE: 
    ///     If the session is in the state LiveNotInServer, make sure Server Listener port in ATLAS is different to the one specified here. 
    ///     Additionally, ensure UDP and TCP packets are allowed through the firewall settings on the Server Listener Port configured. 
    /// </summary>
    internal class Program
    {
        // The server listener configuration
        private const int ServerListenerPortNumber = 6566;
        private const string ServerListenerIpAddress = "127.0.0.1";

        public static void Main(string[] args)
        {
            var dataSource = @"C:\temp\test\livesession_85307.ssndb";
            Console.WriteLine(dataSource);

            /// connection strings are case and whitespace sensitive, the following format must be strictly followed for the Server Listener Protocol to successfully establish.
            /// SQLite: "DbEngine=SQLite;Data Source={dataSource};Pooling=false;"
            /// SQLServer: "server={dataSource};Initial Catalog={database};Trusted_Connection=True;"
            var connectionString = $@"DbEngine=SQLite;Data Source={dataSource};Pooling=false;";
            string recorderDbEngine = "SQLite"; // SQLite or SQLServer
            var sessionIdentifier = "ServerListener Live 85307 test";

            Console.WriteLine("Initialising");
            Console.WriteLine(Directory.GetCurrentDirectory());
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            Console.WriteLine("Setting up Server Listener Instance");
            Core.ConfigureServer(true, new IPEndPoint(IPAddress.Parse(ServerListenerIpAddress), ServerListenerPortNumber));
            var recordersConfiguration = RecordersConfiguration.GetRecordersConfiguration();
            recordersConfiguration.AddConfiguration(Guid.NewGuid(), recorderDbEngine, dataSource, dataSource, connectionString, false);

            // Creating a Session
            Console.WriteLine("Creating new Session");

            var dateTimeNow = DateTime.Now;
            var endDateTime = dateTimeNow.AddMinutes(5);

            var timeToday = dateTimeNow - DateTime.Today;
            var endTimeToday = endDateTime - DateTime.Today;

            long startTime = timeToday.ToNanoseconds();
            long endTime = endTimeToday.ToNanoseconds();

            string sessionDescription = string.Format("Example::: {0}", dateTimeNow.ToString("dd-MMM-yy hh:mm:ss tt"));

            var sessionKey = SessionKey.NewKey();

            // Create a session first
            var clientSession = CreateSession(sessionKey, connectionString, sessionIdentifier, dateTimeNow, "Session");

            var session = clientSession.Session;

            // Add some session details which allows values as String, Long, Double, Bool, Datetime, Byte[] etc.
            session.Items.Add(new SessionDataItem("Driver", "Test Driver"));
            session.Items.Add(new SessionDataItem("Car", "Test Car"));

            // Setting up the components of a session
            // Adding a channel with some samples
            var numSamples = 1000;

            try
            {
                clientSession = CreateParameter(clientSession, 1000, startTime, endTime);

                // Add Laps
                var lapNumber = 5;
                var lapTimeDelta = (endTime - startTime) / (lapNumber + 1);
                var timeStamp = startTime;

                for (var i = 0; i < 5; i++)
                {
                    if (i > 0)
                    {
                        timeStamp += lapTimeDelta;
                    }

                    var newLap = new Lap(timeStamp, Convert.ToInt16(i + 1), byte.MinValue, string.Format("Lap {0}", i + 1), true);

                    clientSession.Session.LapCollection.Add(newLap);

                    Console.WriteLine($"New Lap: Id = {newLap.LapId}, Name = {newLap.Name}, StartTime = {newLap.StartTime},  EndTime = {newLap.EndTime}");

                    Thread.Sleep(5000);         // 5 sec;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                session.EndData();
                clientSession.Close();
            }

            Console.WriteLine("Finished!. \r\nHit any key to close the app.");
            Console.ReadLine();
        }

        public static IClientSession CreateSession(SessionKey sessionKey, string connectionString, string sessionIdentifier, DateTime dateOfRecording, string sessionType)
        {
            var sessionManager = SessionManager.CreateSessionManager();
            return sessionManager.CreateSession(connectionString, sessionKey, sessionIdentifier, dateOfRecording, sessionType);

        }

        public static IClientSession CreateParameter(IClientSession clientSession, int numSamples, long startTime, long endTime)
        {
            //Creating the parameter object that will be populated
            var parameter = SessionHelper.CreateSessionConfigurationForOneParameter(clientSession.Session);

            // Populating timestamps and data initially (simulating generating data to be added to the session)
            var sampleData = new List<double>(numSamples);
            var sampleTimeStamps = new List<long>(numSamples);
            var random = new Random(42);

            var currentTime = startTime;
            var timeDelta = (endTime - startTime) / numSamples;

            for (int i = 0; i < numSamples; i++)
            {
                sampleData.Add(random.NextDouble());

                sampleTimeStamps.Add(currentTime);

                currentTime += timeDelta;
            }

            // Adding the samples to the parameter inside the session
            for (int i = 0; i < numSamples; i++)
            {
                var newTimestamp = DateTime.Now.ToNanoseconds();
                var newValue = Math.Sin(i / 360.0);

                clientSession.Session.AddChannelData(
                    parameter.ChannelIds.FirstOrDefault(),
                    sampleTimeStamps[i],
                    1,
                    BitConverter.GetBytes(sampleData[i]));

                Thread.Sleep(100);
                Console.WriteLine($"Written sample. Timestamp: {newTimestamp.ToTimeString()} Value:{newValue}");
            }

            return clientSession;
        }
    }
}
