using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MAT.OCS.Core;
using MAT.SqlRace.Ssn2Splitter;
using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Matlab;

namespace MAT.SQLRace.HelloCreateSSN2FromZeroWithParameters
{
    class Program
    {
        /// <summary>
        /// In order to be able to use this example follow these instructions;
        /// https://github.com/mat-docs/packages
        ///
        /// Whenever you are setting up your project you should use .NETFramework 4.8.
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
            //const string connectionString = @"Data Source=atlas-sr-scratch;Initial Catalog=MK_SQLRACE01;Integrated Security=True";
            const string connectionString = @"DbEngine=SQLite;Data Source=c:\temp\MyTestSession.ssn2;";

            Console.WriteLine("Initializing SQL Race....");
            Console.WriteLine(Directory.GetCurrentDirectory());
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            Console.WriteLine("SQLRace has been initialized correctly");
            // Setting up the components of a session
            var sessionKey = SessionKey.NewKey();
            var sessionName = "MyTestSession";
            var clientSession = CreateSession(sessionKey, connectionString, sessionName, DateTime.Now, "Session");
            // Adding a channel with some samples
            clientSession = CreateParameter(clientSession, 1000);

            // Add Laps
            var random = new Random();
            long startTime = SessionHelper.ConvertDateTimeToNanoseconds(DateTime.Now);
            var time = 20000000000;
            var timeStamp = startTime;

            for (var i = 0; i < 5; i++)
            {
                timeStamp += time + (i * 100000000 * random.Next(1, 500));

                var newLap = new Lap(timeStamp, Convert.ToInt16(i + 1), byte.MinValue, string.Format("Lap{0}", i + 1), true);
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

        public static IClientSession CreateParameter(IClientSession clientSession, int numSamples)
        {
            //Creating the parameter object that will be populated
            var parameter = SessionHelper.CreateSessionConfigurationForOneParameter(clientSession);

            // Populating timestamps and data initially (simulating generating data to be added to the session)
            var sampleData = new List<double>(numSamples);
            var sampleTimeStamps = new List<long>(numSamples);
            var random = new Random(42);
            for (int i = 0; i < numSamples; i++)
            {
                sampleData.Add(random.NextDouble());
                sampleTimeStamps.Add(10.SecondsToNanoseconds() + i * parameter.Channels[0].Interval);
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
