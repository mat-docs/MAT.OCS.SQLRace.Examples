# Cross-Industry Concept Mapping

The SQL Race API uses a set of core concepts for organising time-series data. These concepts have direct equivalents in every industry that uses the platform. This guide maps SQL Race terminology to domain-specific language so you can read the examples in terms you already know.

## Core Concepts

| SQL Race Concept | Description | Motorsport | Aerospace | Energy | Automotive | Industrial / Manufacturing |
|-----------------|-------------|-----------|-----------|--------|------------|---------------------------|
| **Session** | A contiguous recording of time-series data | Race, Practice, Qualifying | Flight, Sortie, Ground Run | Test Run, Commissioning Run | Drive Cycle, Durability Run | Production Batch, Test Cycle |
| **Lap** | A subdivision of a session at defined trigger points | Lap (track circuit) | Test Point, Manoeuvre | Operating Regime, Load Step | Route Segment, Phase | Machine Cycle, Batch Step |
| **Parameter** | A named data channel with a sample rate and unit | Channel (vCar, nEngine) | Measurement (ALT, IAS) | Tag (P101, T201) | Signal (VehicleSpeed) | Process Variable (PV) |
| **Parameter Group** | Logical grouping of related parameters | System (Chassis, Engine) | Subsystem (Flight Controls) | Loop Group (Turbine) | Domain (Powertrain) | Unit Area (Reactor 1) |
| **Application Group** | Source-system grouping for parameters | ECU / Data Source | Avionics Bus | DCS / SCADA | CAN Bus / ECU | PLC / Controller |
| **Event** | A discrete occurrence with a timestamp and priority | Flag, Incident, Pit Stop | Caution, Alert, Exceedance | Alarm, Trip, Setpoint Change | DTC, Warning | Fault, Alarm, Interlock |
| **Marker** | A labelled time range within a session | Stint, Sector | Flight Phase, Test Card | Steady State Window | Test Phase | Process Stage |
| **Constant** | A scalar value that applies to the whole session | Car Number, Driver | Tail Number, Pilot | Unit ID, Test Rig | VIN, Driver ID | Serial Number, Batch ID |
| **Session Data Item** | Key-value metadata attached to a session | Circuit, Weather | Airfield, Sortie Type | Plant, Test Objective | Proving Ground, Test Type | Facility, Work Order |
| **Configuration** | Schema definition for parameters, channels, conversions | Config Set | ICD (Interface Control Document) | Point List | DBC / Signal Database | Tag Database |

## Time Model

| Concept | Description | Typical Values |
|---------|-------------|----------------|
| **Timestamp** | Nanoseconds since midnight (00:00:00.000) on the session date | `long` — e.g., `36000000000000` = 10 hours |
| **Frequency** | Sample rate of a channel | 1 Hz (slow telemetry) to 20,000 Hz (vibration) |
| **Interval** | Time between samples in nanoseconds (`1 / frequency`) | 1 Hz = 1,000,000,000 ns; 100 Hz = 10,000,000 ns |

### Converting between time representations

```
seconds since midnight  →  nanoseconds:    seconds * 1_000_000_000
hours:minutes:seconds   →  nanoseconds:    (h*3600 + m*60 + s) * 1_000_000_000
nanoseconds             →  seconds:        nanoseconds / 1_000_000_000.0
```

## Data Types

| SQL Race DataType | .NET Type | Size | Typical Use |
|-------------------|-----------|------|-------------|
| `Double64Bit` | `double` | 8 bytes | High-precision measurements (temperature, pressure) |
| `FloatingPoint32Bit` | `float` | 4 bytes | Standard measurements (speed, voltage) |
| `Signed16Bit` | `short` | 2 bytes | Raw ADC values, compact storage |
| `Unsigned8Bit` | `byte` | 1 byte | Status flags, digital I/O |

## Session Types

| Type | When to use |
|------|-------------|
| **File Session** (`.ssn2`) | Offline analysis of recorded data. Self-contained SQLite database. |
| **Database Session** | Live or historical data on a SQL Server instance. Multi-user access. |
| **Composite Session** | Virtual session combining multiple sessions for comparison or aggregation. |

## Parameter Naming Convention

This repository uses **domain-neutral** parameter names in all examples:

```
{Measurement}:{ApplicationGroup}
```

Examples:
- `Temperature:Sensors` — a temperature reading from a sensor group
- `Pressure:Inlet` — inlet pressure measurement
- `Speed:Shaft` — shaft rotational speed
- `Acceleration:Structure` — structural acceleration (vibration)
- `Voltage:Battery` — battery voltage
- `FlowRate:Fuel` — fuel flow rate

In your own code, use names that match your domain — the API imposes no naming restrictions.
