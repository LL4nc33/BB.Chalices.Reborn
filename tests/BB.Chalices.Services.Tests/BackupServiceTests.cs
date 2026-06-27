using BB.Chalices.Services;

namespace BB.Chalices.Services.Tests;

public class BackupServiceTests
{
    [Fact]
    public void Backup_Lifecycle_CreateListRestoreDelete()
    {
        var temp = Directory.CreateTempSubdirectory("bbchalices-test");
        try
        {
            var config = new ConfigService();
            config.Settings.BackupDirectory = Path.Combine(temp.FullName, "backups");
            var backups = new BackupService(config);

            var save = Path.Combine(temp.FullName, "userdata0000");
            File.WriteAllBytes(save, [1, 2, 3]);

            backups.Create(save, "test");
            var first = backups.GetAll();
            Assert.Single(first);

            // Change the save, then restore the backup.
            File.WriteAllBytes(save, [9, 9, 9]);
            backups.Restore(save, first[0]);
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(save));

            // Restore keeps a pre-restore copy, so there are two backups now.
            var afterRestore = backups.GetAll();
            Assert.Equal(2, afterRestore.Count);

            backups.Delete(afterRestore[0]);
            Assert.Single(backups.GetAll());
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [Fact]
    public void Create_MissingSave_ReturnsSaveNotFound()
    {
        var temp = Directory.CreateTempSubdirectory("bbchalices-test");
        try
        {
            var config = new ConfigService();
            config.Settings.BackupDirectory = Path.Combine(temp.FullName, "backups");
            var backups = new BackupService(config);

            string missing = Path.Combine(temp.FullName, "does-not-exist");

            Assert.Equal("Save file not found.", backups.Create(missing));
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [Fact]
    public void Restore_MissingSave_ReturnsSaveNotFound()
    {
        var temp = Directory.CreateTempSubdirectory("bbchalices-test");
        try
        {
            var config = new ConfigService();
            config.Settings.BackupDirectory = Path.Combine(temp.FullName, "backups");
            var backups = new BackupService(config);

            var dummy = new BackupInfo(
                Path.Combine(temp.FullName, "any.bak"), "dummy", DateTime.Now, "userdata0000", "");
            string missing = Path.Combine(temp.FullName, "does-not-exist");

            Assert.Equal("Save file not found.", backups.Restore(missing, dummy));
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [Fact]
    public void Restore_RealSaveMissingBackup_ReturnsBackupNotFound()
    {
        var temp = Directory.CreateTempSubdirectory("bbchalices-test");
        try
        {
            var config = new ConfigService();
            config.Settings.BackupDirectory = Path.Combine(temp.FullName, "backups");
            var backups = new BackupService(config);

            string save = Path.Combine(temp.FullName, "userdata0000");
            File.WriteAllBytes(save, [1, 2, 3]);

            var dummy = new BackupInfo(
                Path.Combine(temp.FullName, "missing.bak"), "dummy", DateTime.Now, "userdata0000", "");

            Assert.Equal("Backup file not found.", backups.Restore(save, dummy));
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [Fact]
    public void Create_WithNote_StoresNoteAndOriginalFile()
    {
        var temp = Directory.CreateTempSubdirectory("bbchalices-test");
        try
        {
            var config = new ConfigService();
            config.Settings.BackupDirectory = Path.Combine(temp.FullName, "backups");
            var backups = new BackupService(config);

            string save = Path.Combine(temp.FullName, "userdata0000");
            File.WriteAllBytes(save, [1, 2, 3]);

            backups.Create(save, "my note");

            var all = backups.GetAll();
            Assert.Single(all);
            Assert.Equal("my note", all[0].Notes);
            Assert.Equal("userdata0000", all[0].OriginalFile);
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }
}
