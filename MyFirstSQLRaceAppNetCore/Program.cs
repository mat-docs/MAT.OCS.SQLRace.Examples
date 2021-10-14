using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MAT.OCS.Core;

using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;

namespace MyFirstSQLRaceAppNet
{
    class Program
    {
        private const string Ssn2 = "210930134636.ssn2";
        private static readonly SessionKey CarSessionKey = SessionKey.Parse("8133368c-75b3-42d4-8d93-3c9a53d4eaaa");

        static void Main(string[] args)
        {
            var frameworkTargetAttribute = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>();
            Console.WriteLine($"Net Version {(!string.IsNullOrWhiteSpace(frameworkTargetAttribute?.FrameworkDisplayName) ? frameworkTargetAttribute.FrameworkDisplayName : RuntimeInformation.FrameworkDescription)}");

#if NETFRAMEWORK
            var executingLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // SQLite style connection string
            var connectionString = $@"DbEngine=SQLite;Data Source={Path.Combine(executingLocation, Ssn2)}";
#else
            // SQLServer style connection string
            var connectionString = "Server=localhost;Database=FIA;Trusted_Connection=True";
#endif
            Console.WriteLine("Initializing SQL Race....");

            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            ReadSamples(connectionString, CarSessionKey);

            Console.WriteLine("Press ENTER key to close.");
            Console.ReadLine();
        }

        /// <summary>
        ///     Reads samples from an historic session.
        /// </summary>
        private static void ReadSamples(string connectionString, SessionKey sessionKey)
        {
            var identifiers = new List<string>
            {
                //"vCar:Chassis",
                //"MEngine:Controller",
                "gLat:Chassis",
            };

            Console.WriteLine("Loading session....");

            using (var clientSession = LoadSession(sessionKey, connectionString))
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

        private static IClientSession LoadSession(SessionKey sessionKey, string connection)
        {
            var sessionManager = SessionManager.CreateSessionManager();
            return sessionManager.Load(sessionKey, connection);
        }

    }
}
