// <copyright file="SessionHelper.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

using System;
using System.Collections.Generic;

using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;

namespace MAT.SQLRace.HelloCreateSSN2FromZeroWithParameters
{
    internal class SessionHelper
    {
        public static Dictionary<string, uint> CreateSessionConfigurationForMultipleParameter(
            IClientSession clientSession,
            IEnumerable<string> identifiers)
        {
            var channels = new Dictionary<string, uint>();

            const string ConversionFunctionName = "CONV_MyParam:MyApp";
            const string ApplicationGroupName = "MyApp";
            const string ParameterGroupIdentifier = "MyParamGroup";
            const string ParameterName = "MyParam";

            var session = clientSession.Session;

            var config = session.CreateConfiguration();

            var group1 = new ParameterGroup(ParameterGroupIdentifier, "pg1_description");

            var applicationGroup1 = new ApplicationGroup(
                ApplicationGroupName,
                ApplicationGroupName,
                new List<string>
                {
                    group1.Identifier
                })
            {
                SupportsRda = false
            };

            config.AddGroup(applicationGroup1);
            config.AddParameterGroup(group1);

            config.AddConversion(
                new RationalConversion(ConversionFunctionName, "kph", "%5.2f", 0.0, 1.0, 0.0, 0.0, 0.0, 1.0));

            var vCarFrequency = new Frequency(2, FrequencyUnit.Hz);

            uint channelId = 1;
            foreach (var identifier in identifiers)
            {
                channels[identifier] = channelId;

                var channel = new Channel(
                    channelId++,
                    "Channel" + channelId,
                    vCarFrequency.ToInterval(),
                    DataType.Signed16Bit,
                    ChannelDataSourceType.Periodic);

                var parameter = new Parameter(
                    identifier,
                    identifier,
                    identifier,
                    400, //vCar maximum value
                    0, //vCar minimum value
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

        public static Parameter CreateSessionConfigurationForOneParameter(IClientSession clientSession)
        {
            const string ConversionFunctionName = "CONV_MyParam:MyApp";
            const string ApplicationGroupName = "MyApp";
            const string ParameterGroupIdentifier = "MyParamGroup";
            const string ParameterName = "MyParam";
            const uint MyParamChannelId = 999998;   //must be unique
            const int ApplicationId = 998;
            var parameterIdentifier = $"{ParameterName}:{ApplicationGroupName}";

            var session = clientSession.Session;

            var config = session.CreateConfiguration();

            var group1 = new ParameterGroup(ParameterGroupIdentifier, "pg1_description");
            
            var applicationGroup1 = new ApplicationGroup(
                ApplicationGroupName,
                ApplicationGroupName,
                ApplicationId,
                new List<string>
                {
                    group1.Identifier
                })
            {
                SupportsRda = false
            };

            config.AddGroup(applicationGroup1);
            config.AddParameterGroup(group1);

            config.AddConversion(
                new RationalConversion(ConversionFunctionName, "kph", "%5.2f", 0.0, 1.0, 0.0, 0.0, 0.0, 1.0));

            var myParamFrequency = new Frequency(2, FrequencyUnit.Hz);

            var myParameterChannel = new Channel(
                MyParamChannelId,
                "MyParamChannel",
                myParamFrequency.ToInterval(),
                DataType.Signed16Bit,
                ChannelDataSourceType.Periodic);

            var myParameter = new Parameter(
                parameterIdentifier,
                ParameterName,
                ParameterName + "Description",
                400,
                0,
                1,
                0,
                0,
                0xFFFF,
                0,
                ConversionFunctionName,
                new List<string>
                {
                    ParameterGroupIdentifier
                },
                myParameterChannel.Id,
                ApplicationGroupName);

            config.AddChannel(myParameterChannel);
            config.AddParameter(myParameter);

            config.Commit();

            return myParameter;
        }

        public static Parameter CreateTransientConfigurationForOneParameter(IClientSession clientSession)
        {
            const string ConversionFunctionName = "CONV_MyParam:MyApp";
            const string ApplicationGroupName = "MyApp";
            const string ParameterGroupIdentifier = "MyParamGroup";
            const string ParameterName = "MyParam";
            const uint MyParamChannelId = 999999;   //must be unique
            const int ApplicationId = 999;
            var parameterIdentifier = $"{ParameterName}:{ApplicationGroupName}";

            var session = clientSession.Session;

            var transientConfigSet = session.CreateTransientConfiguration();

            var parameterGroup = new ParameterGroup(ParameterGroupIdentifier, "some description!");
            transientConfigSet.AddParameterGroup(parameterGroup);

            var applicationGroup = new ApplicationGroup(
                ApplicationGroupName,
                ApplicationGroupName + "App Group Desc!!",
                ApplicationId,
                new List<string>
                {
                    parameterGroup.Identifier
                });
            transientConfigSet.AddGroup(applicationGroup);

            var conversion = RationalConversion.CreateSimple1To1Conversion(ConversionFunctionName, "myunit", "%5.2f");
            transientConfigSet.AddConversion(conversion);

            var paramFrequency = new Frequency(2, FrequencyUnit.Hz);

            var paramChannel = new Channel(
                MyParamChannelId,
                "MyParamChannel",
                paramFrequency.ToInterval(),
                DataType.Double64Bit,
                ChannelDataSourceType.Periodic,
                string.Empty, 
                true);

            transientConfigSet.AddChannel(paramChannel);

            var myParameter = new Parameter(
                parameterIdentifier,
                ParameterName,
                ParameterName + "Description",
                400, //maximum value
                0, //minimum value
                1,
                0,
                0,
                0xFFFF,
                0,
                ConversionFunctionName,
                new List<string>
                {
                    ParameterGroupIdentifier
                },
                paramChannel.Id,
                ApplicationGroupName);

            transientConfigSet.AddParameter(myParameter);

            transientConfigSet.Commit();

            return myParameter;
        }
    }
}