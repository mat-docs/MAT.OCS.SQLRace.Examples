using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;
using MESL.SqlRace.Enumerators;
using MESL.SqlRace.Functions.Interfaces.Enums;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace MAT.SqlRace.Functions.HelloDotNet
{
    [Export(typeof(IDotNetFunction))]
    [Serializable]
    public class HelloFunction : IDotNetFunction
    {
        internal const string FunctionName = "HelloDotNet";
        internal const string ParameterName = "vCarDoubledParam";
        internal const string ApplicationName = "DotNetGroup";
        internal const string ParameterIdentifier = ParameterName + ":" + ApplicationName;

        public string Name => FunctionName;

        /// <summary>
        ///     This method will be called by SqlRace framework.
        /// </summary>
        /// <param name="functionManager"></param>
        public void Initialize(IFunctionManager functionManager)
        {
            // create the function definition
            var functionDefinition = functionManager.CreateFunctionDefinition(DotNetFunctionConstants.UniqueId, FunctionMode.Instantaneous, CalculationModeInfoDefinition.EachSamplePoint());
            //var functionDefinition = functionManager.CreateFunctionDefinition(DotNetFunctionConstants.UniqueId);
            functionDefinition.CalculateOverWholeSession = false;
            functionDefinition.InterpolateBetweenSamples = false;
            functionDefinition.JoinGapsAroundNull = false;
            functionDefinition.Name = FunctionName;
            functionDefinition.StoreInSession = false;

            // set the implementation 
            var implementationDefinition = (DotNetFunctionImplementationDefinition)functionDefinition.ImplementationDefinition;
            implementationDefinition.Function = this;

            // create the single output parameter
            functionDefinition.OutputParameterDefinitions.Add(
                new FunctionOutputParameterDefinition
                {
                    ApplicationName = ApplicationName,
                    ByteOrder = ByteOrderType.BigEndian,
                    Description = "HelloDotNet.vCarDoubled",
                    FormatOverride = @"%5.2f",
                    Name = ParameterName,
                    Units = "km/h",
                    MaximumValue = "800",
                    MinimumValue = "0",
                    ShowInBrowser = true
                });

            functionDefinition.InputParameterIdentifiers.Add("vCar:Chassis");

            // build the function
            var buildResults = functionManager.Build(functionDefinition);

            // make sure we have no build errors
            if (buildResults.Errors.Count > 0)
            {
                Console.WriteLine($"Error building DotNet function '{FunctionName}': {buildResults.Errors.FirstOrDefault()?.ErrorText}.");
            }
            else
            {
                Console.WriteLine($"DotNet function '{FunctionName}' created successfully.");
            }
        }

        /// <summary>
        ///     This method is called once per data request.
        /// </summary>
        /// <param name="executionContext"></param>
        public void Execute(IExecutionContext executionContext)
        {
            var timestamps = executionContext.FunctionInput.Timestamps;
            var timestampsCount = timestamps.Length;

            var index = executionContext.FunctionOutput.OutputParameterIndexes[ParameterIdentifier];
            var parameterOutput = executionContext.FunctionOutput.OutputParametersValues[index];
            var input = executionContext.FunctionInput.Values[index];

            for (int i = 0; i < timestampsCount; i++)
            {
                parameterOutput[i] = input[i] * 2;
                //Console.WriteLine($"{data.Timestamp[i]},{new DateTime(data.Timestamp[i] / 100):HH:mm:ss.fff},{data.Data[i]} = {result[i]}");
            }
        }
    }
}
