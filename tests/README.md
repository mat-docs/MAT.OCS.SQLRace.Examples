# Tests & End-to-End Validation

> Internal developer documentation — not part of the customer-facing example docs.

The examples are validated against **verified inputs**: a deterministic SQL Race session
whose every sample is produced by a closed-form formula. Because the inputs are known, the
correct outputs are known — so the examples are checked for *correctness*, not just "it ran".

## The pieces

| Component | Location | What it does |
|-----------|----------|--------------|
| Fixture generator | [`../tools/FixtureGenerator`](../tools/FixtureGenerator) | Writes a deterministic `.ssn2` from known formulas. `VerifiedFixture` exposes the formulas as constants the tests assert against. |
| Capability tests | [`SqlRace.Examples.Tests/EndToEndExampleTests.cs`](SqlRace.Examples.Tests/EndToEndExampleTests.cs) | Loads the fixture and asserts that load/read/time-range/multi-rate/query operations return the values the formulas imply. |
| Function tests | [`SqlRace.Examples.Tests/FunctionComputationTests.cs`](SqlRace.Examples.Tests/FunctionComputationTests.cs) | Asserts the FunctionLibrary maths (C→F, ratio, rolling average) against hand-derived values. No licence needed. |
| Snippet smoke-runner | [`../scripts/run-examples-e2e.ps1`](../scripts/run-examples-e2e.ps1) | Generates the fixture, runs the actual runnable snippets against it, and checks they exit cleanly and print the expected anchor output. |

## The verified fixture

`VerifiedFixture.Generate(connectionString)` writes one session (`Verified Test Session`,
fixed key `f1c0ffee-0000-4000-8000-000000000001`, start 10:00:00, 10 s long):

| Parameter | Rate | Samples | Formula | Anchors |
|-----------|------|---------|---------|---------|
| `Temperature:Sensors` | 100 Hz | 1000 | `20.0 + 0.1·i` | `[0]=20.0`, `[999]=119.9` |
| `Pressure:Inlet` | 100 Hz | 1000 | `2.0` (constant) | all `2.0` |
| `Speed:Shaft` | 50 Hz | 500 | `0.2·i` | `[0]=0.0`, `[499]=99.8` |

## Running it

> **Requires the SQL Race runtime + a valid licence.** These run locally (or on a licenced
> self-hosted runner). GitHub-hosted CI has no licence, so it builds only and does **not**
> run tests — see [`../.github/workflows/build-and-test.yml`](../.github/workflows/build-and-test.yml).

```powershell
# Capability + function tests
dotnet test SqlRace.Examples.Tests/SqlRace.Examples.Tests.csproj -c Release

# Snippet smoke-run against the fixture
pwsh ../scripts/run-examples-e2e.ps1
```

The function-computation tests need no licence, so they can be run anywhere with
`--filter "FullyQualifiedName~FunctionComputation"`.

## Notes / known limitations (discovered while building this)

- **`samples.Data` is a capacity buffer.** Only indices `[0, SampleCount)` hold valid data;
  reading `Data[Data.Length-1]` (or `Data[^1]`) returns a trailing zero. Index with
  `Data[SampleCount-1]`.
- **Laps in freshly-created SQLite sessions.** A created session always reloads with exactly
  one (default) lap; laps added via `LapCollection.Add` (individually or batched) do not
  survive a reload through this API path. The verified fixture therefore does not rely on
  laps. Pre-existing `.ssn2` files may contain laps written by other tooling.
- **Multi-parameter sessions.** Use `ParameterBuilder.BuildBatch` (single config commit) —
  separate `Build()` calls each commit their own configuration and only the first survives a
  reload.
