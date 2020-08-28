using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;
using MESL.SqlRace.Enumerators;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace MAT.SqlRace.Functions.HelloDotNet
{
    [Export(typeof(IDotNetFunction))]
    [Serializable]
    public class HelloPdaCountFunction : IDotNetFunction
    {
        internal const string FunctionName = "HelloWithPdaDotNet";
        internal const string ParameterName = "vCarPdaCountParam";  //the name that appears in parameter browser
        internal const string ApplicationName = "DotNetGroup";
        internal const string ParameterIdentifier = ParameterName + ":" + ApplicationName;

        [NonSerialized]
        private ParameterDataAccessBase parameterDataAccess;

        public string Name => FunctionName;

        /// <summary>
        /// API consumer must call this method explicitly after instantiating IFunctionManager; FunctionManagerFactory.Create();
        /// </summary>
        /// <param name="functionManager"></param>
        public void Initialize(IFunctionManager functionManager)
        {
            // create the function definition
            var functionDefinition = functionManager.CreateFunctionDefinition(DotNetFunctionConstants.UniqueId);
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
                    Description = "HelloDotNet.vCarPdaCountParam",
                    FormatOverride = @"%5.2f",
                    Name = ParameterName,
                    Units = "",
                    MaximumValue = "1000000",
                    MinimumValue = "0",
                    ShowInBrowser = true
                });

            //functionDefinition.InputParameterIdentifiers.Add("vCar:Chassis");

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
        /// 
        /// </summary>
        /// <param name="executionContext"></param>
        public void Execute(IExecutionContext executionContext)
        {
            var timestamps = executionContext.FunctionInput.Timestamps;
            var timestampsCount = timestamps.Length;

            var index = executionContext.FunctionOutput.OutputParameterIndexes[ParameterIdentifier];
            var parameterOutput = executionContext.FunctionOutput.OutputParametersValues[index];
            var input = executionContext.FunctionInput.Values[index];

            if (parameterDataAccess == null)
            {
                this.parameterDataAccess = executionContext.Session.CreateParameterDataAccess("vCar:Chassis");
            }

            var count = parameterDataAccess.GetSamplesCount(timestamps.FirstOrDefault(), timestamps.LastOrDefault());

            for (int i = 0; i < timestampsCount; i++)
            {
                parameterOutput[i] = count;
            }
        }
    }
}
