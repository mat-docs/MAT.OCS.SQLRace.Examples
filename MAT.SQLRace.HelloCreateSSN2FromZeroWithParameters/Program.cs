using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MAT.OCS.Core;
using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;

namespace MAT.SQLRace.HelloCreateSSN2FromZeroWithParameters
{
    class Program
    {
        /// <summary>
        /// In order to be able to use this example follow these instructions;
        /// https://github.com/mat-docs/packages
        ///
        /// Whenever you are setting up your project you should use .NET 8.
        /// You need to set up the compilation to be for x64 processors in order for this example to work
        /// 
        /// Once registered in Visual Studio, you need to install MESL.SQLRace.API package from NuGet making sure that you have selected the McLaren Applied Github packages in the top
        /// right corner (defaults to nuget.org, where if you search for the package it would not show).
        /// Apart from this NuGet package, if you plan to load SSN files you need to follow these steps too: (not needed for this example)
        /// - Install MAT.ATLAS.SupportFiles from NuGet directory making sure that you have selected on the top right corner MA repository
        /// - Setup the build config to build in x64 based CPUs. This is required for SSNs to be loaded.
        /// Once this package is installed, you would only need to pay attenion to the TODOs written in the code.
        ///
        /// Find the documentation for the API in the link below
        /// https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
        /// 
        /// Further examples can be found here:
        /// https://github.com/mat-docs/MAT.OCS.SQLRace.Examples
        /// </summary>
        static void Main(string[] args)
        {
            // TODO: Change the location to where do you want the session to be created
            const string fileFullPath = "C:\\temp\\MyTestSession.ssn2";

            if (File.Exists(fileFullPath))
            {
                Console.WriteLine($"The file \'{fileFullPath}\' already exists");
                Console.WriteLine("Finished!..");
                return;
            }

            const string connectionString = @"DbEngine=SQLite;Data Source=c:\temp\MyTestSession.ssn2;";

            Console.WriteLine("Initializing SQL Race....");
            Console.WriteLine(Directory.GetCurrentDirectory());
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            Console.WriteLine("SQLRace has been initialized correctly");

            // Creating a Session
            var dateTimeNow = DateTime.Now;
            var endDateTime = dateTimeNow.AddMinutes(5);

            var timeToday = dateTimeNow - DateTime.Today;
            var endTimeToday = endDateTime - DateTime.Today;

            long startTime = timeToday.ToNanoseconds();
            long endTime = endTimeToday.ToNanoseconds();

            // This is another way to get the Nanoseconds values,
            // some people may find it to be more explicit and illustrative.
            //
            //long startTime = SessionHelper.ConvertDateTimeToNanoseconds(dateTimeNow);
            //long endTime = SessionHelper.ConvertDateTimeToNanoseconds(endDateTime);

            string sessionDescription = string.Format("Example::: {0}", dateTimeNow.ToString("dd-MMM-yy hh:mm:ss tt"));

            var sessionKey = SessionKey.NewKey();
            var sessionName = "MyTestSession";

            // Create a session first
            var clientSession = CreateSession(sessionKey, connectionString, sessionName, dateTimeNow, "Session");

            var session = clientSession.Session;

            // Add some session details which allows values as String, Long, Double, Bool, Datetime, Byte[] etc.
            session.Items.Add(new SessionDataItem("Driver", "Test Driver"));
            session.Items.Add(new SessionDataItem("Car", "Test Car"));

            // Setting up the components of a session
            // Adding a channel with some samples
            var numSamples = 1000;
            clientSession = CreateParameter(clientSession, 1000, startTime, endTime);

            // Add Laps
            var lapNumber = 5;
            var lapTimeDelta = (endTime - startTime) / (lapNumber + 1);
            var timeStamp = startTime;

            for (var i = 0; i < 5; i++)
            {
                if (i > 0)
                    timeStamp += lapTimeDelta;

                var newLap = new Lap(timeStamp, Convert.ToInt16(i + 1), byte.MinValue, string.Format("Lap {0}", i + 1), true);
                clientSession.Session.LapCollection.Add(newLap);
            }

            // Closing the session before exporting.
            clientSession.Close();

            Console.WriteLine("Finished.");
        }

        public static IClientSession CreateSession(SessionKey sessionKey, string connectionString, string sessionName, DateTime dateOfRecording, string sessionType)
        {
            var sessionManager = MESL.SqlRace.Domain.SessionManager.CreateSessionManager();
            return sessionManager.CreateSession(connectionString, sessionKey, sessionName, dateOfRecording, sessionType);

        }

        public static IClientSession CreateParameter(IClientSession clientSession, int numSamples, long startTime, long endTime)
        {
            //Creating the parameter object that will be populated
            var parameter = SessionHelper.CreateSessionConfigurationForOneParameter(clientSession);

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
                clientSession.Session.AddChannelData(parameter.ChannelIds.FirstOrDefault(),
                    sampleTimeStamps[i],
                    1,
                    BitConverter.GetBytes(sampleData[i]));
            }

            return clientSession;
        }
    }
}
