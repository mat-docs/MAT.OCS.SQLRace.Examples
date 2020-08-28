// <copyright file="Functionhelper.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

using System;

using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.Fdl;
using MESL.SqlRace.Enumerators;
using MESL.SqlRace.Functions.Interfaces.Enums;

namespace MAT.SQLRace.HelloData
{
    internal class FunctionHelper
    {
        public static IFunctionDefinition CreateFdlFunctionDefinition(IFunctionManager functionManager, FunctionMode functionMode = FunctionMode.Instantaneous)
        {
            //Create function
            const string CarSpeedDoubledInFdl = "return ($vCar:Chassis * 2)";

            var functionDefinition = functionManager.CreateFunctionDefinition(FdlFunctionConstants.UniqueId);

            // The implementation of a function is completely agnostic. We requested a function definition from
            // the FDL runtime, so the generic function implementation definition in the definition returned can
            // be safely cast to the more specific FDL implementation definition.
            var fdlFunctionImplementationDefinition = (IFdlFunctionImplementationDefinition)functionDefinition.ImplementationDefinition;
            fdlFunctionImplementationDefinition.FunctionCode = CarSpeedDoubledInFdl;

            // FDL doesn't define the output for a single parameter - it's simply the return value of the
            // function - so we have to tell the function executor what to do with the output it generates
            var outputParameter = FunctionOutputParameterDefinition
                .Create("vCarDoubled", "FunctionParameters", "Double car speed!")
                .Units("kph")
                .FormatOverride("%.2f")
                .MinimumValue("0.0")
                .MaximumValue("700.0");

            functionDefinition.OutputParameterDefinitions.Add(outputParameter);

            // Functions in ATLAS 9 have a number of different properties that define how, where, when and on
            // what a function executes. ATLAS 10 will add more of these properties. Each value here is the
            // default for its property, shown for illustration.
            functionDefinition.Name = $"DoubleCarSpeed_{Environment.TickCount}";
            functionDefinition.FunctionMode = functionMode;
            functionDefinition.CalculationModeInfoDefinition.Mode = CalculationMode.EachSamplePoint;
            functionDefinition.InterpolateBetweenSamples = false;
            functionDefinition.JoinGapsAroundNull = true;
            functionDefinition.CalculateOverWholeSession = false;
            functionDefinition.StoreInSession = false;
            functionDefinition.ShouldHide = false;
            functionDefinition.ShouldPersist = false;

            return functionDefinition;
        }
    }
}