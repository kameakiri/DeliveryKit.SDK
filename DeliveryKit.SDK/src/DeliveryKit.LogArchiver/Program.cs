using System;
using System.IO;

namespace DeliveryKit.LogArchiver;

class Program
{
    static void Main()
    {
        string basePath = @"C:\DeliveryKit\Api\Logs";
        string archivePath = @"C:\DeliveryKit\Api\LogsArchive";

        int days = 180;
        DateTime threshold = DateTime.Now.AddDays(-days);

        string[] categories = { "core", "api", "error", "security", "audit", "access" };

        foreach (var category in categories)
        {
            string dir = Path.Combine(basePath, category);
            string archiveDir = Path.Combine(archivePath, category);

            Directory.CreateDirectory(archiveDir);

            foreach (var file in Directory.GetFiles(dir, "*.log"))
            {
                DateTime lastWrite = File.GetLastWriteTime(file);

                if (lastWrite < threshold)
                {
                    string dest = Path.Combine(archiveDir, Path.GetFileName(file));
                    File.Move(file, dest);
                }
            }
        }
    }
}
