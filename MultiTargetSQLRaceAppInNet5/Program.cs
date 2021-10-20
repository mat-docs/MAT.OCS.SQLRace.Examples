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
        private const string Ssn = "AtlasWriteTest.ssn";
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

            Console.WriteLine("Initialize SQLRace...");

            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            while (Do())
            {
            }
        }

        private static bool Do()
        {
            Console.WriteLine("Choose Session Type: SQL[S]erver, SQ[L]ite, SS[N], [R]ecorder or e[X]it");
            var key = char.ToLowerInvariant(Console.ReadKey(true).KeyChar);

            var executingLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            string connectionString = default;
            string sessionSource;
            IClientSession clientSession;
            SessionBase session;
            switch (key)
            {
                case 's':
                case 'l':
                    if (key == 's')
                    {
                        connectionString = "Server=localhost;Database=FIA;Trusted_Connection=True";
                    }
                    else
                    {
                        connectionString = $@"DbEngine=SQLite;Data Source={Path.Combine(executingLocation, Ssn2)}";
                    }

                    clientSession = LoadSession(CarSessionKey, connectionString);
                    clientSession.Session.LoadConfiguration();

                    session = clientSession.Session;
                    sessionSource = session.Key.ToString();
                    break;
                case 'n':
                    var ssnPath = @"C:\Users\steven.morgan\Documents\McLaren Electronic Systems\ATLAS 9\Data\200115114824.ssn"; //Path.Combine(executingLocation, Ssn);
                    clientSession = LoadSession(ssnPath);
                    if (clientSession == null)
                    {
                        Console.WriteLine($"Invalid SSN: {ssnPath}");
                        Console.WriteLine();
                        return true;
                    }

                    session = clientSession.Session;
                    sessionSource = ssnPath;
                    break;
                case 'r':
                    StandaloneRecorder.Do();
                    return true;
                case 'x':
                    return false;

                default:
                    Console.WriteLine("Unknown option!");
                    Console.WriteLine();
                    return true;
            }

            try
            {
                using (clientSession)
                {
                    ReadSamples(session);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine();
            }

            Console.WriteLine($"Session: {sessionSource}");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine($"Connection String: {connectionString}");
            }

            Console.WriteLine();
            return true;
        }

        private static void ReadSamples(SessionBase session)
        {
            var identifiers = new List<string>
            {
                //"vCar:Chassis",
                //"MEngine:Controller",
                "gLat:Chassis",
            };

            Console.WriteLine("Session loaded...");

            foreach (var identifier in identifiers)
            {
                using (var pda = session.CreateParameterDataAccess(identifier))
                {
                    var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

                    Console.WriteLine($"** Samples for {identifier}");
                    for (var i = 0; i < samples.SampleCount; i++)
                    {
                        Console.WriteLine($"{samples.Timestamp[i].ToTimeString()} {samples.Data[i]}");
                    }
                }
            }

            Console.WriteLine();
        }

        private static IClientSession LoadSession(SessionKey sessionKey, string connectionString)
        {
            Console.WriteLine("Loading Session...");

            var sessionManager = SessionManager.CreateSessionManager();
            return sessionManager.Load(sessionKey, connectionString);
        }

        private static IClientSession LoadSession(string ssnPath)
        {
            Console.WriteLine("Loading Session...");

            var fileSessionManager = FileSessionManager.CreateFileSessionManager();
            return fileSessionManager.Load(ssnPath); // No error on failure!
        }
    }
}
