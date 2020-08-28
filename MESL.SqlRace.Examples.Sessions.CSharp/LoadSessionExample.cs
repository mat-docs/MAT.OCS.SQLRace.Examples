// <copyright file="LoadSessionExample.cs" company="McLaren Applied Technologies Ltd">
// Copyright (c) McLaren Applied Technologies Ltd</copyright>

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MESL.SqlRace.Examples.Sessions.CSharp
{
    /// <summary>
    /// A Class demonstrating loading of a SQL Race session.
    /// </summary>
    public class LoadSessionExample
    {
        private readonly SessionManager sessionManager;
        private readonly string connectionString;

        private double[] data;
        private long[] timeStamps;
        private DataStatusType[] dataStatus;

        /// <summary>
        /// Initialises a new instance of the <see cref="LoadSessionExample"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        public LoadSessionExample(string connectionString)
        {
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();
            this.sessionManager = SessionManager.CreateSessionManager();
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Gets the Data read from the session. used for graphical display.
        /// </summary>
        public double[] Data
        {
            get { return this.data; }
        }

        /// <summary>
        /// Gets the Time stamp read from the session. used for graphical display.
        /// </summary>
        public long[] TimeStamps
        {
            get { return this.timeStamps; }
        }

        /// <summary>
        /// Gets the Data status read from the session. used for grid display.
        /// </summary>
        public DataStatusType[] DataStatus
        {
            get { return this.dataStatus; }
        }

        /// <summary>
        /// An example demonstrating how to load a session and read data
        /// </summary>
        /// <returns>String value, an output of the example run</returns>
        public string LoadSessionAndReadData()
        {
            try
            {
                const string Parameter = "highFrequency_1KHz";
                const int NumberOfSamples = 25;

                // Create the session and add data first
                var sessionCreation = new SessionCreationExample(this.connectionString);
                SessionKey sessionKey = sessionCreation.CreateSessionAndAddData();

                // Load the just created session
                var clientSession = this.sessionManager.Load(sessionKey, this.connectionString);

                var session = clientSession.Session;
                // Read parameters
                var parameters = new List<ParameterBase>(session.Parameters);

                // Read all channels
                var channels = parameters.OfType<Parameter>().SelectMany(p => p.Channels);

                // Read all conversions
                var conversions = new List<ConversionBase>(session.Conversions);

                // Read Session details and constants
                var sessionDataItems = new List<SessionDataItem>(session.Items);
                var constants = new List<Constant>(session.Constants);

                // Read all the Laps, Lap data items and lap constants
                var laps = new List<Lap>(session.LapCollection);
                var lapDataItems = new List<LapDataItem>(session.LapCollection.LapItems);
                var lapConstants = new List<LapConstant>(session.LapCollection.Constants);

                // Use PDA to read the data

                this.data = new double[NumberOfSamples];
                this.timeStamps = new long[NumberOfSamples];
                this.dataStatus = new DataStatusType[NumberOfSamples];

                var pdaParameter = parameters.FirstOrDefault(p => p.Identifier == Parameter) as Parameter;
                if (pdaParameter != null)
                {
                    var pda = session.CreateParameterDataAccess(pdaParameter.Identifier);
                    pda.GoTo(session.StartTime);

                    // GetNextData returns samples values
                    pda.SampleTime = pdaParameter.Channels[0].Interval;
                    pda.GetNextData(NumberOfSamples, template: new ParameterValuesTemplate { Data = data, DataStatus = this.dataStatus });

                    // GetNextSamples returns actual samples
                    pda.GetNextSamples(NumberOfSamples, template: new ParameterValuesTemplate
                    {
                        Data = this.data,
                        DataStatus = this.dataStatus,
                        Timestamp = this.timeStamps
                    });


                }

                clientSession.Close();

                return
                    new StringBuilder().AppendFormat("{1}Session with the Key {0} loaded successfully.{1}", sessionKey, Environment.NewLine)
                        .AppendFormat("Parameters = {0}{1}", parameters.Count, Environment.NewLine)
                        .AppendFormat("Channels = {0}{1}", channels.Count(), Environment.NewLine)
                        .AppendFormat("Conversions = {0}{1}", conversions.Count, Environment.NewLine)
                        .AppendFormat("SessionDataItems = {0}{1}", sessionDataItems.Count, Environment.NewLine)
                        .AppendFormat("Constants = {0}{1}", constants.Count, Environment.NewLine)
                        .AppendFormat("Laps = {0}{1}", laps.Count, Environment.NewLine)
                        .AppendFormat("LapConstants = {0}{1}", lapConstants.Count, Environment.NewLine)
                        .AppendFormat("LapDataItems = {0}", lapDataItems.Count)
                        .ToString();

            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
