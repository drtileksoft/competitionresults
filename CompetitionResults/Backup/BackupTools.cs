namespace CompetitionResults.Backup
{
    public static class BackupTools
    {
        public static async Task BackupDatabaseAsync()
        {
            await Task.Run(() => BackupDatabase());
        }

        public static void BackupDatabase()
        {
            try
            {
                var sourceFilePath = "./competition.db";
                var backupDirectory = Path.GetDirectoryName(sourceFilePath);
                var backupBaseFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                var backupExtension = ".bak";
                // Get the current time, rounded down to the nearest 10 minutes
                var now = DateTime.Now;
                var roundedTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute - now.Minute % 10, 0);
                var backupFilePath = Path.Combine(backupDirectory, $"{backupBaseFileName}_{roundedTime:yyyyMMddHHmm}{backupExtension}");

                if (File.Exists(sourceFilePath) && !File.Exists(backupFilePath))
                {
                    try
                    {
                        File.Copy(sourceFilePath, backupFilePath, overwrite: false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error writing to file: {ex.Message}");
                    }

                    // Keep only the most recent N backups
                    var maxBackupFiles = 50; // Adjust based on how many backups you want to keep
                    var backupFiles = Directory.GetFiles(backupDirectory, $"{backupBaseFileName}_*{backupExtension}")
                                               .OrderByDescending(f => f)
                                               .Skip(maxBackupFiles);

                    foreach (var file in backupFiles)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error backuping to file: {ex.Message}");
            }
        }

    }
}
