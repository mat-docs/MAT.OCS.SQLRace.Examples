// <copyright file="SessionCreationExample.cs" company="McLaren Applied Technologies Ltd">
// Copyright (c) McLaren Applied Technologies Ltd</copyright>

using System;
using System.Collections.Generic;
using System.Linq;

using MAT.OCS.Core;

using MESL.SqlRace.Domain;
using MESL.SqlRace.Enumerators;

namespace MESL.SqlRace.Examples.Sessions.CSharp
{
    /// <summary>
    /// A class for demonstrating Session creation examples in SQL Race
    /// </summary>
    public class SessionCreationExample
    {
        private readonly SessionManager sessionManager;
        private readonly string connectionString;

        /// <summary>
        /// Initialises a new instance of the <see cref="SessionCreationExample"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        public SessionCreationExample(string connectionString)
        {
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();
            this.sessionManager = SessionManager.CreateSessionManager();
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Creates a session and adds data.
        /// </summary>
        /// <returns>Guid of the session</returns>
        public SessionKey CreateSessionAndAddData()
        {
            long startTime = ConvertDateTimeToNanoseconds(DateTime.Now);
            long endTime = ConvertDateTimeToNanoseconds(DateTime.Now.AddMinutes(2));
            SessionKey sessionKey = SessionKey.NewKey();
            DateTime date = DateTime.Now;
            string sessionDescription = string.Format("Example::: {0}", date.ToString("dd-MMM-yy hh:mm:ss tt"));
            var random = new Random();

            // Create a session first
            var clientSession = this.sessionManager.CreateSession(this.connectionString, sessionKey, sessionDescription, date, "TAG-310");
            var session = clientSession.Session;

            // Add some session details which allows values as String, Long, Double, Bool, Datetime, Byte[] etc.
            session.Items.Add(new SessionDataItem("Driver Name", "Driver xxxxx"));
            session.Items.Add(new SessionDataItem("Race", "Silverstone GP"));
            session.Items.Add(new SessionDataItem("Date", DateTime.Now.AddDays(7)));
            session.Items.Add(new SessionDataItem("Fuel level in gallons", 56.45));

            // Add conversions - Rational and table
            var config = session.CreateConfiguration();
            var conversions = new List<ConversionBase>
                {
                    new RationalConversion("Rational_conv", "mph", "%5.2f", 0.0, 1.0, 0.0, 0.0, 0.0, 1.0)
                };

            var rawValues = new double[] { 0, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95 };
            var calibratedValues = new double[] { 50, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120, 125, 130, 135, 140, 145 };
            conversions.Add(new TableConversion("Table_conv", "mph", "%5.2f", rawValues, calibratedValues, false));
            config.AddConversions(conversions);

            // Add Application and Parameter groups
            var parameterGroups = new List<ParameterGroup>
                                          {
                                              new ParameterGroup("chassis", "Chassis group"),
                                              new ParameterGroup("engine", "Engine group"),
                                              new ParameterGroup("bios", "Bios group")
                                          };
            var applicationGroup = new ApplicationGroup("SQLRace App Group", "SQLRace App Group", parameterGroups.Select(x => x.Identifier).ToList());
            config.AddGroup(applicationGroup);
            config.AddParameterGroups(parameterGroups);

            // Add Parameters - Periodic, SlowRow and Synchro
            var channel1 = new Channel(1, "lowFrequencyPeriodic1", 100000000, DataType.FloatingPoint32Bit, ChannelDataSourceType.Periodic);
            var channel2 = new Channel(2, "highFrequencyPeriodic", 1000000, DataType.FloatingPoint32Bit, ChannelDataSourceType.Periodic);
            var channel3 = new Channel(3, "lowFrequencyPeriodic2", 50000000, DataType.FloatingPoint32Bit, ChannelDataSourceType.Periodic);
            var channel4 = new Channel(100, "synchroChannel", 0, DataType.Unsigned8Bit, ChannelDataSourceType.Synchro);
            var channel5 = new Channel(1000, "SlowRowDataChannel", 0, DataType.Unsigned8Bit, ChannelDataSourceType.RowData);
            var parameters = new List<ParameterBase>
                {
                    new Parameter("lowFrequency_10Hz", "lowFrequency_10Hz", "Low frequency parameter", 200, 0, 200, 0, 0, 0xffffffff, 0, conversions[0].Name, new List<string> { "chassis" }, channel1.Id), 
                    new Parameter("highFrequency_1KHz", "highFrequency_1KHz", "High frequency parameter", 200, 0, 200, 0, 0, 0xffffffff, 0, conversions[0].Name, new List<string> { "chassis" }, channel2.Id),
                    new Parameter("lowFrequency_20Hz", "lowFrequency_20Hz", "parameter for Table conversion", 200, 0, 200, 0, 0, 0xffffffff, 0, conversions[1].Name, new List<string> { "chassis" }, channel3.Id),
                    new Parameter("synchroData", "synchroData", "Synchro data parameter", 200, 0, 200, 0, 0, 0xff, 0, conversions[0].Name, new List<string> { "engine" }, channel4.Id),
                    new Parameter("slowRowData", "slowRowData", "slow Row Data parameter", 200, 0, 200, 0, 0, 0xff, 0, conversions[0].Name, new List<string> { "bios" }, channel5.Id)
                };

            config.AddParameters(parameters);
            config.AddChannels(new List<Channel> { channel1, channel2, channel3, channel4, channel5 });

            // Add event definitions
            var eventDefinitions = new List<EventDefinition>
                {
                    new EventDefinition(
                        1,
                        "FirstEvent",
                        EventPriorityType.High,
                        new List<string>
                        {
                            conversions[0].Name,
                            conversions[0].Name,
                            conversions[0].Name
                        },
                        "Group1"),
                    new EventDefinition(
                        2,
                        "SecondEvent",
                        EventPriorityType.Low,
                        new List<string>
                        {
                            conversions[0].Name,
                            conversions[0].Name,
                            conversions[0].Name
                        },
                        "Group1"),
                    new EventDefinition(
                        3,
                        "LastEvent",
                        EventPriorityType.Medium,
                        new List<string>
                        {
                            conversions[0].Name,
                            conversions[0].Name,
                            conversions[0].Name
                        },
                        "Group2")
                };
            config.AddEventDefinitions(eventDefinitions);
            config.Commit();

            // Each samples will be seperated by 250*256 + 100 = (64100 * delta scale) nano secs.
            var synchroBurst = new byte[] { 11, 100, 250, 22, 100, 250, 33, 100, 250, 44, 100, 250, 55, 100, 250, 66, 100, 250, 77, 100, 250, 88, 100, 250, 99 };
            var doubleData = new[] { 0, 10.2, 15.45, 20, 25.82, 30, 35, 40, 45.01, 50.99, 55.7, 60.4, 65, 70, 75, 80, 85.67, 90.77, 95 };
            var doubleDataBytes = new byte[doubleData.Length * 4];
            var index = 0;

            // Convert double array to byte array
            foreach (var byteArray in doubleData.Select(data => BitConverter.GetBytes(Convert.ToSingle(data))))
            {
                Buffer.BlockCopy(byteArray, 0, doubleDataBytes, index, 4);
                index += 4;
            }

            foreach (var parameter in session.Parameters.OfType<Parameter>())
            {
                var currentTime = startTime;
                var channel = parameter.Channels[0];
                byte counter = 0;

                while (currentTime < endTime)
                {
                    switch (channel.DataSource)
                    {
                        case ChannelDataSourceType.Periodic:
                            session.AddChannelData(channel.Id, currentTime, doubleData.Length, doubleDataBytes);
                            currentTime += channel.Interval * doubleData.Length;
                            break;
                        case ChannelDataSourceType.Synchro:
                            session.AddSynchroChannelData(currentTime, channel.Id, counter++, 2000, synchroBurst);
                            currentTime += 1000000000;
                            break;
                        case ChannelDataSourceType.RowData:
                            session.AddRowData(currentTime, new List<uint> { channel.Id }, new[] { (byte)random.Next(0, 200) });
                            currentTime += 5000000000;
                            break;
                    }
                }
            }

            // Add Laps
            var time = 20000000000;
            var timeStamp = startTime;
            for (var i = 0; i < 5; i++)
            {
                timeStamp += time + (i * 100000000 * random.Next(1, 500));
                session.LapCollection.Add(new Lap(timeStamp, Convert.ToInt16(i + 1), 0, string.Format("Lap{0}", i + 1), true));
            }

            // Add event instances
            time = 10000000000;
            timeStamp = startTime;
            for (var i = 0; i < 5; i++)
            {
                timeStamp += time + (i * 100000000 * random.Next(1, 500));
                session.Events.AddEventData(1, "Group1", timeStamp, new List<double> { 300, 200, 100 }, true);
                session.Events.AddEventData(2, "Group1", timeStamp + 100000000, new List<double> { 243, 14, 5 }, true);
                session.Events.AddEventData(3, "Group2", timeStamp + 200000000, new List<double> { 111, 0, 3456 }, true);
            }

            // Add LapItems, Session Constants and LapConstants
            session.LapCollection.LapItems.Add(new LapDataItem(5, "Information", "Pit Stop in next Lap"));
            session.Constants.Add(new Constant("Race Start car weight", 653.92, "Car weight at the start of the race", "kg", "%5.2f"));
            session.LapCollection.Constants.Add(new LapConstant(4, "FuelConsumed", 4.5, "Fuel consumed for this lap", "ltr", "%4.2f"));
            clientSession.Close();


            return sessionKey;
        }

        /// <summary>
        /// Converts a DateTime to nanoseconds
        /// </summary>
        /// <param name="time">DateTime object</param>
        /// <returns>Time in nanoseconds</returns>
        private static long ConvertDateTimeToNanoseconds(DateTime time)
        {
            int hours = time.Hour * 3600000;
            int minutes = time.Minute * 60000;
            int seconds = time.Second * 1000;
            int milliseconds = time.Millisecond;
            string nanosecondString = Convert.ToString(milliseconds + seconds + minutes + hours) + "000000";
            return Convert.ToInt64(nanosecondString);
        }
    }
}