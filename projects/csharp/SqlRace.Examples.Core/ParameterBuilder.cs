using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;

namespace SqlRace.Examples.Core;

/// <summary>
/// Result of building a parameter, containing both the <see cref="Parameter"/> and its channel ID.
/// </summary>
public record BuiltParameter(Parameter Parameter, uint ChannelId);

/// <summary>
/// Fluent builder for creating SQL Race parameter configurations.
/// Encapsulates the boilerplate of creating <see cref="ParameterGroup"/>,
/// <see cref="ApplicationGroup"/>, <see cref="Channel"/>, <see cref="RationalConversion"/>,
/// and <see cref="Parameter"/> objects.
/// </summary>
/// <example>
/// <code>
/// new ParameterBuilder(session)
///     .WithIdentifier("Temperature:Sensors")
///     .WithDescription("Inlet temperature sensor")
///     .InApplicationGroup("Sensors")
///     .InParameterGroup("Thermal")
///     .WithFrequency(100, FrequencyUnit.Hz)
///     .WithDataType(DataType.Double64Bit)
///     .WithConversion("degC", "%5.2f")
///     .WithRange(min: 0, max: 500)
///     .Build();
/// </code>
/// </example>
public class ParameterBuilder
{
    private readonly Session _session;

    private string _identifier = "";
    private string _name = "";
    private string _description = "";
    private string _applicationGroup = "Default";
    private string _parameterGroup = "Default";
    private double _frequency = 100;
    private FrequencyUnit _frequencyUnit = FrequencyUnit.Hz;
    private DataType _dataType = DataType.Double64Bit;
    private string _conversionUnit = "";
    private string _conversionFormat = "%5.2f";
    private double _min;
    private double _max = 1000;

    /// <summary>
    /// Creates a new <see cref="ParameterBuilder"/> for the specified session.
    /// </summary>
    /// <param name="session">The session to add the parameter to.</param>
    public ParameterBuilder(Session session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Sets the parameter identifier in the format <c>"Name:Group"</c>.
    /// Also sets the display name to the portion before the colon, if not explicitly set.
    /// </summary>
    public ParameterBuilder WithIdentifier(string identifier)
    {
        _identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        if (string.IsNullOrEmpty(_name))
        {
            var colonIndex = identifier.IndexOf(':');
            _name = colonIndex > 0 ? identifier[..colonIndex] : identifier;
        }
        return this;
    }

    /// <summary>Sets the display name. If not called, derived from the identifier.</summary>
    public ParameterBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>Sets the parameter description.</summary>
    public ParameterBuilder WithDescription(string description)
    {
        _description = description ?? throw new ArgumentNullException(nameof(description));
        return this;
    }

    /// <summary>Sets the application group name (source system grouping).</summary>
    public ParameterBuilder InApplicationGroup(string group)
    {
        _applicationGroup = group ?? throw new ArgumentNullException(nameof(group));
        return this;
    }

    /// <summary>Sets the parameter group name (logical grouping).</summary>
    public ParameterBuilder InParameterGroup(string group)
    {
        _parameterGroup = group ?? throw new ArgumentNullException(nameof(group));
        return this;
    }

    /// <summary>Sets the sample frequency.</summary>
    public ParameterBuilder WithFrequency(double value, FrequencyUnit unit = FrequencyUnit.Hz)
    {
        _frequency = value;
        _frequencyUnit = unit;
        return this;
    }

    /// <summary>Sets the data type for the channel.</summary>
    public ParameterBuilder WithDataType(DataType dataType)
    {
        _dataType = dataType;
        return this;
    }

    /// <summary>
    /// Sets the engineering unit conversion.
    /// </summary>
    /// <param name="unit">The engineering unit string (e.g., "degC", "bar", "rpm").</param>
    /// <param name="format">The display format string (e.g., "%5.2f").</param>
    public ParameterBuilder WithConversion(string unit, string format = "%5.2f")
    {
        _conversionUnit = unit ?? throw new ArgumentNullException(nameof(unit));
        _conversionFormat = format;
        return this;
    }

    /// <summary>Sets the minimum and maximum display range.</summary>
    public ParameterBuilder WithRange(double min, double max)
    {
        _min = min;
        _max = max;
        return this;
    }

    /// <summary>
    /// Builds the parameter, adding all required configuration objects to the session,
    /// and commits the configuration.
    /// </summary>
    /// <returns>A <see cref="BuiltParameter"/> containing the parameter and its channel ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the identifier has not been set.</exception>
    public BuiltParameter Build()
    {
        if (string.IsNullOrWhiteSpace(_identifier))
            throw new InvalidOperationException("Parameter identifier must be set. Call WithIdentifier() before Build().");

        var config = _session.CreateConfiguration();

        var freq = new Frequency(_frequency, _frequencyUnit);
        var interval = freq.ToInterval();
        var channelId = Guid.NewGuid().GetHashCode() & 0x7FFFFFFF; // Positive ID

        // Conversion (1:1 rational — raw value = engineering value)
        var conversionName = $"{_identifier}_conversion";
        var conversion = RationalConversion.CreateSimple1To1Conversion(conversionName, _conversionUnit, _conversionFormat);
        config.AddConversion(conversion);

        // Channel
        var channel = new Channel((uint)channelId, _name, interval, _dataType, ChannelDataSourceType.Periodic, _identifier, false);
        config.AddChannel(channel);

        // Groups — ParameterGroup must be parented under an ApplicationGroup
        config.AddParameterGroup(new ParameterGroup(_parameterGroup, _parameterGroup));
        var appGroup = new ApplicationGroup(_applicationGroup, _applicationGroup, new List<string> { _parameterGroup }) { SupportsRda = false };
        config.AddGroup(appGroup);

        // Parameter
        var parameterGroups = new List<string> { _parameterGroup };
        var parameter = new Parameter(
            _identifier,
            _name,
            _description,
            _max,
            _min,
            _max,
            _min,
            0.0,
            0u,
            0u,
            conversionName,
            parameterGroups,
            (uint)channelId,
            _applicationGroup);

        config.AddParameter(parameter);
        config.Commit();

        return new BuiltParameter(parameter, (uint)channelId);
    }

    /// <summary>
    /// Builds multiple parameters in a batch with a single configuration commit.
    /// </summary>
    /// <param name="session">The session to add parameters to.</param>
    /// <param name="builders">The configured builders to build.</param>
    /// <returns>A list of created parameters.</returns>
    public static IReadOnlyList<BuiltParameter> BuildBatch(Session session, IEnumerable<ParameterBuilder> builders)
    {
        var config = session.CreateConfiguration();
        var parameters = new List<BuiltParameter>();

        // Collect parameter groups per application group so we can parent them correctly
        var appGroupChildren = new Dictionary<string, HashSet<string>>();
        var builderList = builders.ToList();

        foreach (var builder in builderList)
        {
            if (string.IsNullOrWhiteSpace(builder._identifier))
                throw new InvalidOperationException("All parameter builders must have an identifier set.");

            if (!appGroupChildren.TryGetValue(builder._applicationGroup, out var children))
            {
                children = new HashSet<string>();
                appGroupChildren[builder._applicationGroup] = children;
            }
            children.Add(builder._parameterGroup);
        }

        // Add all parameter groups and application groups up front
        var addedParamGroups = new HashSet<string>();
        foreach (var (appGroupName, childParamGroups) in appGroupChildren)
        {
            foreach (var pg in childParamGroups)
            {
                if (addedParamGroups.Add(pg))
                    config.AddParameterGroup(new ParameterGroup(pg, pg));
            }
            var appGroup = new ApplicationGroup(appGroupName, appGroupName, childParamGroups.ToList()) { SupportsRda = false };
            config.AddGroup(appGroup);
        }

        // Add channels, conversions, and parameters
        foreach (var builder in builderList)
        {
            var freq = new Frequency(builder._frequency, builder._frequencyUnit);
            var interval = freq.ToInterval();
            var channelId = Guid.NewGuid().GetHashCode() & 0x7FFFFFFF;

            var conversionName = $"{builder._identifier}_conversion";
            var conversion = RationalConversion.CreateSimple1To1Conversion(conversionName, builder._conversionUnit, builder._conversionFormat);
            config.AddConversion(conversion);

            var channel = new Channel((uint)channelId, builder._name, interval, builder._dataType, ChannelDataSourceType.Periodic, builder._identifier, false);
            config.AddChannel(channel);

            var parameterGroups = new List<string> { builder._parameterGroup };
            var parameter = new Parameter(
                builder._identifier,
                builder._name,
                builder._description,
                builder._max,
                builder._min,
                builder._max,
                builder._min,
                0.0,
                0u,
                0u,
                conversionName,
                parameterGroups,
                (uint)channelId,
                builder._applicationGroup);

            config.AddParameter(parameter);
            parameters.Add(new BuiltParameter(parameter, (uint)channelId));
        }

        config.Commit();
        return parameters;
    }
}
