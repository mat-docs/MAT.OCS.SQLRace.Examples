using System;
using System.Collections.Generic;
using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;

namespace MAT.SQLRace.CreateSessionWIthMultipleParameters
{
    class Program
    {
        public static IClientSession CreateBasicSession(string connectionString, string sessIdentifier)
        {
            var sessionManager = SessionManager.CreateSessionManager();
            var sessionKey = SessionKey.NewKey();
            var clientSession = sessionManager.CreateSession(connectionString, sessionKey, sessIdentifier,DateTime.Now, "Session");
            return clientSession;
        }

        public static Dictionary<string, uint> PopulateSession(IClientSession clientSession, IEnumerable<string> identifiers)
        {
            // Take into account that the identifiers inside the IEnumerable cannot contain spaces
            var channels = new Dictionary<string, uint>();
            //Constants for creating parameters
            const string ConversionFunctionName = "CONV_MyParam:MyApp";
            const string ApplicationGroupName = "MyApp";
            const string ParameterGroupIdentifier = "MyParamGroup";

            var session = clientSession.Session;
            var config = session.CreateConfiguration();
            // Creating the parameter group where the channels will be added later
            var group1 = new ParameterGroup(ParameterGroupIdentifier, "SampleDescription");

            // Application group without RDA where the parameter group will live
            var appGroup1 = new ApplicationGroup(
                ApplicationGroupName,
                ApplicationGroupName,
                new List<string>
                {
                    group1.Identifier
                })
            {
                SupportsRda = false
            };

            config.AddGroup(appGroup1);
            config.AddParameterGroup(group1);
            //Conversion function
            config.AddConversion(
                new RationalConversion(ConversionFunctionName, 
                                    "kph", 
                                    "%5.2f", 
                                    0.0, 
                                    1.0, 
                                    0.0, 
                                    0.0, 
                                    0.0, 
                                    1.0)
                );
            Frequency samplingFrequency = new Frequency(2, FrequencyUnit.Hz);
            uint channelId = 1;
            // We start to iterate over the identifiers to add the parameters to the param group
            foreach (string identifier in identifiers)
            {
                channels[identifier] = channelId;
                var channel = new Channel(
                    channelId++,
                    "Channel" + channelId,
                    samplingFrequency.ToInterval(),
                    DataType.Signed16Bit,
                    ChannelDataSourceType.Periodic);

                var parameter = new Parameter(
                    identifier,
                    identifier,
                    identifier,
                    400, // Your max
                    0, // Your min
                    1,
                    0,
                    0,
                    255,
                    0,
                    ConversionFunctionName,
                    new List<string>
                    {
                        ParameterGroupIdentifier
                    },
                    channel.Id,
                    ApplicationGroupName);

                config.AddChannel(channel);
                config.AddParameter(parameter);
            }

            config.Commit();
            return channels;

        }

        public static void FillSessionDetails(IClientSession clientSession, IEnumerable<string> details)
        {
            var session = clientSession.Session;
            // Populating session details
            var i = 1;
            foreach (var detail in details)
            {
                session.Items.Add(new SessionDataItem("Detail" + i, detail));
                i++;
            }

        }

        public static void AddSampleLap(IClientSession clientSession, long startTime, short number, byte triggersource, string name, bool countForFastestLap)
        {
            var session = clientSession.Session;

            var lap = new Lap(startTime, number, triggersource, name, countForFastestLap);
            // You can use the lap object to edit the different properties of it
            //lap.EndTime = 1;

            session.Laps.Add(lap);
            //In case you would like to add data attached to the lap you can do the following
            session.Laps.LapItems.Add(new LapDataItem(number, name, "Race start"));
        }

        static void Main(string[] args)
        {
            string connectionString = $@"DbEngine=SQLite;Data Source={
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                }\McLaren Applied Technologies\ATLAS 10\SQL Race\test.db;";

            Console.WriteLine("Initializing SQL Race....");
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();

            Console.WriteLine($"Creating session");
            var clientSession = CreateBasicSession(connectionString, "SampleIdentifier");

            // Adding channels
            var identifiers = new List<string>
            {
                "sampleparam1",
                "sampleparam2"
            };

            var channels = PopulateSession(clientSession, identifiers);

            //Adding session details
            var details = new List<string>
            {
                "sampledetail1",
                "sampledetail2"
            };
              
            FillSessionDetails(clientSession, details);
            //Adding a sample lap
            AddSampleLap(clientSession, 0, 1, 0, "Lap1", false);

            Console.WriteLine("Press ENTER or RETURN key to finish...");
            Console.ReadLine();
        }
    }
}
