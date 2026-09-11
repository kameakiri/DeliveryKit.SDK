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

            foreach (var file in Directory.GetFiles(dir, "*.log"))
            {
                DateTime lastWrite = File.GetLastWriteTime(file);

                if (lastWrite >= threshold)
                    continue;

                try
                {
                    // 移動するファイルがあると分かってから作る。無条件に作ると、
                    // 対象が1件も無いカテゴリにも空のフォルダが残る。
                    Directory.CreateDirectory(archiveDir);

                    string dest = Path.Combine(archiveDir, Path.GetFileName(file));

                    // 同名が既にある場合は移動しない。File.Move(file, dest) は
                    // 上書きせず例外を投げるが、**中身を検証せず上書きするより、
                    // 元ファイルを残して次回に回す方が安全**である。ログは追記専用で、
                    // 無警告の消失が最も困る。
                    if (File.Exists(dest))
                    {
                        Console.Error.WriteLine($"[LogArchiver] 同名のファイルが既にあるため移動しません: {dest}");
                        continue;
                    }

                    File.Move(file, dest);
                }
                catch (IOException ex)
                {
                    // **1ファイルの失敗で全体を止めない。** 書き込み中のログは
                    // ロックされていて移動できないが、それは次回の実行で片付く。
                    // ここで例外を投げると、後続のファイル・カテゴリが
                    // まるごと未処理のまま残る。
                    Console.Error.WriteLine($"[LogArchiver] 移動できませんでした（次回再試行します）: {file} - {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.Error.WriteLine($"[LogArchiver] アクセスできませんでした: {file} - {ex.Message}");
                }
            }
        }
    }
}
