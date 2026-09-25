# TorrentHelper

Console utility for a **local TV torrent workflow**: move finished downloads, optionally rename episodes using [TheTVDB](https://thetvdb.com/) metadata, and transcode video to **MP4** with **HandBrake CLI**.

Repository: [github.com/jo15765/TorrentHelper](https://github.com/jo15765/TorrentHelper)

![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6.1-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)

---

## What this project does (overview)

TorrentHelper automates three steps that you can turn on or off in `Program.cs`:

| Step | Method | Default in `Main` | Purpose |
|------|--------|-------------------|---------|
| 1 | `MoveFiles()` | Commented out | Move video files from an **in-progress** folder to a **completed** folder |
| 2 | `GetTorrentInfo()` | Commented out | Clean release names, look up show/season/episode on TVDB, **rename** files with episode titles |
| 3 | `ConvertToMP4()` | **Active** | Run **HandBrakeCLI** on each video and write **`.mp4`** files to an output folder |

Supported video extensions (all steps): **`.avi`**, **`.mp4`**, **`.m4v`**, **`.mkv`**, **`.mod`**

---

## Requirements

### Software

- **Windows**
- **.NET Framework 4.6.1** (or later)
- **Visual Studio 2017+** (optional, for building from source)
- **HandBrake CLI** (required for `ConvertToMP4`)
  - Default path in code: `C:\Program Files\HandBrake CLI\HandBrakeCLI.exe`
  - [HandBrake downloads](https://handbrake.fr/downloads.php) — install the CLI build
- **TheTVDB API key** (required only if you enable `GetTorrentInfo()`)
  - Register at [TheTVDB](https://thetvdb.com/) and replace the key in `Program.cs` (do not commit real keys to public repos)

### NuGet dependency

- **TVDBSharp 2.1.0** (included under `packages/` in this repo)

---

## Project structure

```text
TorrentHelper/
├── TorrentHelper.sln
├── TorrentHelper/
│   ├── Program.cs           # All workflow logic and paths
│   ├── App.config
│   ├── TorrentHelper.csproj
│   └── bin/Debug/           # Built exe (after compile)
└── packages/
    └── TVDBSharp.2.1.0/
```

---

## Step 1 — Configure folder paths

Open `TorrentHelper/Program.cs` and set these constants to match your machine:

```csharp
private const string scandir   = "D:\\__InProgressTorrents\\";
private const string movedir   = "D:\\__CompletedTorrents\\";
private const string OutputPath = "D:\\__MoveToServer\\";
```

| Constant | Used by | Meaning |
|----------|---------|---------|
| `scandir` | `MoveFiles()` | Where torrent clients drop **in-progress** or finished downloads you want to pick up |
| `movedir` | `MoveFiles()`, `GetTorrentInfo()`, `ConvertToMP4()` | Staging folder for **completed** videos before rename/transcode |
| `OutputPath` | `ConvertToMP4()` | Destination for converted **`.mp4`** files |

Create these folders on disk before running the app.

---

## Step 2 — Configure HandBrake (transcode step)

Still in `Program.cs`:

```csharp
const string HandBrakeLocation = "C:\\Program Files\\HandBrake CLI\\HandBrakeCLI.exe";
const string HandBrakeCommandLine = @"-i ""{in}"" -o ""{out}"" --preset=""Fast 1080p30""";
```

1. Install HandBrake CLI if it is not already installed.
2. Update `HandBrakeLocation` if your install path differs.
3. Change `--preset=` to any preset name your HandBrake build supports.

The converter:

- Reads each matching file in **`movedir`**
- Writes **`OutputPath\<same basename>.mp4`**
- Skips output files that **already exist**
- Preserves creation and last-write timestamps on the new file

---

## Step 3 — Choose which workflow to run

In `Program.Main`, uncomment the calls you need:

```csharp
public static void Main(string[] args)
{
    // MoveFiles();        // Step A: in-progress → completed
    // GetTorrentInfo();   // Step B: TVDB rename
    ConvertToMP4();        // Step C: HandBrake → MP4 (default)

    // Optional: delete source MKV after successful convert (not implemented in repo — add if desired)
}
```

Recommended order for a full pipeline:

1. `MoveFiles();`
2. `GetTorrentInfo();`
3. `ConvertToMP4();`

---

## Step 4 — Build the application

### Option A: Visual Studio

1. Clone the repo:

   ```bash
   git clone https://github.com/jo15765/TorrentHelper.git
   cd TorrentHelper
   ```

2. Open **`TorrentHelper.sln`**
3. Restore NuGet packages if prompted (TVDBSharp is under `packages/`)
4. Build **Debug** or **Release**
5. Run `TorrentHelper\bin\Debug\TorrentHelper.exe` (or Release)

### Option B: MSBuild

From **Developer Command Prompt for VS**:

```cmd
msbuild TorrentHelper.sln /p:Configuration=Release
```

---

## Step 5 — Run each workflow (detailed)

### Step A — `MoveFiles()`

1. Put completed torrent videos under **`scandir`** (searched **recursively**).
2. Run the app with `MoveFiles()` enabled.
3. Each matching file is **moved** (not copied) to **`movedir`** with the same file name.

### Step B — `GetTorrentInfo()` (TVDB rename)

Expects release-style names in **`movedir`**, for example dotted names with **`SxxExxx`** near the end.

For each video file, the tool:

1. **`CleanFileName`** — strips common release tokens (`720p`, `x264`, group tags, etc.; see `charstoremove` in code).
2. Parses **show name**, **season**, and **episode** from the file name.
3. **`ShowNameUpdater`** — maps ambiguous names (e.g. `The Flash` → `The Flash (2014)`) for better TVDB matches.
4. Calls **TVDBSharp** `Search` and finds the episode **title**.
5. Renames the file to:

   ```text
   {Show Name} - {Season}x{Episode} - {Episode Title}{extension}
   ```

Example shape:

```text
The Goldbergs - 1x5 - Mall-mentary.mkv
```

**Important:** Set your own TVDB API key in `QueryTVDBAPI` before using this step in production.

### Step C — `ConvertToMP4()` (default)

1. Place videos in **`movedir`** (or run after Steps A and B).
2. Run with `ConvertToMP4()` enabled.
3. For each file:
   - HandBrake runs with preset **Fast 1080p30**
   - Output: **`OutputPath\<filename>.mp4`**
4. Console prints `Conversion completed!!` and waits for **Enter** (`Console.ReadLine()`).

---

## Troubleshooting

| Problem | Things to check |
|---------|------------------|
| HandBrake never starts | Path to `HandBrakeCLI.exe`, preset name, antivirus blocking |
| No files processed | Extension must be in `MeetsCriteria`; files must be in the correct folder (`scandir` vs `movedir`) |
| TVDB rename fails | API key, show name mapping in `ShowNameUpdater`, release name must contain parseable `SxxExxx` |
| Output skipped | Target `.mp4` already exists in `OutputPath` |
| Wrong drive letters | All paths are hardcoded — edit constants in `Program.cs` |

---

## Security and maintenance notes

- **Paths and API keys are hardcoded** in `Program.cs`. For sharing or public repos, move secrets and paths to `App.config` or user settings and use placeholders in git.
- The repo includes **`bin/`** and **`obj/`** build output. For cleaner git history, consider adding a `.gitignore` and building locally.
- Only use TorrentHelper on content you **have the right to download and transcode**.

---

## License

Add a `LICENSE` file when you choose a license (for example MIT). Until then, default copyright applies.

---

## Author

**jo15765** — [TorrentHelper on GitHub](https://github.com/jo15765/TorrentHelper)
