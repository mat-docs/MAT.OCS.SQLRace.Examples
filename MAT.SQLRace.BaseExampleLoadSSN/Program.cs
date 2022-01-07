using System;
using System.IO;
using MESL.SqlRace.Common;
using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;

namespace BaseExampleSSN
{
    class Program
    {
        /// <summary>
        /// In order to be able to use this example find the NuGet packages required below:
        /// https://atlas.mclarenapplied.com/developer/nuget/
        ///
        /// Whenever you are setting up your project you should use .NETFramework 4.8.
        /// 
        /// Once registered in Visual Studio, you need to install MESL.SQLRace.API package from NuGet making sure that you have selected the McLaren Applied Github packages in the top
        /// right corner (defaults to nuget.org, where if you search for the package it would not show).
        /// Apart from this NuGet package, if you plan to use SSN files you need to follow these steps too:
        /// - Install MAT.ATLAS.SupportFiles from NuGet directory making sure that you have selected on the top right corner MA repository
        /// - Setup the build config to build in x64 based CPUs. This is required for SSNs to be loaded.
        /// Once this package is installed, you would only need to pay attenion to the TODOs written in the code.
        ///
        /// Find the documentation for the API in the link below
        /// https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
        /// 
        /// Further examples can be found here:
        /// https://github.com/mat-docs/MAT.OCS.SQLRace.Examples
        /// 
        /// </summary>
        static void Main(string[] args)
        {
            //TODO: Change the path to a .ssn file that you have locally.
            const string pathToFile = @"C:\Users\YOUR_USER\SOME_FOLDER\SOME_FILE.ssn";

            //Initialize SQLRaceAPI
            Console.WriteLine("Initializing SQL Race....");
            Console.WriteLine(Directory.GetCurrentDirectory());
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            Console.WriteLine("SQLRace has been initialized correctly");
            var clientSession = LoadSession(pathToFile);
            ReadSamples(clientSession);

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
            Log.SetMinimumLoggingLevel(LogLevel.Warn);
        }

        private static void ReadSamples(IClientSession clientSession)
        {
            const string vCarIdentifier = "vCar:Chassis";
            using (var pda = clientSession.Session.CreateParameterDataAccess(vCarIdentifier))
            {
                var sessionMidPoint = clientSession.Session.StartTime + (clientSession.Session.EndTime - clientSession.Session.StartTime) / 2;
                pda.GoTo(sessionMidPoint);
                //MAX SAMPLES RETRIEVED: 10000
                var samples = pda.GetNextSamples(10);

                Console.WriteLine($"** Data for {vCarIdentifier}");
                for (var i = 0; i < samples.SampleCount; i++)
                {
                    Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                }
            }
        }

        private static IClientSession LoadSession(string pathToFile)
        {
            var fileSessionManager = FileSessionManager.CreateFileSessionManager();
            var clientSession = fileSessionManager.Load(pathToFile); // session with associates

            if (clientSession == null)
            {
                Console.WriteLine("Session not found");
                return null;
            }

            return clientSession;
        }
    }
}
