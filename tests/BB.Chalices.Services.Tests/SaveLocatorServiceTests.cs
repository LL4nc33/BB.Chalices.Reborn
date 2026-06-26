using BB.Chalices.Services;

namespace BB.Chalices.Services.Tests;

public class SaveLocatorServiceTests
{
    // Mirrors Noxde's original filter: character files start with "userdata",
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
}
