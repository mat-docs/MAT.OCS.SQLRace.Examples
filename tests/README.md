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
| Snippet compilation | [`SqlRace.Examples.Tests/SnippetCompilationTests.cs`](SqlRace.Examples.Tests/SnippetCompilationTests.cs) | Compiles every `snippets/csharp/**.cs` against the real SQL Race assemblies with Roslyn. Metadata-only, so **no licence needed** — the one suite GitHub CI can run. |
| Snippet runner | [`../scripts/run-examples-e2e.ps1`](../scripts/run-examples-e2e.ps1) | Runs the runnable C#, Python and MATLAB snippets against the fixture and compares their output against golden files in [`golden/`](golden). |
| Snippet manifest | [`../scripts/snippet-manifest.psd1`](../scripts/snippet-manifest.psd1) | Which snippets run, what arguments they take, and why the rest are skipped. |

## Breaking-change early warning

These pieces exist to answer one question before a release ships: *will the new
`MESL.SQLRace.API` break code customers already have?* Three layers, each catching something
the previous one cannot.

| Layer | Catches | Licence? |
|-------|---------|----------|
| `SnippetCompilationTests` | A snippet that no longer compiles — the same errors a customer would hit | No |
| `run-examples-e2e.ps1` golden comparison | A snippet that still compiles and runs but now returns different data | Yes |
| Its Python and MATLAB legs | Renames and signature changes a C# compile would catch but which fail silently for `pythonnet` and `NET.addAssembly` callers | Yes |

To validate a candidate build before it is published, pin the version:

```powershell
dotnet test SqlRace.Examples.Tests/SqlRace.Examples.Tests.csproj -p:SqlRaceApiVersion=2.1.26212.6-ci
../scripts/run-examples-e2e.ps1 -SqlRaceApiVersion 2.1.26212.6-ci
```

The SQL Race pipeline runs both against every nightly build — see
`Build/generic-steps/run-consumer-compat-tests.yml` in `MAT.OCS.SQLRace.API`. That is the
counterpart to its `check-api-compatibility.yml`, which diffs the public API surface against
the last published package.

### Golden output

`run-examples-e2e.ps1` compares each snippet's stdout against `golden/<language>/<name>.txt`.
Values that legitimately vary between runs — GUIDs, timestamps, absolute paths, elapsed times
— are normalised before comparison, so what is actually asserted is the data: parameter
names, sample counts, computed values.

After an intended change, regenerate and **review the diff** — `-UpdateGolden` will happily
bake in a regression:

```powershell
../scripts/run-examples-e2e.ps1 -UpdateGolden
```

A snippet that runs but has no golden file is reported as `NOGOLD`, not as a pass: an absent
golden means nothing was verified. Skipped snippets are listed in the summary for the same
reason — a snippet silently missing from the run would otherwise be indistinguishable from
one that passed.

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

# All three languages against the fixture
../scripts/run-examples-e2e.ps1

# One language, or one snippet
../scripts/run-examples-e2e.ps1 -Language Python
../scripts/run-examples-e2e.ps1 -Filter '*read*'
```

The function-computation and snippet-compilation tests need no licence, so they can be run
anywhere with `--filter "FullyQualifiedName~FunctionComputation"` or
`--filter "FullyQualifiedName~SnippetCompilation"`.

### Prerequisites for the Python and MATLAB legs

- **`pip install pythonnet cffi`.** `cffi` is a `clr_loader` dependency that does not always
  come in; without it every Python snippet dies in `pythonnet.load()` with
  `Failed to create a .NET runtime (coreclr)`, which looks like a SQL Race problem and is not.
- **A licensed MATLAB on `PATH`** (`matlab -batch` is used). Tested against R2025b.
- **Restore needs feed credentials.** `MESL.SQLRace.API` is referenced as `Version="*"`, and
  NuGet cannot resolve a floating version without reaching the feed. Set `NUGET_AUTH_USERNAME`
  and `NUGET_AUTH_TOKEN` (see `.env.example`), or pass `-SqlRaceApiVersion` to pin a version
  already in the local NuGet cache.

The script points `SQLRACE_DLL_PATH` at a *flattened* copy of the build output rather than at
the output folder itself. A NuGet-restored build keeps platform-specific assemblies under
`runtimes/<rid>/` and relies on `deps.json` to select them; Python and MATLAB host the CLR
themselves, so there is no `deps.json` and the loader picks up the cross-platform placeholder
`System.Data.SqlClient` (which throws `PlatformNotSupportedException`) and cannot find
`SQLite.Interop.dll` at all. **Customers pointing pythonnet or MATLAB at a NuGet-restored SQL
Race hit exactly this**; those pointing at an ATLAS installation do not, because that
directory is already flat.

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
