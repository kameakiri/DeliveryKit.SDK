using System;
using System.IO;

namespace DeliveryKit.LogArchiver;

class Program
{
    static void Main(string[] args)
    {
        // 配布可能なツールが特定ドライブの絶対パスを前提にするのは環境依存が強すぎるため、
        // 既定値は実行ディレクトリ配下の相対パスにし、環境変数／コマンドライン引数で
        // 上書きできるようにした（Windowsタスクスケジューラでの運用を想定）。
        //   LogArchiver.exe [basePath] [archivePath] [days]
        string basePath = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("DELIVERYKIT_LOG_PATH")
              ?? Path.Combine(AppContext.BaseDirectory, "Logs");

        string archivePath = args.Length > 1
            ? args[1]
            : Environment.GetEnvironmentVariable("DELIVERYKIT_LOG_ARCHIVE_PATH")
              ?? Path.Combine(AppContext.BaseDirectory, "LogsArchive");

        int days = args.Length > 2 && int.TryParse(args[2], out var parsedDays) ? parsedDays : 180;

        DateTime threshold = DateTime.Now.AddDays(-days);

        string[] categories = { "core", "api", "error", "security", "audit", "access" };

        foreach (var category in categories)
        {
            string dir = Path.Combine(basePath, category);

            // ログが一度も書かれていないカテゴリはディレクトリ自体が存在せず、
            // Directory.GetFiles が例外を投げるため、事前にガードする。
            if (!Directory.Exists(dir))
                continue;

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
