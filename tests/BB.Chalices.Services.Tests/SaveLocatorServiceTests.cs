using BB.Chalices.Services;

namespace BB.Chalices.Services.Tests;

public class SaveLocatorServiceTests
{
    // Mirrors Nox's original filter: character files start with "userdata",
    // aren't backups, and aren't the "...10" system/options file.
    [Theory]
    [InlineData("userdata0000", true)]
    [InlineData("userdata0001", true)]
    [InlineData("userdata0009", true)]
    [InlineData("userdata0010", false)]                       // system / options file
    [InlineData("userdata0000.bak", false)]
    [InlineData("userdata0001_userdata_backup.bak", false)]   // shadPS4 backup
    [InlineData("sce_sys", false)]
    [InlineData("param.sfo", false)]
    public void IsCharacterSave_FiltersTheSameWayAsTheOriginal(string fileName, bool expected)
    {
        Assert.Equal(expected, SaveLocatorService.IsCharacterSave(fileName));
    }

    [Fact]
    public void FindSaveFiles_ReturnsOnlyCharacterSaves()
    {
        var temp = Directory.CreateTempSubdirectory("bbchalices-locator");
        try
        {
            foreach (string name in new[] { "userdata0000", "userdata0010", "userdata0000.bak", "param.sfo" })
                File.WriteAllBytes(Path.Combine(temp.FullName, name), [0]);

            var locator = new SaveLocatorService();
            var files = locator.FindSaveFiles(temp.FullName);

            Assert.Single(files);
            Assert.Equal("userdata0000", Path.GetFileName(files[0]));
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [Fact]
    public void FindSaveFolders_LocatesSprj0005Folder()
    {
        var temp = Directory.CreateTempSubdirectory("bbchalices-locator");
        try
        {
            string sprj = Path.Combine(temp.FullName, "user", "home", "x", "savedata", "CUSA", "SPRJ0005");
            Directory.CreateDirectory(sprj);

            var locator = new SaveLocatorService();
            var folders = locator.FindSaveFolders(temp.FullName);

            Assert.Single(folders);
            Assert.Equal(sprj, folders[0]);
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }
}
