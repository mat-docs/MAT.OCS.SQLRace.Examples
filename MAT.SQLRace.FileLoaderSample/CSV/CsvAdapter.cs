// <copyright file="FileTelemetryRecorderWithDeleteOnCloseTest.cs" company="McLaren Applied Ltd.">
// Copyright (c) McLaren Applied Ltd.</copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;

namespace MAT.SQLRace.FileLoaderSample.CSV
{
    internal class CsvAdapter  
    {
        //Format options
        private readonly char columnDelimiter;
        private readonly int headerLines;
        private readonly int parameterNamesLine;
        private readonly int unitLine;
        private readonly bool dateExists;

        private readonly string filePath;

        private const string DefaultLap1Name = "Lap 1";
        private const int ParameterMaxValue = 100;
        private const int ParameterMinValue = 0;

        private string[] parametersName;
        private long[] timestamps;
        private double[][] values;
        

        public CsvAdapter(string filePath, char columnDelimiter, int headerLines, int parameterNamesLine, int unitLine, bool dateExists)
        {
            this.filePath = filePath;
            this.columnDelimiter = columnDelimiter;
            this.headerLines = headerLines;
            this.parameterNamesLine = parameterNamesLine;
            this.unitLine = unitLine;
            this.dateExists = dateExists;
        }

        /// <summary>
        ///     EndTime
        /// </summary>
        public long EndTime { get; private set; }

        /// <summary>
        ///     Identifier
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        ///     True if the data has been loaded
        /// </summary>
        public bool IsDataLoaded { get; private set; }

        /// <summary>
        ///     ParameterIdentifiers
        /// </summary>
        public IList<string> ParameterIdentifiers { get; private set; }

        /// <summary>
        ///     ParameterIdentifiers
        /// </summary>
        public IDictionary<string, string> ParameterUnits { get; private set; }

        /// <summary>
        ///     StartTime
        /// </summary>
        public long StartTime { get; private set; }

        /// <summary>
        ///     TimeOfRecording
        /// </summary>
        public DateTime TimeOfRecording { get; private set; }

        /// <summary>
        ///  Laps
        /// </summary>
        public List<Lap> Laps { get; set; } = new List<Lap>();
        
        /// <summary>
        ///     GetNextSample
        /// </summary>
        /// <param name="parameterIdentifier"></param>
        /// <param name="startTime"></param>
        /// <param name="sample"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public DataStatusType GetNextSample(
            string parameterIdentifier,
            long startTime,
            out double sample,
            out long timestamp)
        {
            return GetSample(parameterIdentifier, startTime, out sample, out timestamp);
        }

        /// <summary>
        ///     GetNextSamples
        /// </summary>
        /// <param name="parameterIdentifier"></param>
        /// <param name="startTime"></param>
        /// <param name="maximumNumberOfSamples"></param>
        /// <returns></returns>
        public ISamplesDataResult GetNextSamples(string parameterIdentifier, long startTime, int maximumNumberOfSamples)
        {
            var parameterIdentifierIndex = this.GetParameterIndexInData(parameterIdentifier);
            if (parameterIdentifierIndex < 0)
            {
                return null;
            }

            var startIndex = this.GetIndexForTimestamp(startTime);
            var endIndex = this.GetIndexForTimestamp(this.EndTime);

            var data = new List<double>();
            var timestamps = new List<long>();
            var dataStatusTypes = new List<DataStatusType>();

            var sampleCount = 0;

            for (var i = startIndex; i < endIndex; i++)
            {
                var value = this.values[i][parameterIdentifierIndex];

                if (double.IsNaN(value))
                {
                    continue;
                }

                data.Add(value);
                timestamps.Add(this.GetTimeStamps()[i]);
                dataStatusTypes.Add(DataStatusType.Sample);

                sampleCount++;

                if (sampleCount >= maximumNumberOfSamples)
                {
                    break;
                }
            }

            return new SamplesDataResult(data.ToArray(), dataStatusTypes.ToArray(), timestamps.ToArray(), sampleCount);
        }
        
        /// <summary>
        ///     Gets the parameter format.
        /// </summary>
        /// <param name="parameterIdentifier">The parameter identifier</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string GetParameterFormat(string parameterIdentifier, double value)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                Math.Abs(value) < double.Epsilon)
            {
                return "%2.0f";
            }

            var numberOfDigitsBeforeDecimal = Math.Truncate(Math.Abs(value))
                .ToString(CultureInfo.InvariantCulture)
                .Length;

            var text = value.ToString(CultureInfo.InvariantCulture).TrimEnd('0');
            var decpoint = text.IndexOf('.');
            var numberOfDecimals = decpoint < 0 ? 0 : text.Length - decpoint - 1;

            return $"%{numberOfDigitsBeforeDecimal}.{numberOfDecimals}f";
        }

        /// <summary>
        ///     Maximums the parameter value.
        /// </summary>
        /// <param name="parameterIdentifier">The parameter identifier.</param>
        /// <returns></returns>
        public double GetParameterMaxValue(string parameterIdentifier)
        {
            return this.GetAllParameterSamples(parameterIdentifier).DefaultIfEmpty(ParameterMaxValue).Max();
        }

        /// <summary>
        ///     Gets the parameter minimum value.
        /// </summary>
        /// <param name="parameterIdentifier">The parameter identifier.</param>
        /// <returns></returns>
        public double GetParameterMinValue(string parameterIdentifier)
        {
            return this.GetAllParameterSamples(parameterIdentifier).DefaultIfEmpty(ParameterMinValue).Min();
        }
        
        /// <summary>
        ///     GetParameterUnit
        /// </summary>
        /// <param name="parameterIdentifier"></param>
        /// <returns></returns>
        public string GetParameterUnit(string parameterIdentifier)
        {
            if (this.ParameterUnits?.Any() != true)
            {
                return string.Empty;
            }

            try
            {
                var unit = this.ParameterUnits[parameterIdentifier];
                return unit;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        ///     GetPrevSample
        /// </summary>
        /// <param name="parameterIdentifier"></param>
        /// <param name="startTime"></param>
        /// <param name="sample"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public DataStatusType GetPrevSample(
            string parameterIdentifier,
            long startTime,
            out double sample,
            out long timestamp)
        {
            return GetSample(parameterIdentifier, startTime, out sample, out timestamp);
        }

        /// <summary>
        ///     GetPrevSamples
        /// </summary>
        /// <param name="parameterIdentifier"></param>
        /// <param name="startTime"></param>
        /// <param name="maximumNumberOfSamples"></param>
        /// <returns></returns>
        public ISamplesDataResult GetPrevSamples(string parameterIdentifier, long startTime, int maximumNumberOfSamples)
        {
            var parameterIdentifierIndex = this.GetParameterIndexInData(parameterIdentifier);
            if (parameterIdentifierIndex < 0)
            {
                return null;
            }

            var startIndex = Math.Max(0, this.GetIndexForTimestamp(startTime) - 1);
            var endIndex = this.GetIndexForTimestamp(this.StartTime);

            var data = new List<double>();
            var timestamps = new List<long>();
            var dataStatusTypes = new List<DataStatusType>();

            var sampleCount = 0;

            for (var i = startIndex; i > endIndex; i--)
            {
                var value = this.values[i][parameterIdentifierIndex];

                if (double.IsNaN(value))
                {
                    continue;
                }

                data.Add(value);
                timestamps.Add(this.GetTimeStamps()[i]);
                dataStatusTypes.Add(DataStatusType.Sample);

                sampleCount++;

                if (sampleCount >= maximumNumberOfSamples)
                {
                    break;
                }
            }

            return new SamplesDataResult(data.ToArray(), dataStatusTypes.ToArray(), timestamps.ToArray(), sampleCount);
        }

        /// <summary>
        ///     GetSamplesForParameter
        /// </summary>
        /// <param name="parameterIdentifier"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public ISamplesDataResult GetSamplesForParameter(string parameterIdentifier, long startTime, long endTime)
        {
            var parameterIdentifierIndex = this.ParameterIdentifiers.IndexOf(parameterIdentifier);
            if (parameterIdentifierIndex < 0)
            {
                return null;
            }

            var startIndex = this.GetIndexForTimestamp(startTime);
            var endIndex = this.GetIndexForTimestamp(endTime);

            var data = new List<double>();
            var loadedTimestamps = new List<long>();
            var dataStatusTypes = new List<DataStatusType>();

            var sampleCount = 0;

            for (var i = startIndex; i < endIndex; i++)
            {
                var value = this.values[i][parameterIdentifierIndex];

                if (double.IsNaN(value))
                {
                    continue;
                }

                var timestampToAdd = this.GetTimeStamps()[i];

                if (timestampToAdd == 0)
                {
                    continue;
                }

                data.Add(value);
                loadedTimestamps.Add(timestampToAdd);
                dataStatusTypes.Add(DataStatusType.Sample);

                sampleCount++;
            }

            if (data.Count == 0)
            {
                double nextSample;
                long nextSampleTime;

                var nextSampleStatus = this.GetNextSample(
                    parameterIdentifier,
                    endTime,
                    out nextSample,
                    out nextSampleTime);

                if (!nextSampleStatus.HasFlag(DataStatusType.Sample))
                {
                    return new SamplesDataResult(
                        data.ToArray(),
                        dataStatusTypes.ToArray(),
                        loadedTimestamps.ToArray(),
                        sampleCount);
                }

                double previousSample;
                long previousSampleTime;
                var previousSampleStatus = this.GetPrevSample(
                    parameterIdentifier,
                    startTime,
                    out previousSample,
                    out previousSampleTime);

                if (!previousSampleStatus.HasFlag(DataStatusType.Sample))
                {
                    return new SamplesDataResult(
                        data.ToArray(),
                        dataStatusTypes.ToArray(),
                        loadedTimestamps.ToArray(),
                        sampleCount);
                }

                data.Add(previousSample);
                loadedTimestamps.Add(previousSampleTime);
                dataStatusTypes.Add(DataStatusType.Sample);
                sampleCount++;

                data.Add(nextSample);
                loadedTimestamps.Add(nextSampleTime);
                dataStatusTypes.Add(DataStatusType.Sample);
                sampleCount++;
            }

            return new SamplesDataResult(
                data.ToArray(),
                dataStatusTypes.ToArray(),
                loadedTimestamps.ToArray(),
                sampleCount);
        }

        /// <summary>
        ///     Gets the session epoch date.
        /// </summary>
        /// <returns></returns>
        public DateTime? GetSessionEpochDate()
        {
            return null;
        }

        /// <summary>
        ///     LoadForData
        /// </summary>
        /// <returns></returns>
        public bool LoadForData()
        {
            if (this.IsDataLoaded)
            {
                return true;
            }

            if (!this.LoadForInfoAndConfig())
            {
                return false;
            }

            var lines = new List<string>();
            using (var fs = new FileStream(
                this.filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    while (sr.Peek() >= 0)
                    {
                        lines.Add(sr.ReadLine());
                    }
                }
            }

            return this.ParseLines(lines);
        }

        private bool ParseLines(List<string> fileLines)
        {
            Tuple<DateTime?, double[]>[] acquiredData;

            try
            {
                var dataLines = this.headerLines > 0 ? fileLines.Skip(this.headerLines) : fileLines;
                this.parametersName = this.parameterNamesLine > 0
                    ? fileLines.Skip(this.parameterNamesLine - 1).First().Split(this.columnDelimiter).Skip(1)
                        .ToArray()
                    : null;
                acquiredData = (from valuesLine in dataLines
                    let values = valuesLine.Split(this.columnDelimiter)
                    let adaptedValues = this.dateExists
                        ? Tuple.Create(values.First(), values.Skip(1))
                        : Tuple.Create((string) null, (IEnumerable<string>) values)
                    select Tuple.Create(
                        this.dateExists ? (DateTime?) this.ParseTime(adaptedValues.Item1) : null,
                        adaptedValues.Item2.Select(double.Parse).ToArray())).ToArray();

            }
            catch (Exception ex)
            {
                return false;
            }

            this.IsDataLoaded = true;

            this.timestamps = acquiredData
                .Select(o => this.SystemToAtlasTime(o.Item1 ?? DateTime.Today)).ToArray();
            this.values = acquiredData.Select(o => o.Item2).ToArray();

            this.Identifier = Path.GetFileNameWithoutExtension(this.filePath);
            this.TimeOfRecording = File.GetCreationTime(this.filePath);
            this.ParameterIdentifiers = this.parametersName;

            var standardTimestamps = this.GetTimeStamps();

            this.StartTime = standardTimestamps[0];
            this.EndTime = standardTimestamps[standardTimestamps.Length - 1];

            this.Laps.Add(new Lap(this.StartTime, 1, 0x00, DefaultLap1Name, true));

            this.ParameterUnits = new Dictionary<string, string>();

            var units = this.unitLine > 0
                ? fileLines[this.unitLine - 1].Split(this.columnDelimiter).Skip(1)
                    .ToList()
                : new List<string>();

            for (var i = 0; i < units.Count; i++)
            {
                this.ParameterUnits.Add(this.ParameterIdentifiers[i], units[i]);
            }

            return true;
        }

        private DateTime ParseTime(string s)
        {
            var milliseconds = double.Parse(s);
            return DateTime.Today.Add(TimeSpan.FromMilliseconds(milliseconds));
        }

        /// <summary>
        ///     LoadForInfoAndConfig
        /// </summary>
        /// <returns></returns>
        public bool LoadForInfoAndConfig()
        {
            this.Identifier = Path.GetFileNameWithoutExtension(this.filePath);
            this.TimeOfRecording = File.GetCreationTime(this.filePath);
            this.StartTime = long.MinValue;
            this.EndTime = long.MaxValue;

            return true;
        }

        private long[] GetTimeStamps()
        {
            return this.timestamps;

        }
        
        /// <summary>
        ///     SystemToAtlasTime
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private long SystemToAtlasTime(DateTime time)
        {
            return (long)time.TimeOfDay.TotalMilliseconds * 1000000;
        }

        private IEnumerable<double> GetAllParameterSamples(string parameterIdentifier)
        {
            var index = this.GetParameterIndexInData(parameterIdentifier);
            return index == -1
                ? Enumerable.Empty<double>()
                : this.values.Select(value => value[index]).Where(v => !double.IsNaN(v));
        }

        private int GetIndexForTimestamp(long timestamp)
        {
            var index = Array.BinarySearch(this.GetTimeStamps(), timestamp);

            if (index < 0)
            {
                index = ~index;
            }

            return index;
        }

        private int GetParameterIndexInData(string parameterIdentifier)
        {
            return this.ParameterIdentifiers.IndexOf(parameterIdentifier);
        }

        private DataStatusType GetSample(string parameterIdentifier, long startTime, out double sample, out long timestamp)
        {
            sample = double.NaN;
            timestamp = 0;

            var parameterIdentifierIndex = this.GetParameterIndexInData(parameterIdentifier);
            if (parameterIdentifierIndex < 0)
            {
                return DataStatusType.Missing;
            }

            var startIndex = this.GetIndexForTimestamp(startTime);
            var endIndex = this.GetIndexForTimestamp(this.EndTime);

            for (var i = startIndex; i < endIndex; i++)
            {
                var value = this.values[i][parameterIdentifierIndex];

                if (double.IsNaN(value))
                {
                    continue;
                }

                sample = value;
                timestamp = this.GetTimeStamps()[i];
                break;
            }

            return !double.IsNaN(sample) ? DataStatusType.Sample : DataStatusType.Missing;
        }
    }
}
