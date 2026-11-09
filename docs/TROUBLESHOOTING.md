# Troubleshooting

**The game will not launch from a ZIP preview.** Extract the **entire** portable Windows ZIP into a real folder, then run `ZombieMall.exe`. Do not download GitHub's automatically generated source archive when you want the playable executable.

**There is no executable.** You have the source repository. Download the Windows x64 release asset from GitHub Releases, or install the .NET 10 SDK and build from source with `Build-Windows.bat` on Windows.

**Black screen / startup failure.** Update the graphics driver and make sure OpenGL works. The packaged Windows x64 release is the main target; other operating systems need DesktopGL native libraries. For source diagnostics, run with `--ui-check` only where a graphical display is available.

**Missing music, artwork, or areas.** Do not move the executable out of its folder. `Assets/`, `Campaigns/` and `StylePacks/` must be retained with the game. The sound check (`--audio-check`) is available to source builders.

**No sound.** Check in-game settings/mute, OS volume and the three soundtrack WAVs in `Assets/`. Some systems may not initialize the sound device.

**Saved game missing / old save lacks new locations.** On Windows saves and preferences are stored under `%LOCALAPPDATA%\ZombieMall`. Older saves preserve their embedded campaign definitions. Start a fresh campaign for the complete latest map and finales if the save predates them; the 0.33.2 art update itself needs no reset.

**NuGet/.NET error while building.** Install the .NET **10 SDK**, then run `dotnet restore ZombieMall.sln` followed by `dotnet build ZombieMall.sln -c Release`. First restore requires access to NuGet.

**Reporting a crash.** Check `%LOCALAPPDATA%\ZombieMall\crash.log`. Share version, OS, exact reproduction steps, and a *sanitized* excerpt if appropriate. Remove private paths, local usernames and other personal data before posting public issues.
