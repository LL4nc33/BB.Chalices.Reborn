using BB.Chalices.Services;

namespace BB.Chalices.Services.Tests;

public class AppPathsTests
{
    // Older versions persisted the profile backup folder as an absolute path;
    // ConfigService uses this to treat that stale value as "unset" so backups
    // follow the portable data folder. A folder the user chose stays honoured.
    [Fact]
    public void IsLegacyBackupDefault_MatchesTheProfileFolderNotACustomOne()
    {
        Assert.True(AppPaths.IsLegacyBackupDefault(AppPaths.ProfileBackupsDirectory));
        // trailing separator / non-canonical form still matches
        Assert.True(AppPaths.IsLegacyBackupDefault(
            AppPaths.ProfileBackupsDirectory + Path.DirectorySeparatorChar));

        Assert.False(AppPaths.IsLegacyBackupDefault(Path.Combine(Path.GetTempPath(), "my-chalice-backups")));
        Assert.False(AppPaths.IsLegacyBackupDefault(null));
        Assert.False(AppPaths.IsLegacyBackupDefault(""));
        Assert.False(AppPaths.IsLegacyBackupDefault("   "));
    }
}
