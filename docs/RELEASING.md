# GitHub publishing checklist

The repository and its Windows archive are prepared for public distribution, but **no GitHub repository or release was created by this package**.

1. Create a public GitHub repository (suggested name: `ZombieMall`) and upload **the contents of the source ZIP** to its default branch. Do not upload the Windows binary into Git history; attach it to a release instead.
2. Add the repository description: **A party-based zombie survival RPG set in a vast, explorable shopping mall. Built with C# and MonoGame.**
3. Suggested topics: `zombie`, `survival-rpg`, `rpg`, `monogame`, `csharp`, `dotnet`, `indie-game`, `windows`.
4. Set the repository website field if you have one. Turn on GitHub Issues and optionally Discussions. Enable private vulnerability reporting if appropriate.
5. Consider using `docs/screenshots/phase5-overview.png` as the GitHub social-preview image (it is from an earlier playable build). Verify the **Build and regression checks** workflow succeeds on Windows and Linux. Run the Windows portable executable manually on a real Windows x64 machine, testing title, new campaign, loading, audio, and access to the 170-area content.
6. Create an annotated tag: `git tag -a v0.33.2 -m "Zombie Mall 0.33.2"` then `git push origin v0.33.2`. The release workflow runs the tests, publishes Windows x64, prepares the cleaned folder and attaches a ZIP and SHA-256 checksum to a GitHub Release.
7. Use [`RELEASE_NOTES.md`](../RELEASE_NOTES.md) as the release body. The source ZIP is the repository, not the Windows ZIP. If the automated workflow succeeds, use its built ZIP rather than attaching a second duplicate.
8. For manual releases, you can upload the supplied cleaned Windows ZIP to the GitHub Release assets. Use the versioned file name and the checksum file supplied with it.

**Before publishing:** Check that you have rights to distribute all bundled work and that upstream third-party notices remain accurate. The original project license is CC BY-SA 4.0 and is **not** replaced with MIT; individual Playroma components have their own MIT terms.

## Release assets

- `ZombieMall-win-x64-v0.33.2.zip` — complete portable game; contains the `ZombieMall.exe` and required folders.
- `ZombieMall-win-x64-v0.33.2.zip.sha256` — checksum to verify transfer.
- The GitHub auto-generated `Source code (zip)` is for builders, not players.

No CI success claims are made until the actual workflow executes on GitHub; the provided ZIP is verified for file integrity and asset layout only.
