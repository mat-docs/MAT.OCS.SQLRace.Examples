using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MAT.OCS.Core;
using MAT.SqlRace.Ssn2Splitter;
using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Domain.Infrastructure.Enumerators;
using MESL.SqlRace.Domain.Query;
using MESL.SqlRace.Domain.SessionProcessing;
using MESL.SqlRace.Enumerators;

/// <summary>
/// In order to be able to use this example you need to register McLaren Applied NuGet repository in Visual Studio. Link below
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
/// 
/// 
/// </summary>

namespace MAT.SQLRace.LoadSessionFromDatabaseAndReadParameters
{
    class Program
    {

        private const int MAXSAMPLESREQUEST = 32767;
        // TODO: Change the sessionGUID to something sensible
        private static string sessionGUID = "SOME_SESSION_GUID";
        private static List<string> parameterList = new List<string> { "vCar:Chassis", "nEngine:FIA", "aSteerFIA", "aSteerWheel:Chassis" };

        static void Main(string[] args)
        {
            //TODO: Change the connection string for it to suit your Database system
            string connectionString = @"Data Source=SOME_DATABASE_SERVER\LOCAL;Initial Catalog=SOME_DATABASE_NAME;Integrated Security=True";
            Console.WriteLine("Initializing SQL Race....");
            Console.WriteLine(Directory.GetCurrentDirectory());
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            Console.WriteLine("SQLRace has been initialized correctly");
            var clientSession = LoadSessionFromGUID(sessionGUID, connectionString);
            ReadSamples(clientSession, parameterList);

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        private static void ReadSamples(IClientSession clientSession, List<string> parameterList)
        {
            Console.WriteLine("Trying to acquire samples of the following parameters");
            foreach (var parameter in parameterList)
            {
                Console.WriteLine(parameter);
            }
            foreach (var parameter in parameterList)
            {
                var lastTimedSample = "";
                var session = clientSession.Session;
                using (var pda = clientSession.Session.CreateParameterDataAccess(parameter))
                {
                    var end = false;
                    pda.GoTo(session.StartTime);
                    var requestedSamples = -1;
                    var nSamples = pda.GetSamplesCount(session.StartTime, session.EndTime);
                    List<DataStatusType> dataStatusTypes = new List<DataStatusType>((int)nSamples);
                    List<double> data = new List<double>((int)nSamples);
                    List<long> timestamps = new List<long>((int)nSamples);
                    List<double> missingSamplesData = new List<double>((int)nSamples);
                    // This is one approach to extract samples iteratively, see below for a more streamlined approach
                    while (!end)
                    {
                        
                        if (nSamples > MAXSAMPLESREQUEST)
                        {
                            requestedSamples = MAXSAMPLESREQUEST;
                            nSamples -= MAXSAMPLESREQUEST;
                        }
                        else
                        {
                            requestedSamples = (int)nSamples;
                            end = true;
                        }

                        var samples = pda.GetNextSamples(requestedSamples);

                        for (int i = 0; i < samples.Data.Length; i++)
                        {
                            if (samples.DataStatus[i] == DataStatusType.Missing)
                            {
                                missingSamplesData.Add(samples.Data[i]);
                            }

                        }
                        data.AddRange(samples.Data);
                        timestamps.AddRange(samples.Timestamp);
                        dataStatusTypes.AddRange(samples.DataStatus);
                    }

                    var nDSSamples = dataStatusTypes.FindAll(d => d == DataStatusType.Sample).Count();
                    var nDSDefault = dataStatusTypes.FindAll(d => d == DataStatusType.Missing).Count();
                    var nDSOther = dataStatusTypes.Count() - nDSSamples - nDSDefault;
                    Console.WriteLine("{3}: Samples : {0}, Missing: {1}, Other: {2}", nDSSamples, nDSDefault, nDSOther, parameter);

                    // Another Approach
                    var numOfSamples = pda.GetSamplesCount(session.StartTime, session.EndTime);
                    pda.GoTo(session.StartTime);
                    // You have to be mindful of the int max value limitation in case your session has way more samples. 
                    var mySamples = pda.GetNextSamples((int)numOfSamples);
                    
                    // TODO: Do what you want with your samples
                }

            }

        }

        private static IClientSession LoadSessionFromGUID(string sessionGUID, string connectionString)
        {
            if (sessionGUID != null)
            {
                var sessionManager = SessionManager.CreateSessionManager();
                return sessionManager.Load(SessionKey.Parse(sessionGUID), connectionString);
            }
            else
            {
                throw new Exception("SessionGUID cannot be null. No session will be loaded");
            }
        }
    }
}
