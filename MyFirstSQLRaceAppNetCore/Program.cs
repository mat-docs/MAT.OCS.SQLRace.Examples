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
            // Write .Net framework version
            var frameworkTargetAttribute = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>();
            if (string.IsNullOrWhiteSpace(frameworkTargetAttribute?.FrameworkDisplayName))
            {
                Console.WriteLine(RuntimeInformation.FrameworkDescription);
            }
            else
            {
                Console.WriteLine(frameworkTargetAttribute.FrameworkDisplayName);
            }

            while (Do())
            {
            }
        }

        private static bool Do()
        {
            Console.WriteLine("Choose Session Type: SQL[S]erver, SQ[L]ite, SS[N] or e[X]it");
            var key = char.ToLowerInvariant(Console.ReadKey(true).KeyChar);

            var sessionKey = CarSessionKey;
            string connectionString = default;
            switch (key)
            {
                case 's':
                    connectionString = "Server=localhost;Database=FIA;Trusted_Connection=True";
                    break;
                case 'l':
                    var executingLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    connectionString = $@"DbEngine=SQLite;Data Source={Path.Combine(executingLocation, Ssn2)}";
                    break;
                case 'n':
                    Console.WriteLine("SSN not supported yet!");
                    Console.WriteLine();
                    return true;
                case 'x':
                    return false;

                default:
                    Console.WriteLine("Unknown option!");
                    Console.WriteLine();
                    return true;
            }

            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            try
            {
                ReadSamples(connectionString, CarSessionKey);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine();
            }

            Console.WriteLine($"Session: {sessionKey}");
            Console.WriteLine($"Connection String: {connectionString}");
            Console.WriteLine();
            return true;
        }

        private static void ReadSamples(string connectionString, SessionKey sessionKey)
        {
            var identifiers = new List<string>
            {
                //"vCar:Chassis",
                //"MEngine:Controller",
                "gLat:Chassis",
            };

            Console.WriteLine("Loading Session...");

            using (var clientSession = LoadSession(sessionKey, connectionString))
            {
                Console.WriteLine("Session loaded...");
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

        private static IClientSession LoadSession(SessionKey sessionKey, string connectionString)
        {
            var sessionManager = SessionManager.CreateSessionManager();
            return sessionManager.Load(sessionKey, connectionString);
        }
    }
}
