// <copyright file="FileTelemetryRecorderWithDeleteOnCloseTest.cs" company="McLaren Applied Ltd.">
// Copyright (c) McLaren Applied Ltd.</copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MAT.OCS.Core;
using MAT.OCS.RDA.Serialization.PulDeserialization;
using MAT.SQLRace.FileLoaderSample.CSV;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.FileSession;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;

namespace MAT.SQLRace.FileLoaderSample
{
    /// <summary>
    ///     Loader
    /// </summary>
    [Export(typeof(ISessionLoader))]
    [ExportMetadata("FileType", ".csv")]
    public sealed class Loader : ISessionLoader
    {
        private CsvAdapter csvAdapter;

        private const uint UnrestrictedOwnerId = 0xFFFF;
        private const string GroupName = "CsvParameters";

        public event EventHandler<FileSessionConfigurationAvailableEventArgs> OnFileSessionConfigurationAvailableEvent;
        public event EventHandler<FileSessionConfigurationLoadedEventArgs> OnFileSessionConfigurationLoadedEvent;
        public event EventHandler<FileSessionFileLoadedEventArgs> OnFileSessionFileLoadedEvent;

        public bool IsPreparedForDetailsOnly { get; private set; }
        
        public DataStatusType GetNextSample(
            string parameterIdentifier,
            long startTime,
            out double sample,
            out long timestamp)
        {
            return this.csvAdapter.GetNextSample(parameterIdentifier, startTime, out sample, out timestamp);
        }

        public ISamplesDataResult GetNextSamples(string parameterIdentifier, long startTime, int maximumNumberOfSamples)
        {
            return this.csvAdapter.GetNextSamples(parameterIdentifier, startTime, maximumNumberOfSamples);
        }

        public DataStatusType GetPrevSample(
            string parameterIdentifier,
            long startTime,
            out double sample,
            out long timestamp)
        {
            return this.csvAdapter.GetPrevSample(parameterIdentifier, startTime, out sample, out timestamp);
        }

        public ISamplesDataResult GetPrevSamples(string parameterIdentifier, long startTime, int maximumNumberOfSamples)
        {
            return this.csvAdapter.GetPrevSamples(parameterIdentifier, startTime, maximumNumberOfSamples);
        }

        public ISamplesDataResult GetSamplesForParameter(string parameterIdentifier, long startTime, long endTime)
        {
            return this.csvAdapter.GetSamplesForParameter(parameterIdentifier, startTime, endTime);
        }

        public DateTime? GetSessionEpochDate()
        {
            return this.csvAdapter.GetSessionEpochDate();
        }

        public IEnumerable<ISessionConfiguration> LoadSessionConfiguration()
        {
            var applicationGroup = new ApplicationGroup(
                "File",
                "File Application Group");
            var parameterGroup = new ParameterGroup(GroupName, "Csv Parameter Group");
            applicationGroup.AddSubGroup(parameterGroup);
            
            var parameters = new List<ParameterBase>();
            var channels = new List<IChannel>();
            var conversions = new List<ConversionBase>();

            var channelId = 1u;
            foreach (var parameterIdentifier in this.csvAdapter.ParameterIdentifiers)
            {
                if (string.IsNullOrEmpty(parameterIdentifier) ||
                    parameterIdentifier.Length > 150)
                {
                    continue;
                }

                var parameterMax = this.csvAdapter.GetParameterMaxValue(parameterIdentifier);
                
                var conversion = CreateConversion(parameterIdentifier, parameterMax);
                conversions.Add(conversion);

                channels.Add(CreateChannel(channelId, parameterIdentifier));
                
                parameters.Add(CreateParameter(parameterIdentifier, parameterMax, conversion.Name, channelId));

                channelId++;
            }

            return new List<ISessionConfiguration>
            {
                new SessionConfiguration
                {
                    ApplicationGroups = new List<ApplicationGroup>
                    {
                        applicationGroup
                    },
                    Channels = channels,
                    Conversions = new List<ConversionBase>(conversions),
                    EventDefinitions = Enumerable.Empty<EventDefinition>(),
                    Identifier = string.Format((string) "FileAppGroup_{0}", (object) this.csvAdapter.Identifier),
                    ParameterGroups = new List<ParameterGroup>()
                    {
                        parameterGroup
                    },
                    Parameters = parameters,
                    UnlockLists = new Dictionary<uint, ParameterUnlockList>()
                }
            };
        }

       public ISessionInformation LoadSessionInformation()
        {
            return new SessionInformation
            {
                EndTime = this.csvAdapter.EndTime,
                Identifier = this.csvAdapter.Identifier,
                Laps = this.csvAdapter.Laps,
                StartTime = this.csvAdapter.StartTime,
                TimeOfRecording = this.csvAdapter.TimeOfRecording,
                OwnerId = UnrestrictedOwnerId,
                Constants = Enumerable.Empty<Constant>(), // to implement if needed
                SessionDataItems = Enumerable.Empty<SessionDataItem>(), // to implement if needed
            };
        }

        public bool PrepareFileForLoadingSessionInformationAndConfiguration(string filePath, bool detailsOnly)
        {
            this.csvAdapter = new CsvAdapter(filePath, ',', 1,1,0, true);

            this.IsPreparedForDetailsOnly = true;

            return this.csvAdapter.LoadForInfoAndConfig();
        }

        public bool PrepareFileForRetrievingData(string filePath)
        {
            this.csvAdapter = new CsvAdapter(filePath, ',', 1, 1, 0, true);
            
            this.IsPreparedForDetailsOnly = false;

            return this.csvAdapter.LoadForData();
        }
        
        public void UpdateSessionDetail(string detailName, string detailValue)
        {
            // CsvAdapter doesn't support updating session details
        }

        public void CloseSession()
        {
            this.csvAdapter = null;
        }

        public ISamplesDataResult GetDataForParameter(
            string parameterIdentifier,
            long sampleRate,
            long startTime,
            long endTime,
            bool interpolate)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<Event> GetEventData()
        {
            return Enumerable.Empty<Event>();
        }

        public IEnumerable<Event> GetEventData(long startTime, long endTime)
        {
            return Enumerable.Empty<Event>();
        }

        private Parameter CreateParameter(string parameterIdentifier, double parameterMax, string conversionName,
            uint channelId)
        {
            var parameterMin = this.csvAdapter.GetParameterMinValue(parameterIdentifier);

            return new Parameter(
                parameterIdentifier,
                parameterIdentifier,
                parameterIdentifier,
                parameterMax,
                parameterMin,
                parameterMax,
                parameterMin,
                0,
                0,
                0,
                conversionName,
                new List<string>
                {
                    GroupName
                },
                channelId);
        }

        private Channel CreateChannel(uint channelId, string parameterIdentifier)
        {
            return new Channel(
                channelId,
                parameterIdentifier,
                0,
                DataType.Double64Bit,
                ChannelDataSourceType.RowData);
        }

        private ConversionBase CreateConversion(string parameterIdentifier, double parameterMax)
        {
            var parameterFormat = this.csvAdapter.GetParameterFormat(parameterIdentifier, parameterMax);

            var conversionName = "conversion_" + parameterIdentifier;
            return RationalConversion.CreateSimple1To1Conversion(
                conversionName,
                this.csvAdapter.GetParameterUnit(parameterIdentifier),
                parameterFormat);
        }
    }
}