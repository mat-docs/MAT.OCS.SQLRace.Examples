# Session Cropper

Reads an `.ssn2` file, displays session timing info, and crops it to a new time range — saving the result as a new `.ssn2` file.

## Usage

```bash
# Interactive — prompts for start/end times
dotnet run -- path/to/session.ssn2

# Non-interactive — specify crop range in seconds from session start
dotnet run -- path/to/session.ssn2 --start 5 --end 30
```

## What It Does

1. Opens the source `.ssn2` and loads the most recent session
2. Displays start time, end time, duration, and parameter count
3. Prompts for (or accepts via CLI args) a crop start and end in seconds
4. Creates a new `_cropped.ssn2` file alongside the source
5. Copies all parameter configuration (conversions, channels, groups, parameters)
6. Copies only the sample data within the crop window
7. Copies laps/segments, markers, session items, and constants that fall within the window

## Output

The cropped file is saved as `<original>_cropped.ssn2` in the same directory. If that file already exists, it appends a counter (`_cropped_1.ssn2`, etc.).

## Build

```bash
cd projects/csharp
dotnet build SessionCropper/SessionCropper.csproj
```
